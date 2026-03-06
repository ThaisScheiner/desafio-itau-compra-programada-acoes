using System.Net.Http.Json;
using BuildingBlocks.Exceptions;
using Microsoft.EntityFrameworkCore;
using RebalanceamentosService.Api.Domain.Entities;
using RebalanceamentosService.Api.Domain.Enums;
using RebalanceamentosService.Api.Infrastructure.HttpClients.Dto;
using RebalanceamentosService.Api.Infrastructure.Kafka;
using RebalanceamentosService.Api.Infrastructure.Kafka.Messages;
using RebalanceamentosService.Api.Infrastructure.Persistence;

namespace RebalanceamentosService.Api.Application;

public sealed class RebalanceamentoExecutor(
    RebalanceamentosDbContext db,
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    IKafkaProducer producer) : IRebalanceamentoExecutor
{
    public async Task ExecutarMudancaCestaParaTodos(CestaAlteradaMessage msg, CancellationToken ct)
    {
        var clientesHttp = httpClientFactory.CreateClient("ClientesService");
        var ativos = await clientesHttp.GetFromJsonAsync<ClientesAtivosResponse>(
            "/api/clientes/ativos",
            ct);

        var lista = ativos?.Clientes ?? new List<ClienteAtivoDto>();
        if (lista.Count == 0)
            return;

        const decimal limite = 2m;
        const decimal pctMov = 10m;

        var totalExecutados = 0;

        foreach (var c in lista)
        {
            var result = await ExecutarParaCliente(
                c.ClienteId,
                TipoRebalanceamento.MUDANCA_CESTA,
                limite,
                pctMov,
                ct);

            if (result.Executou)
                totalExecutados++;
        }

        var topicOut = config["Kafka:TopicRebalanceamentos"] ?? "rebalanceamentos-eventos";

        await producer.ProduceAsync(
            topicOut,
            key: $"cesta:{msg.CestaNovaId}",
            payload: new
            {
                tipo = "MUDANCA_CESTA_PROCESSADA",
                cestaId = msg.CestaNovaId,
                nomeCesta = msg.NomeCestaNova,
                dataAlteracao = msg.DataCriacaoCestaNova,
                totalClientes = lista.Count,
                totalRebalanceados = totalExecutados
            },
            ct);
    }

    public async Task<ExecucaoRebalanceamentoResult> ExecutarParaCliente(
        long clienteId,
        TipoRebalanceamento tipo,
        decimal limiteDesvioPercentual,
        decimal percentualMovimentacao,
        CancellationToken ct)
    {
        if (limiteDesvioPercentual <= 0)
            throw new DomainException("LIMITE_INVALIDO", "LimiteDesvioPercentual deve ser > 0.");

        if (percentualMovimentacao <= 0 || percentualMovimentacao > 100)
            throw new DomainException("PERCENTUAL_INVALIDO", "PercentualMovimentacao deve estar entre 0 e 100.");

        var cestasHttp = httpClientFactory.CreateClient("CestasRecomendacaoService");
        var cotacoesHttp = httpClientFactory.CreateClient("CotacoesService");
        var custodiasHttp = httpClientFactory.CreateClient("CustodiasService");
        var eventosIrHttp = httpClientFactory.CreateClient("EventosIRService");

        // Cesta atual
        var cesta = await cestasHttp.GetFromJsonAsync<CestaDto>(
            "/api/admin/cesta/atual",
            ct);

        if (cesta is null || cesta.Itens.Count != 5)
            throw new DomainException("CESTA_NAO_ENCONTRADA", "Nenhuma cesta ativa encontrada.");

        var alvo = cesta.Itens
            .Where(i => !string.IsNullOrWhiteSpace(i.Ticker))
            .ToDictionary(
                i => i.Ticker.Trim().ToUpperInvariant(),
                i => decimal.Round(i.Percentual, 4),
                StringComparer.OrdinalIgnoreCase);

        // Custodia do cliente
        var custodia = await custodiasHttp.GetFromJsonAsync<CustodiasClientResponse>(
            $"/api/custodias/cliente/{clienteId}",
            ct);

        var posicoes = custodia?.Posicoes ?? new List<CustodiaPosicaoDto>();

        if (posicoes.Count == 0)
        {
            return new ExecucaoRebalanceamentoResult
            {
                ClienteId = clienteId,
                Executou = false,
                Mensagem = "Cliente sem posições em custódia. Nada a rebalancear."
            };
        }

        // Cotacoes atuais
        var precoPorTicker = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var t in alvo.Keys)
        {
            var cot = await cotacoesHttp.GetFromJsonAsync<UltimoFechamentoResponse>(
                $"/api/cotacoes/fechamento/ultimo?ticker={Uri.EscapeDataString(t)}",
                ct);

            var preco = cot?.PrecoFechamento ?? 0m;
            if (preco <= 0)
                throw new DomainException("COTACAO_NAO_ENCONTRADA", $"Cotacao invalida para {t}.");

            precoPorTicker[t] = preco;
        }

        // Valor atual da carteira
        var valorPorTicker = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        decimal valorTotalCarteira = 0m;

        foreach (var p in posicoes)
        {
            var t = p.Ticker.Trim().ToUpperInvariant();
            if (!alvo.ContainsKey(t))
                continue;

            var preco = precoPorTicker[t];
            var valor = decimal.Round(p.Quantidade * preco, 2);

            valorPorTicker[t] = (valorPorTicker.TryGetValue(t, out var acc) ? acc : 0m) + valor;
            valorTotalCarteira += valor;
        }

        if (valorTotalCarteira <= 0)
        {
            return new ExecucaoRebalanceamentoResult
            {
                ClienteId = clienteId,
                Executou = false,
                ValorCarteira = 0m,
                Mensagem = "Carteira com valor total 0 (sem ativos da cesta). Nada a rebalancear."
            };
        }

        // Diagnostico
        decimal maiorDesvioAbs = 0m;

        var diagnostico = alvo.Keys
            .OrderBy(x => x)
            .Select(t =>
            {
                var pctAlvo = alvo[t];
                var valorAtual = valorPorTicker.TryGetValue(t, out var v) ? v : 0m;
                var pctAtual = decimal.Round((valorAtual / valorTotalCarteira) * 100m, 4);
                var desvio = decimal.Round(pctAtual - pctAlvo, 4);

                maiorDesvioAbs = Math.Max(maiorDesvioAbs, Math.Abs(desvio));

                return new
                {
                    Ticker = t,
                    PercentualAlvo = pctAlvo,
                    PercentualAtual = pctAtual,
                    DesvioPercentual = desvio,
                    ValorAtual = decimal.Round(valorAtual, 2)
                };
            })
            .ToList();

        if (maiorDesvioAbs < limiteDesvioPercentual)
        {
            return new ExecucaoRebalanceamentoResult
            {
                ClienteId = clienteId,
                Executou = false,
                ValorCarteira = decimal.Round(valorTotalCarteira, 2),
                MaiorDesvioAbsPercentual = decimal.Round(maiorDesvioAbs, 4),
                Mensagem = $"Maior desvio ({maiorDesvioAbs:0.####}%) abaixo do limite ({limiteDesvioPercentual:0.####}%)."
            };
        }

        // Escolhe maior sobrealocacao para vender e maior subalocacao para comprar
        var vender = diagnostico.OrderByDescending(x => x.DesvioPercentual).First();
        var comprar = diagnostico.OrderBy(x => x.DesvioPercentual).First();

        if (vender.DesvioPercentual <= 0 || comprar.DesvioPercentual >= 0)
        {
            return new ExecucaoRebalanceamentoResult
            {
                ClienteId = clienteId,
                Executou = false,
                ValorCarteira = decimal.Round(valorTotalCarteira, 2),
                MaiorDesvioAbsPercentual = decimal.Round(maiorDesvioAbs, 4),
                Mensagem = "Nao foi possivel determinar par de compra/venda (desvios inconsistentes)."
            };
        }

        var tickerVendido = vender.Ticker;
        var tickerComprado = comprar.Ticker;

        var precoVenda = precoPorTicker[tickerVendido];
        var precoCompra = precoPorTicker[tickerComprado];

        // Valor a movimentar
        var valorMovimentar = decimal.Round(valorTotalCarteira * (percentualMovimentacao / 100m), 2);
        if (valorMovimentar <= 0)
            throw new DomainException("VALOR_MOVIMENTACAO_INVALIDO", "Valor de movimentacao calculado <= 0.");

        // Quantidade disponivel para vender
        var posicaoVenda = posicoes
            .FirstOrDefault(p => p.Ticker.Trim().Equals(tickerVendido, StringComparison.OrdinalIgnoreCase));

        var qtdClienteVender = posicoes
            .Where(p => p.Ticker.Trim().Equals(tickerVendido, StringComparison.OrdinalIgnoreCase))
            .Sum(p => p.Quantidade);

        var precoMedio = posicaoVenda?.PrecoMedio ?? 0m;
        if (precoMedio < 0)
            precoMedio = 0m;

        var qtdVenda = (int)decimal.Floor(valorMovimentar / precoVenda);
        qtdVenda = Math.Min(qtdVenda, qtdClienteVender);

        if (qtdVenda <= 0)
        {
            return new ExecucaoRebalanceamentoResult
            {
                ClienteId = clienteId,
                Executou = false,
                ValorCarteira = decimal.Round(valorTotalCarteira, 2),
                MaiorDesvioAbsPercentual = decimal.Round(maiorDesvioAbs, 4),
                Mensagem = $"QtdVenda calculada = 0 (preco alto ou sem quantidade no ticker {tickerVendido})."
            };
        }

        var valorVenda = decimal.Round(qtdVenda * precoVenda, 2);
        var lucroVendaAtual = decimal.Round(valorVenda - (qtdVenda * precoMedio), 2);

        // Quantidade a comprar
        var qtdCompra = (int)decimal.Floor(valorVenda / precoCompra);

        if (qtdCompra <= 0)
        {
            return new ExecucaoRebalanceamentoResult
            {
                ClienteId = clienteId,
                Executou = false,
                ValorCarteira = decimal.Round(valorTotalCarteira, 2),
                MaiorDesvioAbsPercentual = decimal.Round(maiorDesvioAbs, 4),
                Mensagem = $"QtdCompra calculada = 0 (preco alto no ticker {tickerComprado})."
            };
        }

        var dataExecucao = DateTime.UtcNow;

        // Movimenta custodia
        var vendaReq = new MovimentacaoCustodiaRequest
        {
            TipoConta = "FILHOTE",
            ClienteId = clienteId,
            Ticker = tickerVendido,
            TipoOperacao = "VENDA",
            Quantidade = qtdVenda,
            PrecoUnitario = precoVenda,
            DataExecucao = dataExecucao,
            Origem = $"Rebalanceamento {tipo} (VENDA) {dataExecucao:yyyy-MM-dd}"
        };

        var compraReq = new MovimentacaoCustodiaRequest
        {
            TipoConta = "FILHOTE",
            ClienteId = clienteId,
            Ticker = tickerComprado,
            TipoOperacao = "COMPRA",
            Quantidade = qtdCompra,
            PrecoUnitario = precoCompra,
            DataExecucao = dataExecucao,
            Origem = $"Rebalanceamento {tipo} (COMPRA) {dataExecucao:yyyy-MM-dd}"
        };

        var respVenda = await custodiasHttp.PostAsJsonAsync("/api/custodias/movimentar", vendaReq, ct);
        if (!respVenda.IsSuccessStatusCode)
            throw new DomainException("CUSTODIAS_FALHA", $"Falha ao vender {tickerVendido} para cliente {clienteId}.");

        var respCompra = await custodiasHttp.PostAsJsonAsync("/api/custodias/movimentar", compraReq, ct);
        if (!respCompra.IsSuccessStatusCode)
            throw new DomainException("CUSTODIAS_FALHA", $"Falha ao comprar {tickerComprado} para cliente {clienteId}.");

        // Persiste rebalanceamento
        var reb = new Rebalanceamento
        {
            ClienteId = clienteId,
            Tipo = tipo,
            TickerVendido = tickerVendido,
            TickerComprado = tickerComprado,
            ValorVenda = valorVenda,
            DataRebalanceamento = dataExecucao
        };

        db.Rebalanceamentos.Add(reb);

        var vendaRebalanceamento = new VendaRebalanceamento
        {
            ClienteId = clienteId,
            Ticker = tickerVendido,
            Quantidade = qtdVenda,
            PrecoVenda = precoVenda,
            PrecoMedio = precoMedio,
            ValorVenda = valorVenda,
            Lucro = lucroVendaAtual,
            DataOperacaoUtc = dataExecucao
        };

        db.VendasRebalanceamento.Add(vendaRebalanceamento);
        await db.SaveChangesAsync(ct);

        // Calcula IR de venda no mes
        var inicioMes = new DateTime(
            dataExecucao.Year,
            dataExecucao.Month,
            1,
            0,
            0,
            0,
            DateTimeKind.Utc);

        var inicioProximoMes = inicioMes.AddMonths(1);

        var vendasMes = await db.VendasRebalanceamento
            .Where(x =>
                x.ClienteId == clienteId &&
                x.DataOperacaoUtc >= inicioMes &&
                x.DataOperacaoUtc < inicioProximoMes)
            .ToListAsync(ct);

        var totalVendasMes = decimal.Round(vendasMes.Sum(x => x.ValorVenda), 2);
        var lucroLiquidoMes = decimal.Round(vendasMes.Sum(x => x.Lucro), 2);

        decimal valorIrVenda = 0m;

        if (totalVendasMes > 20000m && lucroLiquidoMes > 0m)
        {
            valorIrVenda = decimal.Round(lucroLiquidoMes * 0.20m, 2);

            var irVendaReq = new CriarIrVendaRebalanceamentoRequest
            {
                ClienteId = clienteId,
                MesReferencia = $"{dataExecucao:yyyy-MM}",
                TotalVendasMes = totalVendasMes,
                LucroLiquido = lucroLiquidoMes,
                ValorIR = valorIrVenda,
                DataCalculo = dataExecucao,
                Detalhes = vendasMes.Select(v => new IrVendaDetalheDto
                {
                    Ticker = v.Ticker,
                    Quantidade = v.Quantidade,
                    PrecoVenda = v.PrecoVenda,
                    PrecoMedio = v.PrecoMedio,
                    Lucro = v.Lucro
                }).ToList()
            };

            var respIrVenda = await eventosIrHttp.PostAsJsonAsync(
                "/api/eventos-ir/venda-rebalanceamento",
                irVendaReq,
                ct);

            if (!respIrVenda.IsSuccessStatusCode)
            {
                var body = await respIrVenda.Content.ReadAsStringAsync(ct);
                throw new DomainException(
                    "EVENTOS_IR_FALHA",
                    $"Falha ao registrar IR de venda do rebalanceamento. Status={(int)respIrVenda.StatusCode}. Body={body}");
            }
        }

        // Publica evento Kafka do rebalanceamento executado
        var topicOut = config["Kafka:TopicRebalanceamentos"] ?? "rebalanceamentos-eventos";

        var msgOut = new RebalanceamentoExecutadoMessage
        {
            Tipo = "REBALANCEAMENTO_EXECUTADO",
            ClienteId = clienteId,
            Motivo = tipo == TipoRebalanceamento.MUDANCA_CESTA ? "MUDANCA_CESTA" : "DESVIO",
            TickerVendido = tickerVendido,
            QuantidadeVendida = qtdVenda,
            PrecoVenda = precoVenda,
            ValorVenda = valorVenda,
            TickerComprado = tickerComprado,
            QuantidadeComprada = qtdCompra,
            PrecoCompra = precoCompra,
            DataExecucao = dataExecucao
        };

        await producer.ProduceAsync(
            topicOut,
            key: $"cliente:{clienteId}",
            payload: msgOut,
            ct);

        return new ExecucaoRebalanceamentoResult
        {
            ClienteId = clienteId,
            Executou = true,
            Mensagem = valorIrVenda > 0
                ? $"Rebalanceamento executado com IR de venda calculado: vendeu {qtdVenda} {tickerVendido} e comprou {qtdCompra} {tickerComprado}."
                : $"Rebalanceamento executado: vendeu {qtdVenda} {tickerVendido} e comprou {qtdCompra} {tickerComprado}.",
            ValorCarteira = decimal.Round(valorTotalCarteira, 2),
            MaiorDesvioAbsPercentual = decimal.Round(maiorDesvioAbs, 4),
            TickerVendido = tickerVendido,
            TickerComprado = tickerComprado,
            QuantidadeVendida = qtdVenda,
            QuantidadeComprada = qtdCompra,
            PrecoVenda = precoVenda,
            PrecoCompra = precoCompra,
            ValorVenda = valorVenda,
            ValorIrVendaRebalanceamento = valorIrVenda,
            TotalVendasMes = totalVendasMes,
            LucroLiquidoMes = lucroLiquidoMes
        };
    }
}