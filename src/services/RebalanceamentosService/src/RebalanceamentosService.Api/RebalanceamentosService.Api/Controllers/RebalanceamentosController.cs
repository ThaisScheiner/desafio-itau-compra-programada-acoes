using System.Net.Http.Json;
using BuildingBlocks.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Polly.CircuitBreaker;
using Polly.Timeout;
using RebalanceamentosService.Api.Controllers.Dto;
using RebalanceamentosService.Api.Controllers.Requests;
using RebalanceamentosService.Api.Domain.Entities;
using RebalanceamentosService.Api.Domain.Enums;
using RebalanceamentosService.Api.Infrastructure.HttpClients.Dto;
using RebalanceamentosService.Api.Infrastructure.Persistence;

namespace RebalanceamentosService.Api.Controllers;

[ApiController]
[Route("api/rebalanceamentos")]
public sealed class RebalanceamentosController(
    RebalanceamentosDbContext db,
    IHttpClientFactory httpClientFactory) : ControllerBase
{
    // =========================================================
    // POST /api/rebalanceamentos/executar/{clienteId}
    // =========================================================
    [HttpPost("executar/{clienteId:long}")]
    public async Task<ActionResult<ExecutarRebalanceamentoResponse>> Executar(
        long clienteId,
        [FromBody] ExecutarRebalanceamentoRequest req,
        CancellationToken ct)
    {
        if (req.LimiteDesvioPercentual <= 0)
            throw new DomainException("LIMITE_INVALIDO", "LimiteDesvioPercentual deve ser > 0.");

        if (req.PercentualMovimentacao <= 0 || req.PercentualMovimentacao > 100)
            throw new DomainException("PERCENTUAL_INVALIDO", "PercentualMovimentacao deve estar entre 0 e 100.");

        var cestasHttp = httpClientFactory.CreateClient("CestasRecomendacaoService");
        var cotacoesHttp = httpClientFactory.CreateClient("CotacoesService");
        var custodiasHttp = httpClientFactory.CreateClient("CustodiasService");

        // Cesta atual (alvo)

        CestaDto? cesta;

        try
        {
            cesta = await cestasHttp.GetFromJsonAsync<CestaDto>("/api/admin/cesta/atual", ct);
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

        // Map alvo (%)
        var alvo = cesta.Itens
            .Where(i => !string.IsNullOrWhiteSpace(i.Ticker))
            .ToDictionary(
                i => i.Ticker.Trim().ToUpperInvariant(),
                i => decimal.Round(i.Percentual, 4),
                StringComparer.OrdinalIgnoreCase
            );

        // Custodia do cliente (posição atual)
        var custodia = await custodiasHttp.GetFromJsonAsync<CustodiasClientResponse>(
            $"/api/custodias/cliente/{clienteId}", ct);

        var posicoes = custodia?.Posicoes ?? new List<CustodiaPosicaoDto>();

        // Se estiver sem nada em custodia, nao tem o que rebalancear
        if (posicoes.Count == 0)
        {
            return Ok(new ExecutarRebalanceamentoResponse
            {
                ClienteId = clienteId,
                Tipo = req.Tipo,
                DataExecucao = DateTime.UtcNow,
                LimiteDesvioPercentual = req.LimiteDesvioPercentual,
                Executou = false,
                Mensagem = "Cliente sem posições em custódia. Nada a rebalancear."
            });
        }

        // Cotação atual dos tickers da cesta (pra valorizar carteira)
        var precoPorTicker = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var t in alvo.Keys)
        {
            var cot = await cotacoesHttp.GetFromJsonAsync<UltimoFechamentoResponse>(
                $"/api/cotacoes/fechamento/ultimo?ticker={Uri.EscapeDataString(t)}", ct);

            var preco = cot?.PrecoFechamento ?? 0m;
            if (preco <= 0)
                throw new DomainException("COTACAO_NAO_ENCONTRADA", $"Cotacao invalida para {t}.");

            precoPorTicker[t] = preco;
        }

        // Valor atual da carteira por ticker (considerando somente tickers da cesta)
        var valorPorTicker = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        decimal valorTotalCarteira = 0m;

        foreach (var p in posicoes)
        {
            var t = p.Ticker.Trim().ToUpperInvariant();
            if (!alvo.ContainsKey(t)) continue; 

            var preco = precoPorTicker[t];
            var valor = decimal.Round(p.Quantidade * preco, 2);

            valorPorTicker[t] = (valorPorTicker.TryGetValue(t, out var acc) ? acc : 0m) + valor;
            valorTotalCarteira += valor;
        }

        if (valorTotalCarteira <= 0)
        {
            return Ok(new ExecutarRebalanceamentoResponse
            {
                ClienteId = clienteId,
                Tipo = req.Tipo,
                DataExecucao = DateTime.UtcNow,
                LimiteDesvioPercentual = req.LimiteDesvioPercentual,
                Executou = false,
                Mensagem = "Carteira com valor total 0 (sem ativos da cesta). Nada a rebalancear."
            });
        }

        // Diagnostico: percentual atual vs alvo e desvio
        var diagnostico = new List<DiagnosticoTickerDto>();
        decimal maiorDesvioAbs = 0m;

        foreach (var t in alvo.Keys.OrderBy(x => x))
        {
            var pctAlvo = alvo[t]; 
            var valorAtual = valorPorTicker.TryGetValue(t, out var v) ? v : 0m;
            var pctAtual = decimal.Round((valorAtual / valorTotalCarteira) * 100m, 4);
            var desvio = decimal.Round(pctAtual - pctAlvo, 4); // + acima, - abaixo

            maiorDesvioAbs = Math.Max(maiorDesvioAbs, Math.Abs(desvio));

            diagnostico.Add(new DiagnosticoTickerDto
            {
                Ticker = t,
                PercentualAlvo = pctAlvo,
                PercentualAtual = pctAtual,
                DesvioPercentual = desvio,
                ValorAtual = decimal.Round(valorAtual, 2)
            });
        }

        if (maiorDesvioAbs < req.LimiteDesvioPercentual)
        {
            return Ok(new ExecutarRebalanceamentoResponse
            {
                ClienteId = clienteId,
                Tipo = req.Tipo,
                DataExecucao = DateTime.UtcNow,
                LimiteDesvioPercentual = req.LimiteDesvioPercentual,
                ValorCarteira = decimal.Round(valorTotalCarteira, 2),
                PercentualMovimentacao = req.PercentualMovimentacao,
                ValorMovimentado = 0m,
                MaiorDesvioAbsPercentual = decimal.Round(maiorDesvioAbs, 4),
                Diagnostico = diagnostico,
                Executou = false,
                Mensagem = $"Maior desvio ({maiorDesvioAbs:0.####}%) abaixo do limite ({req.LimiteDesvioPercentual:0.####}%)."
            });
        }

        // Escolher 1 ticker pra vender (mais acima) e 1 pra comprar (mais abaixo)
        var vender = diagnostico.OrderByDescending(x => x.DesvioPercentual).First();
        var comprar = diagnostico.OrderBy(x => x.DesvioPercentual).First();

        if (vender.DesvioPercentual <= 0 || comprar.DesvioPercentual >= 0)
        {
            // nao achou par coerente (deveria ser raro)
            return Ok(new ExecutarRebalanceamentoResponse
            {
                ClienteId = clienteId,
                Tipo = req.Tipo,
                DataExecucao = DateTime.UtcNow,
                LimiteDesvioPercentual = req.LimiteDesvioPercentual,
                ValorCarteira = decimal.Round(valorTotalCarteira, 2),
                PercentualMovimentacao = req.PercentualMovimentacao,
                ValorMovimentado = 0m,
                MaiorDesvioAbsPercentual = decimal.Round(maiorDesvioAbs, 4),
                Diagnostico = diagnostico,
                Executou = false,
                Mensagem = "Nao foi possivel determinar par de compra/venda (desvios inconsistentes)."
            });
        }

        var tickerVendido = vender.Ticker;
        var tickerComprado = comprar.Ticker;

        var precoVenda = precoPorTicker[tickerVendido];
        var precoCompra = precoPorTicker[tickerComprado];

        // Definir quanto movimentar (ex.: 10% da carteira)
        var valorMovimentar = decimal.Round((valorTotalCarteira * (req.PercentualMovimentacao / 100m)), 2);
        if (valorMovimentar <= 0)
            throw new DomainException("VALOR_MOVIMENTACAO_INVALIDO", "Valor de movimentacao calculado <= 0.");

        // Quantidade a vender (limitada pela posição do cliente)
        var qtdClienteVender = posicoes
            .Where(p => p.Ticker.Trim().Equals(tickerVendido, StringComparison.OrdinalIgnoreCase))
            .Sum(p => p.Quantidade);

        var qtdVenda = (int)decimal.Floor(valorMovimentar / precoVenda);
        qtdVenda = Math.Min(qtdVenda, qtdClienteVender);

        if (qtdVenda <= 0)
        {
            return Ok(new ExecutarRebalanceamentoResponse
            {
                ClienteId = clienteId,
                Tipo = req.Tipo,
                DataExecucao = DateTime.UtcNow,
                LimiteDesvioPercentual = req.LimiteDesvioPercentual,
                ValorCarteira = decimal.Round(valorTotalCarteira, 2),
                PercentualMovimentacao = req.PercentualMovimentacao,
                ValorMovimentado = 0m,
                MaiorDesvioAbsPercentual = decimal.Round(maiorDesvioAbs, 4),
                Diagnostico = diagnostico,
                Executou = false,
                Mensagem = $"QtdVenda calculada = 0 (preco alto ou sem quantidade no ticker {tickerVendido})."
            });
        }

        var valorVenda = decimal.Round(qtdVenda * precoVenda, 2);

        // Quantidade a comprar com o valor da venda
        var qtdCompra = (int)decimal.Floor(valorVenda / precoCompra);
        if (qtdCompra <= 0)
        {
            return Ok(new ExecutarRebalanceamentoResponse
            {
                ClienteId = clienteId,
                Tipo = req.Tipo,
                DataExecucao = DateTime.UtcNow,
                LimiteDesvioPercentual = req.LimiteDesvioPercentual,
                ValorCarteira = decimal.Round(valorTotalCarteira, 2),
                PercentualMovimentacao = req.PercentualMovimentacao,
                ValorMovimentado = 0m,
                MaiorDesvioAbsPercentual = decimal.Round(maiorDesvioAbs, 4),
                Diagnostico = diagnostico,
                Executou = false,
                Mensagem = $"QtdCompra calculada = 0 (preco alto no ticker {tickerComprado})."
            });
        }

        // Movimentar custodia FILHOTE (VENDA -> COMPRA)
        var dataExecucao = DateTime.UtcNow;

        var vendaReq = new MovimentacaoCustodiaRequest
        {
            TipoConta = "FILHOTE",
            ClienteId = clienteId,
            Ticker = tickerVendido,
            TipoOperacao = "VENDA",
            Quantidade = qtdVenda,
            PrecoUnitario = precoVenda,
            DataExecucao = dataExecucao,
            Origem = $"Rebalanceamento {req.Tipo} (VENDA) {dataExecucao:yyyy-MM-dd}"
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
            Origem = $"Rebalanceamento {req.Tipo} (COMPRA) {dataExecucao:yyyy-MM-dd}"
        };

        var respVenda = await custodiasHttp.PostAsJsonAsync("/api/custodias/movimentar", vendaReq, ct);
        if (!respVenda.IsSuccessStatusCode)
            throw new DomainException("CUSTODIAS_FALHA", $"Falha ao vender {tickerVendido} para cliente {clienteId}.");

        var respCompra = await custodiasHttp.PostAsJsonAsync("/api/custodias/movimentar", compraReq, ct);
        if (!respCompra.IsSuccessStatusCode)
            throw new DomainException("CUSTODIAS_FALHA", $"Falha ao comprar {tickerComprado} para cliente {clienteId}.");

        // Persistir rebalanceamento (registro)
        var reb = new Rebalanceamento
        {
            ClienteId = clienteId,
            Tipo = req.Tipo,
            TickerVendido = tickerVendido,
            TickerComprado = tickerComprado,
            ValorVenda = valorVenda,
            DataRebalanceamento = dataExecucao
        };

        db.Rebalanceamentos.Add(reb);
        await db.SaveChangesAsync(ct);

        return Ok(new ExecutarRebalanceamentoResponse
        {
            ClienteId = clienteId,
            Tipo = req.Tipo,
            DataExecucao = dataExecucao,
            LimiteDesvioPercentual = req.LimiteDesvioPercentual,
            ValorCarteira = decimal.Round(valorTotalCarteira, 2),
            PercentualMovimentacao = req.PercentualMovimentacao,
            ValorMovimentado = valorVenda,
            TickerVendido = tickerVendido,
            QuantidadeVendida = qtdVenda,
            PrecoVenda = precoVenda,
            ValorVenda = valorVenda,
            TickerComprado = tickerComprado,
            QuantidadeComprada = qtdCompra,
            PrecoCompra = precoCompra,
            MaiorDesvioAbsPercentual = decimal.Round(maiorDesvioAbs, 4),
            Diagnostico = diagnostico,
            Executou = true,
            Mensagem = $"Rebalanceamento executado: vendeu {qtdVenda} {tickerVendido} e comprou {qtdCompra} {tickerComprado}."
        });
    }

    // =========================================================
    // GET /api/rebalanceamentos/cliente/{clienteId}
    // =========================================================
    [HttpGet("cliente/{clienteId:long}")]
    public async Task<IActionResult> ListarPorCliente(long clienteId, CancellationToken ct)
    {
        var lista = await db.Rebalanceamentos
            .Where(x => x.ClienteId == clienteId)
            .OrderByDescending(x => x.DataRebalanceamento)
            .Select(x => new
            {
                x.Id,
                x.ClienteId,
                tipo = x.Tipo.ToString(),
                x.TickerVendido,
                x.TickerComprado,
                x.ValorVenda,
                x.DataRebalanceamento
            })
            .ToListAsync(ct);

        return Ok(new { clienteId, rebalanceamentos = lista });
    }
}