using BuildingBlocks.Exceptions;
using EventosIRService.Api.Controllers.Requests;
using EventosIRService.Api.Domain.Entities;
using EventosIRService.Api.Domain.Enums;
using EventosIRService.Api.Infrastructure.Kafka;
using EventosIRService.Api.Infrastructure.Kafka.Messages;
using EventosIRService.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventosIRService.Api.Controllers;

[ApiController]
[Route("api/eventos-ir")]
public sealed class EventosIRController(
    EventosIRDbContext db,
    IConfiguration config,
    IKafkaProducer producer) : ControllerBase
{
    [HttpPost("dedo-duro")]
    public async Task<IActionResult> CriarDedoDuro([FromBody] CriarDedoDuroRequest req, CancellationToken ct)
    {
        var ticker = (req.Ticker ?? string.Empty).Trim().ToUpperInvariant();

        if (req.ClienteId <= 0)
            throw new DomainException("CLIENTE_INVALIDO", "ClienteId invalido.");

        if (string.IsNullOrWhiteSpace(ticker))
            throw new DomainException("TICKER_INVALIDO", "Ticker invalido.");

        if (req.Quantidade <= 0)
            throw new DomainException("QUANTIDADE_INVALIDA", "Quantidade deve ser maior que zero.");

        if (req.PrecoUnitario <= 0)
            throw new DomainException("PRECO_INVALIDO", "PrecoUnitario deve ser maior que zero.");

        if (req.ValorOperacao <= 0)
            throw new DomainException("VALOR_OPERACAO_INVALIDO", "ValorOperacao deve ser maior que zero.");

        if (req.ValorIR < 0)
            throw new DomainException("VALOR_IR_INVALIDO", "ValorIR nao pode ser negativo.");

        var dataEvento = req.DataOperacao.Date;

        // Idempotencia simples por cliente + tipo + ticker + data
        var exists = await db.EventosIR.AnyAsync(x =>
            x.Tipo == TipoEventoIR.DedoDuro &&
            x.ClienteId == req.ClienteId &&
            x.Ticker == ticker &&
            x.DataEvento == dataEvento, ct);

        if (exists)
        {
            return Ok(new
            {
                mensagem = "Evento de IR dedo-duro ja existente (idempotente)."
            });
        }

        var evento = new EventoIR
        {
            ClienteId = req.ClienteId,
            Tipo = TipoEventoIR.DedoDuro,
            Ticker = ticker,
            Quantidade = req.Quantidade,
            PrecoUnitario = req.PrecoUnitario,
            ValorBase = req.ValorOperacao,
            ValorIR = req.ValorIR,
            PublicadoKafka = false,
            DataEvento = dataEvento
        };

        db.EventosIR.Add(evento);
        await db.SaveChangesAsync(ct);

        var topic = config["Kafka:TopicIR"] ?? "ir-eventos";

        var msg = new IrDedoDuroMessage
        {
            ClienteId = evento.ClienteId,
            Ticker = evento.Ticker,
            Quantidade = evento.Quantidade,
            PrecoUnitario = evento.PrecoUnitario,
            ValorOperacao = evento.ValorBase,
            ValorIR = evento.ValorIR,
            DataOperacao = evento.DataEvento
        };

        try
        {
            await producer.ProduceAsync(topic, msg, ct);
            evento.PublicadoKafka = true;
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            throw new DomainException("KAFKA_INDISPONIVEL", $"Falha ao publicar no Kafka: {ex.Message}");
        }

        return Created($"/api/eventos-ir/{evento.Id}", new
        {
            eventoId = evento.Id,
            publicadoKafka = evento.PublicadoKafka,
            topic
        });
    }

    [HttpPost("venda-rebalanceamento")]
    public async Task<IActionResult> CriarIrVendaRebalanceamento(
        [FromBody] CriarVendaRebalanceamentoRequest req,
        CancellationToken ct)
    {
        if (req.ClienteId <= 0)
            throw new DomainException("CLIENTE_INVALIDO", "ClienteId invalido.");

        if (string.IsNullOrWhiteSpace(req.MesReferencia))
            throw new DomainException("MES_REFERENCIA_INVALIDO", "MesReferencia e obrigatorio. Ex: 2026-03");

        if (req.TotalVendasMes < 0)
            throw new DomainException("TOTAL_VENDAS_INVALIDO", "TotalVendasMes nao pode ser negativo.");

        if (req.LucroLiquido < 0)
            throw new DomainException("LUCRO_INVALIDO", "LucroLiquido nao pode ser negativo para este evento consolidado.");

        if (req.ValorIR < 0)
            throw new DomainException("VALOR_IR_INVALIDO", "ValorIR nao pode ser negativo.");

        var dataEvento = req.DataCalculo.Date;

        // Ticker fixo para diferenciar evento consolidado mensal de venda
        const string tickerEvento = "REBAL";

        // Idempotencia: cliente + tipo + mes/data
        var exists = await db.EventosIR.AnyAsync(x =>
            x.Tipo == TipoEventoIR.IrVendaRebalanceamento &&
            x.ClienteId == req.ClienteId &&
            x.Ticker == tickerEvento &&
            x.DataEvento == dataEvento, ct);

        if (exists)
        {
            return Ok(new
            {
                mensagem = "Evento de IR de venda/rebalanceamento ja existente (idempotente)."
            });
        }

        var evento = new EventoIR
        {
            ClienteId = req.ClienteId,
            Tipo = TipoEventoIR.IrVendaRebalanceamento,
            Ticker = tickerEvento,
            Quantidade = 0,
            PrecoUnitario = 0,
            ValorBase = req.TotalVendasMes,
            ValorIR = req.ValorIR,
            PublicadoKafka = false,
            DataEvento = dataEvento
        };

        db.EventosIR.Add(evento);
        await db.SaveChangesAsync(ct);

        var topic = config["Kafka:TopicIR"] ?? "ir-eventos";

        var msg = new IrVendaRebalanceamentoMessage
        {
            ClienteId = req.ClienteId,
            MesReferencia = req.MesReferencia,
            TotalVendasMes = req.TotalVendasMes,
            LucroLiquido = req.LucroLiquido,
            ValorIR = req.ValorIR,
            DataCalculo = req.DataCalculo
        };

        try
        {
            await producer.ProduceAsync(topic, msg, ct);
            evento.PublicadoKafka = true;
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            throw new DomainException("KAFKA_INDISPONIVEL", $"Falha ao publicar no Kafka: {ex.Message}");
        }

        return Created($"/api/eventos-ir/{evento.Id}", new
        {
            eventoId = evento.Id,
            publicadoKafka = evento.PublicadoKafka,
            topic
        });
    }

    [HttpGet("cliente/{clienteId:long}")]
    public async Task<IActionResult> ListarPorCliente(long clienteId, CancellationToken ct)
    {
        if (clienteId <= 0)
            throw new DomainException("CLIENTE_INVALIDO", "ClienteId invalido.");

        var eventos = await db.EventosIR
            .Where(x => x.ClienteId == clienteId)
            .OrderByDescending(x => x.DataEvento)
            .ThenByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                tipo = x.Tipo.ToString(),
                x.Ticker,
                x.Quantidade,
                x.PrecoUnitario,
                x.ValorBase,
                x.ValorIR,
                x.PublicadoKafka,
                x.DataEvento
            })
            .ToListAsync(ct);

        return Ok(new
        {
            clienteId,
            eventos
        });
    }
}