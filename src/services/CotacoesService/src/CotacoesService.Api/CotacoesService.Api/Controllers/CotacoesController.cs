using BuildingBlocks.Exceptions;
using CotacoesService.Api.Controllers.Requests;
using CotacoesService.Api.Domain.Entities;
using CotacoesService.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CotacoesService.Api.Controllers;

[ApiController]
[Route("api/cotacoes")]
public sealed class CotacoesController(CotacoesDbContext db) : ControllerBase
{
    // ===== DTOs de resposta =====
    public sealed class UpsertCotacaoResponse
    {
        public string Ticker { get; set; } = string.Empty;
        public DateTime DataPregao { get; set; }
        public decimal PrecoFechamento { get; set; }
    }

    public sealed class UltimoFechamentoResponse
    {
        public string Ticker { get; set; } = string.Empty;
        public DateTime DataPregao { get; set; }
        public decimal PrecoFechamento { get; set; }
    }

    // Endpoint para popular cotação (manual no MVP)
    [HttpPost("upsert")]
    public async Task<ActionResult<UpsertCotacaoResponse>> Upsert([FromBody] UpsertCotacaoRequest req, CancellationToken ct)
    {
        var ticker = (req.Ticker ?? "").Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(ticker))
            throw new DomainException("TICKER_INVALIDO", "Ticker invalido.");

        var data = req.DataPregao.Date;

        var existente = await db.Cotacoes
            .FirstOrDefaultAsync(x => x.Ticker == ticker && x.DataPregao == data, ct);

        if (existente is null)
        {
            existente = new Cotacao
            {
                DataPregao = data,
                Ticker = ticker
            };
            db.Cotacoes.Add(existente);
        }

        existente.PrecoAbertura = req.PrecoAbertura;
        existente.PrecoFechamento = req.PrecoFechamento;
        existente.PrecoMaximo = req.PrecoMaximo;
        existente.PrecoMinimo = req.PrecoMinimo;

        await db.SaveChangesAsync(ct);

        return Ok(new UpsertCotacaoResponse
        {
            Ticker = existente.Ticker,
            DataPregao = existente.DataPregao,
            PrecoFechamento = existente.PrecoFechamento
        });
    }

    // Consulta do fechamento mais recente (usado pelo MotorCompra)
    // GET /api/cotacoes/fechamento/ultimo?ticker=BBDC4
    [HttpGet("fechamento/ultimo")]
    public async Task<ActionResult<UltimoFechamentoResponse>> UltimoFechamento([FromQuery] string ticker, CancellationToken ct)
    {
        var t = (ticker ?? "").Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(t))
            throw new DomainException("TICKER_INVALIDO", "Ticker invalido.");

        // 1) tenta exatamente o ticker informado
        var cot = await BuscarUltimoFechamentoPorTicker(t, ct);

        // 2) fallback: se vier com sufixo F (fracionário), tenta sem o F (PETR4F -> PETR4)
        if (cot is null && t.EndsWith('F') && t.Length >= 5)
        {
            var semF = t[..^1];
            cot = await BuscarUltimoFechamentoPorTicker(semF, ct);
        }

        if (cot is null)
            throw new DomainException("COTACAO_NAO_ENCONTRADA", $"Cotacao nao encontrada para ticker {t}.");

        return Ok(new UltimoFechamentoResponse
        {
            Ticker = cot.Ticker,
            DataPregao = cot.DataPregao,
            PrecoFechamento = cot.PrecoFechamento
        });
    }

    private async Task<Cotacao?> BuscarUltimoFechamentoPorTicker(string ticker, CancellationToken ct)
    {
        return await db.Cotacoes
            .Where(x => x.Ticker == ticker)
            .OrderByDescending(x => x.DataPregao)
            .FirstOrDefaultAsync(ct);
    }
}