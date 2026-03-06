using BuildingBlocks.Exceptions;
using CustodiasService.Api.Controllers.Requests;
using CustodiasService.Api.Domain.Entities;
using CustodiasService.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustodiasService.Api.Controllers;

[ApiController]
[Route("api/custodias")]
public sealed class CustodiasController(CustodiasDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Consultar(
        [FromQuery] string tipoConta,
        [FromQuery] long? clienteId,
        [FromQuery] string ticker,
        CancellationToken ct)
    {
        var tConta = tipoConta.Trim().ToUpperInvariant();
        var t = ticker.Trim().ToUpperInvariant();

        if (tConta != "MASTER" && tConta != "FILHOTE")
            throw new DomainException("TIPO_CONTA_INVALIDO", "TipoConta deve ser MASTER ou FILHOTE.");

        if (tConta == "FILHOTE" && clienteId is null)
            throw new DomainException("CLIENTE_ID_OBRIGATORIO", "ClienteId é obrigatório para FILHOTE.");

        var pos = await db.Custodias.FirstOrDefaultAsync(x =>
            x.TipoConta == tConta &&
            x.ClienteId == (tConta == "MASTER" ? null : clienteId) &&
            x.Ticker == t, ct);

        if (pos is null)
            return Ok(new { tipoConta = tConta, clienteId, ticker = t, quantidade = 0, precoMedio = 0m });

        return Ok(new
        {
            tipoConta = pos.TipoConta,
            clienteId = pos.ClienteId,
            ticker = pos.Ticker,
            quantidade = pos.Quantidade,
            precoMedio = pos.PrecoMedio,
            atualizadoEm = pos.AtualizadoEm
        });
    }

    [HttpPost("movimentar")]
    public async Task<IActionResult> Movimentar([FromBody] MovimentacaoRequest req, CancellationToken ct)
    {
        var tipoConta = req.TipoConta.Trim().ToUpperInvariant();
        var ticker = req.Ticker.Trim().ToUpperInvariant();
        var op = req.TipoOperacao.Trim().ToUpperInvariant();

        if (tipoConta != "MASTER" && tipoConta != "FILHOTE")
            throw new DomainException("TIPO_CONTA_INVALIDO", "TipoConta deve ser MASTER ou FILHOTE.");

        if (tipoConta == "FILHOTE" && req.ClienteId is null)
            throw new DomainException("CLIENTE_ID_OBRIGATORIO", "ClienteId é obrigatório para FILHOTE.");

        if (op != "COMPRA" && op != "VENDA")
            throw new DomainException("TIPO_OPERACAO_INVALIDO", "TipoOperacao deve ser COMPRA ou VENDA.");

        if (req.Quantidade <= 0)
            throw new DomainException("QUANTIDADE_INVALIDA", "Quantidade deve ser maior que 0.");

        if (req.PrecoUnitario <= 0)
            throw new DomainException("PRECO_INVALIDO", "PrecoUnitario deve ser maior que 0.");

        var clienteId = tipoConta == "MASTER" ? null : req.ClienteId;
        var now = DateTime.UtcNow;

        var custodia = await db.Custodias.FirstOrDefaultAsync(x =>
            x.TipoConta == tipoConta &&
            x.ClienteId == clienteId &&
            x.Ticker == ticker, ct);

        if (custodia is null)
        {
            custodia = new Custodia
            {
                TipoConta = tipoConta,
                ClienteId = clienteId,
                Ticker = ticker,
                Quantidade = 0,
                PrecoMedio = 0m,
                AtualizadoEm = now
            };
            db.Custodias.Add(custodia);
        }

        if (op == "COMPRA")
        {
            // PM = (qtdAnt * pmAnt + qtdNova * preco) / (qtdAnt + qtdNova)
            var qtdAnt = custodia.Quantidade;
            var pmAnt = custodia.PrecoMedio;

            var qtdNova = req.Quantidade;
            var novoTotal = qtdAnt + qtdNova;

            var pmNovo = novoTotal == 0
                ? 0m
                : ((qtdAnt * pmAnt) + (qtdNova * req.PrecoUnitario)) / novoTotal;

            custodia.Quantidade = novoTotal;
            custodia.PrecoMedio = decimal.Round(pmNovo, 4);
        }
        else // VENDA
        {
            if (custodia.Quantidade < req.Quantidade)
                throw new DomainException("SALDO_INSUFICIENTE", "Quantidade em custodia insuficiente para venda.");

            custodia.Quantidade -= req.Quantidade;

            // PM NÃO MUDA em venda
            if (custodia.Quantidade == 0)
                custodia.PrecoMedio = custodia.PrecoMedio; // mantém (ou poderia zerar; mas no desafio PM não muda em venda)
        }

        custodia.AtualizadoEm = now;
        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            tipoConta = custodia.TipoConta,
            clienteId = custodia.ClienteId,
            ticker = custodia.Ticker,
            quantidade = custodia.Quantidade,
            precoMedio = custodia.PrecoMedio,
            atualizadoEm = custodia.AtualizadoEm
        });
    }

    //listar posicoes do cliente
    [HttpGet("cliente/{clienteId:long}")]
    public async Task<IActionResult> ListarPorCliente(long clienteId, CancellationToken ct)
    {
        var posicoes = await db.Custodias
            .Where(x => x.TipoConta == "FILHOTE" && x.ClienteId == clienteId && x.Quantidade > 0)
            .OrderBy(x => x.Ticker)
            .Select(x => new
            {
                ticker = x.Ticker,
                quantidade = x.Quantidade,
                precoMedio = x.PrecoMedio
            })
            .ToListAsync(ct);

        return Ok(new { clienteId, posicoes });
    }

    //listar posicoes master
    [HttpGet("master")]
    public async Task<IActionResult> ListarMaster(CancellationToken ct)
    {
        var posicoes = await db.Custodias
            .Where(x => x.TipoConta == "MASTER" && x.Quantidade > 0)
            .OrderBy(x => x.Ticker)
            .Select(x => new { ticker = x.Ticker, quantidade = x.Quantidade, precoMedio = x.PrecoMedio })
            .ToListAsync(ct);

        return Ok(new { tipoConta = "MASTER", posicoes });
    }
}