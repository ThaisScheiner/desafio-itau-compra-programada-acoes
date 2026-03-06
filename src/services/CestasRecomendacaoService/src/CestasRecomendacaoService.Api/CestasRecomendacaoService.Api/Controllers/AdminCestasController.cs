using BuildingBlocks.Exceptions;
using CestasRecomendacaoService.Api.Domain.Entities;
using CestasRecomendacaoService.Api.Infrastructure.Kafka;
using CestasRecomendacaoService.Api.Infrastructure.Kafka.Messages;
using CestasRecomendacaoService.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CestasRecomendacaoService.Api.Controllers;

[ApiController]
[Route("api/admin/cesta")]
public sealed class AdminCestasController(
    CestasRecomendacaoDbContext db,                 
    IConfiguration config,
    IKafkaProducer producer) : ControllerBase
{
    // =========================================================
    // POST /api/admin/cesta
    // Cria nova cesta, desativa anterior, publica evento Kafka
    // =========================================================
    [HttpPost]
    public async Task<IActionResult> CriarNovaCesta([FromBody] CriarCestaRequest req, CancellationToken ct)
    {
        if (req is null)
            throw new DomainException("REQUEST_INVALIDO", "Body invalido.");

        var nome = (req.Nome ?? "").Trim();
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("NOME_INVALIDO", "Nome da cesta e obrigatorio.");

        if (req.Itens is null || req.Itens.Count != 5)
            throw new DomainException("QUANTIDADE_ATIVOS_INVALIDA", $"A cesta deve conter exatamente 5 ativos. Quantidade informada: {req.Itens?.Count ?? 0}.");

        // Normaliza tickers + valida
        static string N(string s) => (s ?? "").Trim().ToUpperInvariant();

        var itens = req.Itens
            .Select(i => new { Ticker = N(i.Ticker), Percentual = i.Percentual })
            .ToList();

        if (itens.Any(x => string.IsNullOrWhiteSpace(x.Ticker)))
            throw new DomainException("TICKER_INVALIDO", "Ticker invalido (vazio).");

        if (itens.GroupBy(x => x.Ticker, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))
            throw new DomainException("TICKER_DUPLICADO", "A cesta nao pode conter tickers duplicados.");

        if (itens.Any(x => x.Percentual <= 0))
            throw new DomainException("PERCENTUAL_INVALIDO", "Cada percentual deve ser maior que 0.");

        var soma = itens.Sum(x => x.Percentual);
        if (decimal.Round(soma, 2) != 100m)
            throw new DomainException("PERCENTUAIS_INVALIDOS", $"A soma dos percentuais deve ser exatamente 100%. Soma atual: {decimal.Round(soma, 2)}%.");

        // Busca cesta ativa atual (pode ser null)
        var cestaAnterior = await db.CestasRecomendacao
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Ativa, ct);

        // Transação simples
        using var tx = await db.Database.BeginTransactionAsync(ct);

        // Desativa anterior
        if (cestaAnterior is not null)
        {
            cestaAnterior.Ativa = false;
            cestaAnterior.DataDesativacao = DateTime.UtcNow;
        }

        // Cria nova cesta ativa
        var cestaNova = new CestaRecomendacao
        {
            Nome = nome,
            Ativa = true,
            DataCriacao = DateTime.UtcNow,
            DataDesativacao = null,
            Itens = itens.Select(x => new ItemCesta
            {
                Ticker = x.Ticker,
                Percentual = x.Percentual
            }).ToList()
        };

        db.CestasRecomendacao.Add(cestaNova);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        // ========= Kafka (CESTA_ALTERADA) =========
        var topic = config["Kafka:TopicCestas"] ?? "cestas-eventos";

        var novaTickers = cestaNova.Itens.Select(i => N(i.Ticker)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var anteriorTickers = (cestaAnterior?.Itens ?? new List<ItemCesta>())
            .Select(i => N(i.Ticker)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var removidos = anteriorTickers.Except(novaTickers, StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
        var adicionados = novaTickers.Except(anteriorTickers, StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();

        var evt = new CestaAlteradaMessage
        {
            CestaNovaId = cestaNova.Id,
            NomeCestaNova = cestaNova.Nome,
            DataCriacaoCestaNova = cestaNova.DataCriacao,

            CestaAnteriorId = cestaAnterior?.Id,
            NomeCestaAnterior = cestaAnterior?.Nome,
            DataDesativacaoCestaAnterior = cestaAnterior?.DataDesativacao,

            ItensNova = cestaNova.Itens.Select(i => new CestaItemMessage
            {
                Ticker = N(i.Ticker),
                Percentual = i.Percentual
            }).ToList(),

            AtivosRemovidos = removidos,
            AtivosAdicionados = adicionados
        };

        var publicadoKafka = false;
        string? erroKafka = null;

        try
        {
            await producer.ProduceAsync(topic, key: $"cesta:{cestaNova.Id}", payload: evt, ct);
            publicadoKafka = true;
        }
        catch (Exception ex)
        {
            erroKafka = ex.Message;
        }

        return Created("/api/admin/cesta/atual", new
        {
            cestaId = cestaNova.Id,
            nome = cestaNova.Nome,
            ativa = cestaNova.Ativa,
            dataCriacao = cestaNova.DataCriacao,
            itens = cestaNova.Itens.Select(i => new { ticker = i.Ticker, percentual = i.Percentual }),

            cestaAnteriorDesativada = cestaAnterior is null ? null : new
            {
                cestaId = cestaAnterior.Id,
                nome = cestaAnterior.Nome,
                dataDesativacao = cestaAnterior.DataDesativacao
            },

            rebalanceamentoDisparado = publicadoKafka, // evento publicado
            topicKafka = topic,
            publicadoKafka,
            erroKafka,
            ativosRemovidos = removidos,
            ativosAdicionados = adicionados,
            mensagem = cestaAnterior is null
                ? "Primeira cesta cadastrada com sucesso."
                : "Cesta atualizada. Evento CESTA_ALTERADA publicado (se Kafka estiver ok)."
        });
    }

    // =========================================================
    // GET /api/admin/cesta/atual
    // =========================================================
    [HttpGet("atual")]
    public async Task<IActionResult> Atual(CancellationToken ct)
    {
        var cesta = await db.CestasRecomendacao
            .Include(c => c.Itens)
            .Where(c => c.Ativa)
            .OrderByDescending(c => c.DataCriacao)
            .FirstOrDefaultAsync(ct);

        if (cesta is null)
            throw new DomainException("CESTA_NAO_ENCONTRADA", "Nenhuma cesta ativa encontrada.");

        return Ok(new
        {
            cestaId = cesta.Id,
            nome = cesta.Nome,
            ativa = cesta.Ativa,
            dataCriacao = cesta.DataCriacao,
            itens = cesta.Itens
                .OrderByDescending(i => i.Percentual)
                .Select(i => new { ticker = i.Ticker, percentual = i.Percentual })
        });
    }

    // =========================================================
    // GET /api/admin/cesta/historico
    // =========================================================
    [HttpGet("historico")]
    public async Task<IActionResult> Historico(CancellationToken ct)
    {
        var cestas = await db.CestasRecomendacao
            .Include(c => c.Itens)
            .OrderByDescending(c => c.DataCriacao)
            .ToListAsync(ct);

        return Ok(new
        {
            cestas = cestas.Select(c => new
            {
                cestaId = c.Id,
                nome = c.Nome,
                ativa = c.Ativa,
                dataCriacao = c.DataCriacao,
                dataDesativacao = c.DataDesativacao,
                itens = c.Itens.Select(i => new { ticker = i.Ticker, percentual = i.Percentual })
            })
        });
    }

    // ===================== Request DTOs ======================
    public sealed class CriarCestaRequest
    {
        public string? Nome { get; set; }
        public List<CestaItemRequest> Itens { get; set; } = new();
    }

    public sealed class CestaItemRequest
    {
        public string Ticker { get; set; } = string.Empty;
        public decimal Percentual { get; set; }
    }
}