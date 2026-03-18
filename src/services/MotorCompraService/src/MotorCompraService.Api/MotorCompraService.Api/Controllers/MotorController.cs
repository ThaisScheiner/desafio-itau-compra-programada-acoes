using BuildingBlocks.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotorCompraService.Api.Controllers.Requests;
using MotorCompraService.Api.Domain.Entities;
using MotorCompraService.Api.Infrastructure.HttpClients.Dto;
using MotorCompraService.Api.Infrastructure.Observability;
using MotorCompraService.Api.Infrastructure.Persistence;
using OpenTelemetry.Trace;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System.Diagnostics;
using System.Net.Http.Json;

namespace MotorCompraService.Api.Controllers;

[ApiController]
[Route("api/motor")]
public sealed class MotorController(
    MotorCompraDbContext db,
    IHttpClientFactory httpClientFactory,
    ILogger<MotorController> logger) : ControllerBase
{
    [HttpPost("executar-compra")]
    public async Task<IActionResult> ExecutarCompra([FromBody] ExecutarCompraRequest req, CancellationToken ct)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("motor_compra.executar_compra", ActivityKind.Server);

        using var logScope = logger.BeginScope(LogScopeHelper.CreateTraceScope());

        var stopwatch = Stopwatch.StartNew();

        var dataRef = req.DataReferencia.Date;

        activity?.SetTag("motor_compra.data_referencia", dataRef.ToString("yyyy-MM-dd"));

        // Idempotencia por data
        var jaExecutou = await db.Aportes.AnyAsync(x => x.DataReferencia.Date == dataRef, ct);
        if (jaExecutou)
        {
            activity?.SetTag("motor_compra.idempotencia", true);
            activity?.SetTag("motor_compra.resultado", "compra_ja_executada");
            Telemetry.ComprasComErro.Add(1,
                new KeyValuePair<string, object?>("motivo", "compra_ja_executada"));

            throw new DomainException("COMPRA_JA_EXECUTADA", "Compra ja foi executada para esta data.");
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        try
        {
            // Clientes ativos + 1/3
            var clientesHttp = httpClientFactory.CreateClient("ClientesService");

            ClientesAtivosResponse? ativosResp;
            using (var span = Telemetry.ActivitySource.StartActivity("motor_compra.buscar_clientes_ativos", ActivityKind.Internal))
            {
                try
                {
                    ativosResp = await clientesHttp.GetFromJsonAsync<ClientesAtivosResponse>("/api/clientes/ativos", ct);
                    span?.SetTag("motor_compra.clientes_service.sucesso", true);
                    logger.LogInformation(
                        "Clientes carregados com sucesso. TotalClientes={TotalClientes}",
                        ativosResp.Clientes.Count);

                }
                catch (TimeoutRejectedException)
                {
                    span?.SetTag("motor_compra.clientes_service.sucesso", false);
                    span?.SetTag("motor_compra.erro.tipo", "timeout");
                    throw new DomainException(
                        "SERVICO_INDISPONIVEL",
                        "ClientesService indisponivel no momento (timeout). Tente novamente mais tarde."
                    );
                }
                catch (BrokenCircuitException)
                {
                    span?.SetTag("motor_compra.clientes_service.sucesso", false);
                    span?.SetTag("motor_compra.erro.tipo", "circuit_breaker");
                    throw new DomainException(
                        "SERVICO_INDISPONIVEL",
                        "ClientesService indisponivel no momento (circuit breaker aberto). Tente novamente mais tarde."
                    );
                }
                catch (HttpRequestException ex)
                {
                    span?.SetTag("motor_compra.clientes_service.sucesso", false);
                    span?.SetTag("motor_compra.erro.tipo", "http_request");
                    span?.RecordException(ex);
                    throw new DomainException(
                        "SERVICO_INDISPONIVEL",
                        $"Falha ao consultar ClientesService: {ex.Message}"
                    );
                }
            }

            if (ativosResp is null || ativosResp.Clientes.Count == 0)
            {
                activity?.SetTag("motor_compra.total_clientes", 0);
                Telemetry.ComprasComErro.Add(1,
                    new KeyValuePair<string, object?>("motivo", "sem_clientes_ativos"));

                throw new DomainException("SEM_CLIENTES_ATIVOS", "Nenhum cliente ativo para executar compra.");
            }

            var aportesCalc = ativosResp.Clientes.Select(c => new
            {
                c.ClienteId,
                c.Cpf,
                c.Nome,
                ValorAporte = decimal.Round(c.ValorMensal / 3m, 2)
            }).ToList();

            var totalConsolidado = aportesCalc.Sum(a => a.ValorAporte);

            activity?.SetTag("motor_compra.total_clientes", aportesCalc.Count);
            activity?.SetTag("motor_compra.total_consolidado", (double)totalConsolidado);

            if (totalConsolidado <= 0)
            {
                Telemetry.ComprasComErro.Add(1,
                    new KeyValuePair<string, object?>("motivo", "total_invalido"));

                throw new DomainException("TOTAL_INVALIDO", "Total consolidado invalido.");
            }

            // Cesta atual
            var cestasHttp = httpClientFactory.CreateClient("CestasRecomendacaoService");

            CestaAtualResponse? cesta;
            using (var span = Telemetry.ActivitySource.StartActivity("motor_compra.buscar_cesta_atual", ActivityKind.Internal))
            {
                try
                {
                    cesta = await cestasHttp.GetFromJsonAsync<CestaAtualResponse>("/api/admin/cesta/atual", ct);
                    span?.SetTag("motor_compra.cesta_service.sucesso", true);
                    logger.LogInformation(
                        "Cesta carregada. TotalItens={TotalItens}",
                         cesta.Itens.Count);
                }
                catch (TimeoutRejectedException)
                {
                    span?.SetTag("motor_compra.cesta_service.sucesso", false);
                    span?.SetTag("motor_compra.erro.tipo", "timeout");
                    throw new DomainException(
                        "SERVICO_INDISPONIVEL",
                        "CestasRecomendacaoService indisponivel no momento (timeout). Tente novamente mais tarde."
                    );
                }
                catch (BrokenCircuitException)
                {
                    span?.SetTag("motor_compra.cesta_service.sucesso", false);
                    span?.SetTag("motor_compra.erro.tipo", "circuit_breaker");
                    throw new DomainException(
                        "SERVICO_INDISPONIVEL",
                        "CestasRecomendacaoService indisponivel no momento (circuit breaker aberto). Tente novamente mais tarde."
                    );
                }
                catch (HttpRequestException ex)
                {
                    span?.SetTag("motor_compra.cesta_service.sucesso", false);
                    span?.SetTag("motor_compra.erro.tipo", "http_request");
                    span?.RecordException(ex);
                    throw new DomainException(
                        "SERVICO_INDISPONIVEL",
                        $"Falha ao consultar CestasRecomendacaoService: {ex.Message}"
                    );
                }
            }

            if (cesta is null || cesta.Itens.Count != 5)
            {
                Telemetry.ComprasComErro.Add(1,
                    new KeyValuePair<string, object?>("motivo", "cesta_nao_encontrada"));

                throw new DomainException("CESTA_NAO_ENCONTRADA", "Nenhuma cesta ativa encontrada.");
            }

            activity?.SetTag("motor_compra.total_itens_cesta", cesta.Itens.Count);

            // Cotações
            var cotacoesHttp = httpClientFactory.CreateClient("CotacoesService");
            var cotacoes = new Dictionary<string, UltimoFechamentoResponse>(StringComparer.OrdinalIgnoreCase);

            using (var span = Telemetry.ActivitySource.StartActivity("motor_compra.buscar_cotacoes", ActivityKind.Internal))
            {
                foreach (var item in cesta.Itens)
                {
                    var ticker = (item.Ticker ?? "").Trim().ToUpperInvariant();
                    if (string.IsNullOrWhiteSpace(ticker))
                    {
                        span?.SetTag("motor_compra.erro.tipo", "ticker_invalido");
                        throw new DomainException("TICKER_INVALIDO", "Ticker vazio na cesta.");
                    }

                    cotacoes[ticker] = await GetUltimoFechamentoOrThrow(cotacoesHttp, ticker, ct);
                }

                span?.SetTag("motor_compra.total_cotacoes", cotacoes.Count);
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
            using (var span = Telemetry.ActivitySource.StartActivity("motor_compra.buscar_custodia_master_inicial", ActivityKind.Internal))
            {
                try
                {
                    master = await custodiasHttp.GetFromJsonAsync<CustodiaMasterResponse>("/api/custodias/master", ct)
                             ?? new CustodiaMasterResponse
                             {
                                 TipoConta = "MASTER",
                                 Posicoes = new List<CustodiaPosicaoDto>()
                             };

                    span?.SetTag("motor_compra.custodia_master.qtd_posicoes", master.Posicoes.Count);
                }
                catch (TimeoutRejectedException)
                {
                    span?.SetTag("motor_compra.erro.tipo", "timeout");
                    throw new DomainException(
                        "SERVICO_INDISPONIVEL",
                        "CustodiasService indisponivel no momento (timeout). Tente novamente mais tarde."
                    );
                }
                catch (BrokenCircuitException)
                {
                    span?.SetTag("motor_compra.erro.tipo", "circuit_breaker");
                    throw new DomainException(
                        "SERVICO_INDISPONIVEL",
                        "CustodiasService indisponivel no momento (circuit breaker aberto). Tente novamente mais tarde."
                    );
                }
                catch (HttpRequestException ex)
                {
                    span?.SetTag("motor_compra.erro.tipo", "http_request");
                    span?.RecordException(ex);
                    throw new DomainException(
                        "SERVICO_INDISPONIVEL",
                        $"Falha ao consultar CustodiasService: {ex.Message}"
                    );
                }
            }

            var saldoMasterPorTicker = master.Posicoes
                .GroupBy(x => (x.Ticker ?? "").Trim().ToUpperInvariant())
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantidade), StringComparer.OrdinalIgnoreCase);

            // Plano por ticker: desejado, saldo master, compra necessária
            var planoPorTicker = new Dictionary<string, PlanoTicker>(StringComparer.OrdinalIgnoreCase);
            var ordens = new List<OrdemCompra>();

            using (var span = Telemetry.ActivitySource.StartActivity("motor_compra.calcular_plano_por_ticker", ActivityKind.Internal))
            {
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

                span?.SetTag("motor_compra.total_ordens_planejadas", ordens.Count);
                span?.SetTag("motor_compra.total_tickers_planejados", planoPorTicker.Count);

                logger.LogInformation(
                     "Plano montado. Ordens={Ordens}, Tickers={Tickers}",
                        ordens.Count,
                        planoPorTicker.Count);
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

            using (var span = Telemetry.ActivitySource.StartActivity("motor_compra.distribuir_ativos_e_publicar_ir", ActivityKind.Internal))
            {
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

                span?.SetTag("motor_compra.total_distribuicoes", distribuicoes.Count);
                span?.SetTag("motor_compra.movimentacoes_custodia", movimentacoes);
                span?.SetTag("motor_compra.eventos_ir_publicados", eventosIrPublicados);
            }

            db.Distribuicoes.AddRange(distribuicoes);
            await db.SaveChangesAsync(ct);

            // Resíduos finais
            CustodiaMasterResponse masterFinal;
            using (var span = Telemetry.ActivitySource.StartActivity("motor_compra.buscar_custodia_master_final", ActivityKind.Internal))
            {
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
                    span?.SetTag("motor_compra.erro.tipo", "timeout");
                    throw new DomainException(
                        "SERVICO_INDISPONIVEL",
                        "CustodiasService indisponivel no momento (timeout). Tente novamente mais tarde."
                    );
                }
                catch (BrokenCircuitException)
                {
                    span?.SetTag("motor_compra.erro.tipo", "circuit_breaker");
                    throw new DomainException(
                        "SERVICO_INDISPONIVEL",
                        "CustodiasService indisponivel no momento (circuit breaker aberto). Tente novamente mais tarde."
                    );
                }
                catch (HttpRequestException ex)
                {
                    span?.SetTag("motor_compra.erro.tipo", "http_request");
                    span?.RecordException(ex);
                    throw new DomainException(
                        "SERVICO_INDISPONIVEL",
                        $"Falha ao consultar CustodiasService: {ex.Message}"
                    );
                }
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

            stopwatch.Stop();

            Telemetry.ComprasExecutadas.Add(1,
                new KeyValuePair<string, object?>("resultado", "sucesso"));

            Telemetry.DuracaoExecucaoCompraMs.Record(stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("resultado", "sucesso"));

            activity?.SetTag("motor_compra.resultado", "sucesso");
            activity?.SetTag("motor_compra.total_residuos_master", residuos.Count);

            logger.LogInformation(
                "Compra programada executada com sucesso. DataRef={DataRef}, TotalClientes={TotalClientes}, TotalConsolidado={TotalConsolidado}, EventosIR={EventosIR}",
                dataRef,
                aportesCalc.Count,
                totalConsolidado,
                eventosIrPublicados);

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
        catch (Exception ex)
        {
            stopwatch.Stop();

            Telemetry.ComprasComErro.Add(1,
                new KeyValuePair<string, object?>("resultado", "erro"));

            Telemetry.DuracaoExecucaoCompraMs.Record(stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("resultado", "erro"));

            activity?.SetTag("motor_compra.resultado", "erro");
            activity?.RecordException(ex);

            logger.LogError(ex, "Erro ao executar compra programada para DataRef={DataRef}", dataRef);

            await tx.RollbackAsync(ct);
            throw;
        }
    }

    [HttpGet("aportes/{clienteId:long}")]
    public async Task<IActionResult> Aportes(long clienteId, CancellationToken ct)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("motor_compra.consultar_aportes", ActivityKind.Server);

        using var logScope = logger.BeginScope(LogScopeHelper.CreateTraceScope());

        activity?.SetTag("motor_compra.cliente_id", clienteId);

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

        activity?.SetTag("motor_compra.total_aportes", lista.Count);

        logger.LogInformation("Consulta de aportes finalizada. ClienteId={ClienteId}, TotalAportes={TotalAportes}", clienteId, lista.Count);

        return Ok(new { clienteId, historicoAportes = lista });
    }

    // Helpers
    private static async Task<UltimoFechamentoResponse> GetUltimoFechamentoOrThrow(
        HttpClient cotacoesHttp,
        string ticker,
        CancellationToken ct)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("motor_compra.buscar_ultimo_fechamento", ActivityKind.Internal);
        activity?.SetTag("motor_compra.ticker", ticker);

        var url = $"/api/cotacoes/fechamento/ultimo?ticker={Uri.EscapeDataString(ticker)}";

        HttpResponseMessage resp;
        try
        {
            resp = await cotacoesHttp.GetAsync(url, ct);
        }
        catch (TimeoutRejectedException)
        {
            activity?.SetTag("motor_compra.erro.tipo", "timeout");
            throw new DomainException(
                "SERVICO_INDISPONIVEL",
                $"CotacoesService indisponivel no momento (timeout) para ticker={ticker}. Tente novamente mais tarde."
            );
        }
        catch (BrokenCircuitException)
        {
            activity?.SetTag("motor_compra.erro.tipo", "circuit_breaker");
            throw new DomainException(
                "SERVICO_INDISPONIVEL",
                $"CotacoesService indisponivel no momento (circuit breaker aberto) para ticker={ticker}. Tente novamente mais tarde."
            );
        }
        catch (HttpRequestException ex)
        {
            activity?.SetTag("motor_compra.erro.tipo", "http_request");
            activity?.RecordException(ex);
            throw new DomainException(
                "SERVICO_INDISPONIVEL",
                $"Falha ao consultar CotacoesService para ticker={ticker}: {ex.Message}"
            );
        }

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            activity?.SetTag("motor_compra.erro.tipo", "http_status");
            activity?.SetTag("motor_compra.http_status", (int)resp.StatusCode);

            throw new DomainException(
                "COTACOES_FALHA",
                $"CotacoesService retornou {(int)resp.StatusCode} para ticker={ticker}. Body: {body}"
            );
        }

        var cot = await resp.Content.ReadFromJsonAsync<UltimoFechamentoResponse>(cancellationToken: ct);
        if (cot is null || cot.PrecoFechamento <= 0)
        {
            activity?.SetTag("motor_compra.erro.tipo", "cotacao_invalida");
            throw new DomainException("COTACAO_NAO_ENCONTRADA", $"Cotacao nao encontrada/valida para {ticker}.");
        }

        activity?.SetTag("motor_compra.preco_fechamento", (double)cot.PrecoFechamento);

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