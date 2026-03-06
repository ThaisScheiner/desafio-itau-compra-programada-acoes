using System.Net.Http.Json;
using BuildingBlocks.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotorCompraService.Api.Controllers.Requests;
using MotorCompraService.Api.Domain.Entities;
using MotorCompraService.Api.Infrastructure.HttpClients.Dto;
using MotorCompraService.Api.Infrastructure.Persistence;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace MotorCompraService.Api.Controllers;

[ApiController]
[Route("api/motor")]
public sealed class MotorController(
    MotorCompraDbContext db,
    IHttpClientFactory httpClientFactory) : ControllerBase
{
    [HttpPost("executar-compra")]
    public async Task<IActionResult> ExecutarCompra([FromBody] ExecutarCompraRequest req, CancellationToken ct)
    {
        var dataRef = req.DataReferencia.Date;

        // Idempotencia por data
        var jaExecutou = await db.Aportes.AnyAsync(x => x.DataReferencia.Date == dataRef, ct);
        if (jaExecutou)
            throw new DomainException("COMPRA_JA_EXECUTADA", "Compra ja foi executada para esta data.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        try
        {
            // Clientes ativos + 1/3 
            var clientesHttp = httpClientFactory.CreateClient("ClientesService");

            ClientesAtivosResponse? ativosResp;
            try
            {
                ativosResp = await clientesHttp.GetFromJsonAsync<ClientesAtivosResponse>("/api/clientes/ativos", ct);
            }
            catch (TimeoutRejectedException)
            {
                throw new DomainException(
                    "SERVICO_INDISPONIVEL",
                    "ClientesService indisponivel no momento (timeout). Tente novamente mais tarde."
                );
            }
            catch (BrokenCircuitException)
            {
                throw new DomainException(
                    "SERVICO_INDISPONIVEL",
                    "ClientesService indisponivel no momento (circuit breaker aberto). Tente novamente mais tarde."
                );
            }
            catch (HttpRequestException ex)
            {
                throw new DomainException(
                    "SERVICO_INDISPONIVEL",
                    $"Falha ao consultar ClientesService: {ex.Message}"
                );
            }

            if (ativosResp is null || ativosResp.Clientes.Count == 0)
                throw new DomainException("SEM_CLIENTES_ATIVOS", "Nenhum cliente ativo para executar compra.");

            var aportesCalc = ativosResp.Clientes.Select(c => new
            {
                c.ClienteId,
                c.Cpf,
                c.Nome,
                ValorAporte = decimal.Round(c.ValorMensal / 3m, 2)
            }).ToList();

            var totalConsolidado = aportesCalc.Sum(a => a.ValorAporte);
            if (totalConsolidado <= 0)
                throw new DomainException("TOTAL_INVALIDO", "Total consolidado invalido.");

            // Cesta atual
            var cestasHttp = httpClientFactory.CreateClient("CestasRecomendacaoService");

            CestaAtualResponse? cesta;
            try
            {
                cesta = await cestasHttp.GetFromJsonAsync<CestaAtualResponse>("/api/admin/cesta/atual", ct);
            }
            catch (TimeoutRejectedException)
            {
                throw new DomainException(
                    "SERVICO_INDISPONIVEL",
                    "CestasRecomendacaoService indisponivel no momento (timeout). Tente novamente mais tarde."
                );
            }
            catch (BrokenCircuitException)
            {
                throw new DomainException(
                    "SERVICO_INDISPONIVEL",
                    "CestasRecomendacaoService indisponivel no momento (circuit breaker aberto). Tente novamente mais tarde."
                );
            }
            catch (HttpRequestException ex)
            {
                throw new DomainException(
                    "SERVICO_INDISPONIVEL",
                    $"Falha ao consultar CestasRecomendacaoService: {ex.Message}"
                );
            }

            if (cesta is null || cesta.Itens.Count != 5)
                throw new DomainException("CESTA_NAO_ENCONTRADA", "Nenhuma cesta ativa encontrada.");

            // Cotações
            var cotacoesHttp = httpClientFactory.CreateClient("CotacoesService");
            var cotacoes = new Dictionary<string, UltimoFechamentoResponse>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in cesta.Itens)
            {
                var ticker = (item.Ticker ?? "").Trim().ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(ticker))
                    throw new DomainException("TICKER_INVALIDO", "Ticker vazio na cesta.");

                cotacoes[ticker] = await GetUltimoFechamentoOrThrow(cotacoesHttp, ticker, ct);
            }

            // So persiste os aportes depois de validar dependencias externas
            var aportesEntities = aportesCalc.Select(a => new Aporte
            {
                ClienteId = a.ClienteId,
                DataReferencia = dataRef,
                Valor = a.ValorAporte,
                Parcela = "1/3",
                CriadoEm = DateTime.UtcNow
            }).ToList();

            db.Aportes.AddRange(aportesEntities);
            await db.SaveChangesAsync(ct);

            // Saldo MASTER (residuos anteriores) 
            var custodiasHttp = httpClientFactory.CreateClient("CustodiasService");

            CustodiaMasterResponse master;
            try
            {
                master = await custodiasHttp.GetFromJsonAsync<CustodiaMasterResponse>("/api/custodias/master", ct)
                         ?? new CustodiaMasterResponse
                         {
                             TipoConta = "MASTER",
                             Posicoes = new List<CustodiaPosicaoDto>()
                         };
            }
            catch (TimeoutRejectedException)
            {
                throw new DomainException(
                    "SERVICO_INDISPONIVEL",
                    "CustodiasService indisponivel no momento (timeout). Tente novamente mais tarde."
                );
            }
            catch (BrokenCircuitException)
            {
                throw new DomainException(
                    "SERVICO_INDISPONIVEL",
                    "CustodiasService indisponivel no momento (circuit breaker aberto). Tente novamente mais tarde."
                );
            }
            catch (HttpRequestException ex)
            {
                throw new DomainException(
                    "SERVICO_INDISPONIVEL",
                    $"Falha ao consultar CustodiasService: {ex.Message}"
                );
            }

            var saldoMasterPorTicker = master.Posicoes
                .GroupBy(x => (x.Ticker ?? "").Trim().ToUpperInvariant())
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantidade), StringComparer.OrdinalIgnoreCase);

            // Plano por ticker: desejado, saldo master, compra necessária 
            var planoPorTicker = new Dictionary<string, PlanoTicker>(StringComparer.OrdinalIgnoreCase);
            var ordens = new List<OrdemCompra>();

            foreach (var item in cesta.Itens)
            {
                var ticker = item.Ticker.Trim().ToUpperInvariant();
                var pct = item.Percentual / 100m;

                var preco = cotacoes[ticker].PrecoFechamento;
                if (preco <= 0)
                    throw new DomainException("COTACAO_INVALIDA", $"Cotacao invalida para {ticker}.");

                var valorAtivo = totalConsolidado * pct;
                var qtdDesejada = (int)decimal.Floor(valorAtivo / preco);

                var saldoMaster = saldoMasterPorTicker.TryGetValue(ticker, out var s) ? s : 0;
                var qtdComprar = Math.Max(0, qtdDesejada - saldoMaster);

                planoPorTicker[ticker] = new PlanoTicker(
                    ticker,
                    qtdDesejada,
                    saldoMaster,
                    qtdComprar,
                    preco
                );

                if (qtdComprar <= 0)
                    continue;

                var lotes = qtdComprar / 100;
                var resto = qtdComprar % 100;

                if (lotes > 0)
                {
                    ordens.Add(new OrdemCompra
                    {
                        Ticker = ticker,
                        Quantidade = lotes * 100,
                        PrecoUnitario = preco,
                        TipoMercado = "LOTE_PADRAO",
                        DataExecucao = dataRef
                    });
                }

                if (resto > 0)
                {
                    ordens.Add(new OrdemCompra
                    {
                        Ticker = ticker,
                        Quantidade = resto,
                        PrecoUnitario = preco,
                        TipoMercado = "FRACIONARIO",
                        DataExecucao = dataRef
                    });
                }
            }

            if (ordens.Count > 0)
            {
                db.OrdensCompra.AddRange(ordens);
                await db.SaveChangesAsync(ct);
            }

            // Compra MASTER + Distribuição + IR 
            var eventosIrHttp = httpClientFactory.CreateClient("EventosIRService");

            var distribuicoes = new List<Distribuicao>();
            var movimentacoes = 0;
            var eventosIrPublicados = 0;

            foreach (var kv in planoPorTicker)
            {
                var ticker = kv.Key;
                var plano = kv.Value;

                var qtdTotalDisponivel = plano.SaldoMasterAnterior + plano.QtdComprar;
                if (qtdTotalDisponivel <= 0)
                    continue;

                // Compra master só do que faltou
                if (plano.QtdComprar > 0)
                {
                    var movMasterCompra = new MovimentacaoCustodiaRequest
                    {
                        TipoConta = "MASTER",
                        ClienteId = null,
                        Ticker = ticker,
                        TipoOperacao = "COMPRA",
                        Quantidade = plano.QtdComprar,
                        PrecoUnitario = plano.PrecoUnitario,
                        DataExecucao = dataRef,
                        Origem = $"CompraProgramada {dataRef:yyyy-MM-dd}"
                    };

                    var respCompra = await custodiasHttp.PostAsJsonAsync("/api/custodias/movimentar", movMasterCompra, ct);
                    if (!respCompra.IsSuccessStatusCode)
                    {
                        var body = await respCompra.Content.ReadAsStringAsync(ct);
                        throw new DomainException("CUSTODIAS_FALHA", $"Falha COMPRA MASTER {ticker}. Status={(int)respCompra.StatusCode}. Body={body}");
                    }

                    movimentacoes++;
                }

                // Distribuicao proporcional usando TOTAL DISPONÍVEL
                foreach (var a in aportesCalc)
                {
                    var proporcao = a.ValorAporte / totalConsolidado;
                    var qtdCliente = (int)decimal.Floor(qtdTotalDisponivel * proporcao);
                    if (qtdCliente <= 0)
                        continue;

                    var ordemBase = ordens.FirstOrDefault(o =>
                        o.Ticker.Equals(ticker, StringComparison.OrdinalIgnoreCase));

                    if (ordemBase is null)
                    {
                        ordemBase = new OrdemCompra
                        {
                            Ticker = ticker,
                            Quantidade = 0,
                            PrecoUnitario = plano.PrecoUnitario,
                            TipoMercado = "RESIDUO_MASTER",
                            DataExecucao = dataRef
                        };

                        db.OrdensCompra.Add(ordemBase);
                        await db.SaveChangesAsync(ct);
                        ordens.Add(ordemBase);
                    }

                    distribuicoes.Add(new Distribuicao
                    {
                        OrdemCompraId = ordemBase.Id,
                        ClienteId = a.ClienteId,
                        Ticker = ticker,
                        Quantidade = qtdCliente,
                        PrecoUnitario = plano.PrecoUnitario,
                        DataDistribuicao = dataRef
                    });

                    var movMasterSaida = new MovimentacaoCustodiaRequest
                    {
                        TipoConta = "MASTER",
                        ClienteId = null,
                        Ticker = ticker,
                        TipoOperacao = "VENDA",
                        Quantidade = qtdCliente,
                        PrecoUnitario = plano.PrecoUnitario,
                        DataExecucao = dataRef,
                        Origem = $"Distribuicao p/ cliente {a.ClienteId}"
                    };

                    var movFilhoteEntrada = new MovimentacaoCustodiaRequest
                    {
                        TipoConta = "FILHOTE",
                        ClienteId = a.ClienteId,
                        Ticker = ticker,
                        TipoOperacao = "COMPRA",
                        Quantidade = qtdCliente,
                        PrecoUnitario = plano.PrecoUnitario,
                        DataExecucao = dataRef,
                        Origem = $"Distribuicao Motor {dataRef:yyyy-MM-dd}"
                    };

                    var respSaida = await custodiasHttp.PostAsJsonAsync("/api/custodias/movimentar", movMasterSaida, ct);
                    if (!respSaida.IsSuccessStatusCode)
                    {
                        var body = await respSaida.Content.ReadAsStringAsync(ct);
                        throw new DomainException("CUSTODIAS_FALHA", $"Falha SAIDA MASTER {ticker} cliente {a.ClienteId}. Status={(int)respSaida.StatusCode}. Body={body}");
                    }

                    var respEntrada = await custodiasHttp.PostAsJsonAsync("/api/custodias/movimentar", movFilhoteEntrada, ct);
                    if (!respEntrada.IsSuccessStatusCode)
                    {
                        var body = await respEntrada.Content.ReadAsStringAsync(ct);
                        throw new DomainException("CUSTODIAS_FALHA", $"Falha ENTRADA FILHOTE {ticker} cliente {a.ClienteId}. Status={(int)respEntrada.StatusCode}. Body={body}");
                    }

                    movimentacoes += 2;

                    // IR dedo-duro
                    var valorOperacao = qtdCliente * plano.PrecoUnitario;
                    var aliquota = 0.00005m;
                    var valorIr = decimal.Round(valorOperacao * aliquota, 2);

                    var eventoReq = new
                    {
                        clienteId = a.ClienteId,
                        ticker,
                        quantidade = qtdCliente,
                        precoUnitario = plano.PrecoUnitario,
                        valorOperacao = decimal.Round(valorOperacao, 2),
                        valorIR = valorIr,
                        dataOperacao = dataRef
                    };

                    HttpResponseMessage respIr;
                    try
                    {
                        respIr = await eventosIrHttp.PostAsJsonAsync("/api/eventos-ir/dedo-duro", eventoReq, ct);
                    }
                    catch (TimeoutRejectedException)
                    {
                        throw new DomainException(
                            "SERVICO_INDISPONIVEL",
                            "EventosIRService indisponivel no momento (timeout). Tente novamente mais tarde."
                        );
                    }
                    catch (BrokenCircuitException)
                    {
                        throw new DomainException(
                            "SERVICO_INDISPONIVEL",
                            "EventosIRService indisponivel no momento (circuit breaker aberto). Tente novamente mais tarde."
                        );
                    }
                    catch (HttpRequestException ex)
                    {
                        throw new DomainException(
                            "SERVICO_INDISPONIVEL",
                            $"Falha ao consultar EventosIRService: {ex.Message}"
                        );
                    }

                    if (!respIr.IsSuccessStatusCode)
                    {
                        var body = await respIr.Content.ReadAsStringAsync(ct);
                        throw new DomainException("EVENTOS_IR_FALHA", $"Falha IR (cliente {a.ClienteId}, {ticker}). Status={(int)respIr.StatusCode}. Body={body}");
                    }

                    eventosIrPublicados++;
                }
            }

            db.Distribuicoes.AddRange(distribuicoes);
            await db.SaveChangesAsync(ct);

            // Resíduos finais 
            CustodiaMasterResponse masterFinal;
            try
            {
                masterFinal = await custodiasHttp.GetFromJsonAsync<CustodiaMasterResponse>("/api/custodias/master", ct)
                              ?? new CustodiaMasterResponse
                              {
                                  TipoConta = "MASTER",
                                  Posicoes = new List<CustodiaPosicaoDto>()
                              };
            }
            catch (TimeoutRejectedException)
            {
                throw new DomainException(
                    "SERVICO_INDISPONIVEL",
                    "CustodiasService indisponivel no momento (timeout). Tente novamente mais tarde."
                );
            }
            catch (BrokenCircuitException)
            {
                throw new DomainException(
                    "SERVICO_INDISPONIVEL",
                    "CustodiasService indisponivel no momento (circuit breaker aberto). Tente novamente mais tarde."
                );
            }
            catch (HttpRequestException ex)
            {
                throw new DomainException(
                    "SERVICO_INDISPONIVEL",
                    $"Falha ao consultar CustodiasService: {ex.Message}"
                );
            }

            var residuos = masterFinal.Posicoes
                .Where(p => p.Quantidade > 0)
                .Select(p => new
                {
                    ticker = p.Ticker,
                    quantidade = p.Quantidade,
                    precoMedio = p.PrecoMedio
                })
                .OrderBy(x => x.ticker)
                .ToList();

            await tx.CommitAsync(ct);

            return Ok(new
            {
                dataExecucao = DateTime.UtcNow,
                dataReferencia = dataRef,
                totalClientes = aportesCalc.Count,
                totalConsolidado,

                planoPorTicker = planoPorTicker.Values.Select(p => new
                {
                    p.Ticker,
                    qtdDesejada = p.QtdDesejada,
                    saldoMasterAnterior = p.SaldoMasterAnterior,
                    qtdComprada = p.QtdComprar,
                    totalDisponivel = p.SaldoMasterAnterior + p.QtdComprar
                }),

                ordensCompra = ordens.Select(o => new
                {
                    o.Id,
                    o.Ticker,
                    o.Quantidade,
                    o.PrecoUnitario,
                    o.TipoMercado
                }),

                distribuicoes = distribuicoes
                    .GroupBy(d => d.ClienteId)
                    .Select(g => new
                    {
                        clienteId = g.Key,
                        valorAporte = aportesCalc.First(x => x.ClienteId == g.Key).ValorAporte,
                        ativos = g.Select(x => new { x.Ticker, x.Quantidade })
                    }),

                eventosIRPublicados = eventosIrPublicados,
                movimentacoesCustodia = movimentacoes,
                residuosCustodiaMaster = residuos,

                mensagem = $"Compra programada executada com sucesso para {aportesCalc.Count} clientes."
            });
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    [HttpGet("aportes/{clienteId:long}")]
    public async Task<IActionResult> Aportes(long clienteId, CancellationToken ct)
    {
        var lista = await db.Aportes
            .Where(x => x.ClienteId == clienteId)
            .OrderBy(x => x.DataReferencia)
            .Select(x => new
            {
                data = x.DataReferencia.ToString("yyyy-MM-dd"),
                valor = x.Valor,
                parcela = x.Parcela
            })
            .ToListAsync(ct);

        return Ok(new { clienteId, historicoAportes = lista });
    }

    // Helpers

    private static async Task<UltimoFechamentoResponse> GetUltimoFechamentoOrThrow(
        HttpClient cotacoesHttp,
        string ticker,
        CancellationToken ct)
    {
        var url = $"/api/cotacoes/fechamento/ultimo?ticker={Uri.EscapeDataString(ticker)}";

        HttpResponseMessage resp;
        try
        {
            resp = await cotacoesHttp.GetAsync(url, ct);
        }
        catch (TimeoutRejectedException)
        {
            throw new DomainException(
                "SERVICO_INDISPONIVEL",
                $"CotacoesService indisponivel no momento (timeout) para ticker={ticker}. Tente novamente mais tarde."
            );
        }
        catch (BrokenCircuitException)
        {
            throw new DomainException(
                "SERVICO_INDISPONIVEL",
                $"CotacoesService indisponivel no momento (circuit breaker aberto) para ticker={ticker}. Tente novamente mais tarde."
            );
        }
        catch (HttpRequestException ex)
        {
            throw new DomainException(
                "SERVICO_INDISPONIVEL",
                $"Falha ao consultar CotacoesService para ticker={ticker}: {ex.Message}"
            );
        }

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            throw new DomainException(
                "COTACOES_FALHA",
                $"CotacoesService retornou {(int)resp.StatusCode} para ticker={ticker}. Body: {body}"
            );
        }

        var cot = await resp.Content.ReadFromJsonAsync<UltimoFechamentoResponse>(cancellationToken: ct);
        if (cot is null || cot.PrecoFechamento <= 0)
            throw new DomainException("COTACAO_NAO_ENCONTRADA", $"Cotacao nao encontrada/valida para {ticker}.");

        return cot;
    }

    private sealed record PlanoTicker(
        string Ticker,
        int QtdDesejada,
        int SaldoMasterAnterior,
        int QtdComprar,
        decimal PrecoUnitario
    );
}