using CotacoesService.Api.Infrastructure.Cotahist;
using CotacoesService.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CotacoesService.Api.Controllers;

[ApiController]
[Route("api/cotacoes")]
public sealed class CotahistImportController(
    IConfiguration config,
    CotacoesDbContext db) : ControllerBase
{
    [HttpPost("importar-cotahist")]
    public async Task<IActionResult> Importar([FromQuery] DateTime dataPregao, CancellationToken ct)
    {
        var pasta = config["Cotacoes:PastaCotacoes"]
                    ?? throw new InvalidOperationException("Cotacoes:PastaCotacoes nao configurada.");

        var nome = $"COTAHIST_D{dataPregao:yyyyMMdd}.TXT";
        var caminho = Path.Combine(pasta, nome);

        if (!System.IO.File.Exists(caminho))
            return NotFound(new
            {
                erro = "Arquivo COTAHIST nao encontrado.",
                codigo = "COTACAO_NAO_ENCONTRADA",
                caminho
            });

        var parser = new CotahistParser();

        // Normaliza o ticker
        // mantem 1 linha por ticker com prioridade bem definida
        var registros = parser.ParseArquivo(caminho)
            .GroupBy(x => x.Ticker.Trim().ToUpperInvariant())
            .Select(g =>
            {
                // prioridade 1: Vista (10) + Lote padrão (02)
                var preferido = g.FirstOrDefault(x => x.TipoMercado == 10 && x.CodigoBDI == "02");

                // prioridade 2: Vista (10) + Fracionário (96)
                preferido ??= g.FirstOrDefault(x => x.TipoMercado == 10 && x.CodigoBDI == "96");

                // prioridade 3: Fracionário (20) + Lote padrão (02)
                preferido ??= g.FirstOrDefault(x => x.TipoMercado == 20 && x.CodigoBDI == "02");

                // prioridade 4: Fracionário (20) + Fracionário (96)
                preferido ??= g.FirstOrDefault(x => x.TipoMercado == 20 && x.CodigoBDI == "96");

                // fallback
                preferido ??= g.First();

                return preferido;
            })
            .ToList();

        if (registros.Count == 0)
            return BadRequest(new
            {
                erro = "Nenhum registro valido encontrado no COTAHIST (TIPREG=01, filtros).",
                codigo = "COTAHIST_VAZIO"
            });

        var tickers = registros.Select(x => x.Ticker.Trim().ToUpperInvariant()).Distinct().ToList();

        // Idempotencia por dia, remove do mesmo pregão e mesmos tickers
        var existentes = await db.Cotacoes
            .Where(x => x.DataPregao == dataPregao.Date && tickers.Contains(x.Ticker))
            .ToListAsync(ct);

        if (existentes.Count > 0)
            db.Cotacoes.RemoveRange(existentes);

        // Salva
        foreach (var r in registros)
        {
            db.Cotacoes.Add(new Domain.Entities.Cotacao
            {
                DataPregao = r.DataPregao.Date,
                Ticker = r.Ticker.Trim().ToUpperInvariant(), 
                PrecoAbertura = r.PrecoAbertura,
                PrecoFechamento = r.PrecoFechamento,
                PrecoMaximo = r.PrecoMaximo,
                PrecoMinimo = r.PrecoMinimo
            });
        }

        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            mensagem = "COTAHIST importado com sucesso.",
            dataPregao = dataPregao.ToString("yyyy-MM-dd"),
            tickersImportados = tickers.OrderBy(x => x).ToList(),
            totalTickersImportados = tickers.Count
        });
    }
}