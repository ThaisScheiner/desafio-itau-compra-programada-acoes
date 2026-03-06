using System.Net.Http.Json;
using BuildingBlocks.Exceptions;
using ClientesService.Api.Controllers.Dto;
using ClientesService.Api.Controllers.Requests;
using ClientesService.Api.Infrastructure.HttpClients.Dto;
using ClientesService.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClientesService.Api.Controllers;

[ApiController]
[Route("api/clientes")]
public sealed class ClientesController(
    ClientesDbContext db,
    IHttpClientFactory httpClientFactory) : ControllerBase
{
    // =========================================================
    // Adesão ao produto
    // POST /api/clientes/adesao
    // =========================================================
    [HttpPost("adesao")]
    public async Task<IActionResult> Adesao([FromBody] AdesaoRequest req, CancellationToken ct)
    {
        var cpf = (req.Cpf ?? "").Trim();
        if (cpf.Length != 11 || !cpf.All(char.IsDigit))
            throw new DomainException("CPF_INVALIDO", "CPF deve ter 11 digitos numericos.");

        if (req.ValorMensal < 100m)
            throw new DomainException("VALOR_MENSAL_INVALIDO", "O valor mensal minimo e de R$ 100,00.");

        var existeCpf = await db.Clientes.AnyAsync(x => x.CPF == cpf, ct);
        if (existeCpf)
            throw new DomainException("CLIENTE_CPF_DUPLICADO", "CPF ja cadastrado no sistema.");

        var cliente = new Domain.Entities.Cliente
        {
            Nome = (req.Nome ?? "").Trim(),
            CPF = cpf,
            Email = (req.Email ?? "").Trim(),
            ValorMensal = req.ValorMensal,
            Ativo = true,
            DataAdesao = DateTime.UtcNow,
            DataSaida = null
        };

        db.Clientes.Add(cliente);
        await db.SaveChangesAsync(ct);

        var conta = new Domain.Entities.ContaGrafica
        {
            ClienteId = cliente.Id,
            NumeroConta = $"FLH-{cliente.Id:000000}",
            Tipo = "FILHOTE",
            DataCriacao = DateTime.UtcNow
        };

        db.ContasGraficas.Add(conta);
        await db.SaveChangesAsync(ct);

        return Created($"/api/clientes/{cliente.Id}", new
        {
            clienteId = cliente.Id,
            nome = cliente.Nome,
            cpf = cliente.CPF,
            email = cliente.Email,
            valorMensal = cliente.ValorMensal,
            ativo = cliente.Ativo,
            dataAdesao = cliente.DataAdesao,
            dataSaida = cliente.DataSaida,
            contaGrafica = new
            {
                id = conta.Id,
                numeroConta = conta.NumeroConta,
                tipo = conta.Tipo,
                dataCriacao = conta.DataCriacao
            }
        });
    }

    // =========================================================
    // Saída do produto
    // POST /api/clientes/{clienteId}/saida
    // =========================================================
    [HttpPost("{clienteId:long}/saida")]
    public async Task<IActionResult> Saida(long clienteId, CancellationToken ct)
    {
        var cliente = await db.Clientes.FirstOrDefaultAsync(x => x.Id == clienteId, ct);
        if (cliente is null)
            throw new DomainException("CLIENTE_NAO_ENCONTRADO", "Cliente nao encontrado.");

        if (!cliente.Ativo)
            throw new DomainException("CLIENTE_JA_INATIVO", "Cliente ja estava inativo.");

        cliente.Ativo = false;
        cliente.DataSaida = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            clienteId = cliente.Id,
            nome = cliente.Nome,
            ativo = cliente.Ativo,
            dataSaida = cliente.DataSaida,
            mensagem = "Adesao encerrada. Sua posicao em custodia foi mantida."
        });
    }

    // =========================================================
    // Alterar valor mensal
    // PUT /api/clientes/{clienteId}/valor-mensal
    // =========================================================
    [HttpPut("{clienteId:long}/valor-mensal")]
    public async Task<IActionResult> AlterarValorMensal(long clienteId, [FromBody] AlterarValorMensalRequest req, CancellationToken ct)
    {
        if (req.NovoValorMensal < 100m)
            throw new DomainException("VALOR_MENSAL_INVALIDO", "O valor mensal minimo e de R$ 100,00.");

        var cliente = await db.Clientes.FirstOrDefaultAsync(x => x.Id == clienteId, ct);
        if (cliente is null)
            throw new DomainException("CLIENTE_NAO_ENCONTRADO", "Cliente nao encontrado.");

        var anterior = cliente.ValorMensal;
        cliente.ValorMensal = req.NovoValorMensal;

        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            clienteId = cliente.Id,
            valorMensalAnterior = anterior,
            valorMensalNovo = cliente.ValorMensal,
            dataAlteracao = DateTime.UtcNow,
            mensagem = "Valor mensal atualizado. O novo valor sera considerado a partir da proxima data de compra."
        });
    }

    // =========================================================
    // Endpoint usado pelo MotorCompraService
    // GET /api/clientes/ativos
    // =========================================================
    [HttpGet("ativos")]
    public async Task<IActionResult> Ativos(CancellationToken ct)
    {
        var clientes = await db.Clientes
            .Where(x => x.Ativo)
            .OrderBy(x => x.Id)
            .Select(x => new
            {
                clienteId = x.Id,
                cpf = x.CPF,
                nome = x.Nome,
                valorMensal = x.ValorMensal
            })
            .ToListAsync(ct);

        return Ok(new { clientes });
    }

    // =========================================================
    // Consultar carteira
    // GET /api/clientes/{clienteId}/carteira
    // =========================================================
    [HttpGet("{clienteId:long}/carteira")]
    public async Task<ActionResult<CarteiraResponse>> Carteira(long clienteId, CancellationToken ct)
    {
        var cliente = await db.Clientes
            .Include(c => c.ContaGrafica)
            .FirstOrDefaultAsync(c => c.Id == clienteId, ct);

        if (cliente is null)
            throw new DomainException("CLIENTE_NAO_ENCONTRADO", "Cliente nao encontrado.");

        var custodiasHttp = httpClientFactory.CreateClient("CustodiasService");
        var cotacoesHttp = httpClientFactory.CreateClient("CotacoesService");
        var motorHttp = httpClientFactory.CreateClient("MotorCompraService");

        // Custódia do cliente
        var custodia = await custodiasHttp.GetFromJsonAsync<CustodiasClientDtos>(
            $"/api/custodias/cliente/{clienteId}", ct);

        var posicoes = custodia?.Posicoes ?? new List<CustodiaPosicaoDto>();

        // Total aportado (histórico de aportes)
        var aportes = await motorHttp.GetFromJsonAsync<MotorAportesClientDtos>(
            $"/api/motor/aportes/{clienteId}", ct);

        var historicoAportes = aportes?.HistoricoAportes ?? new List<MotorAporteItemDto>();
        var totalAportado = historicoAportes.Sum(x => x.Valor);

        // Monta ativos com cotação atual (último fechamento)
        var ativos = new List<CarteiraAtivoDto>();
        decimal valorAtualCarteira = 0m;

        foreach (var p in posicoes)
        {
            var ticker = (p.Ticker ?? "").Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(ticker))
                continue;

            var cot = await cotacoesHttp.GetFromJsonAsync<UltimoFechamentoResponse>(
                $"/api/cotacoes/fechamento/ultimo?ticker={Uri.EscapeDataString(ticker)}", ct);

            var cotacaoAtual = cot?.PrecoFechamento ?? 0m;

            var valorAtual = decimal.Round(p.Quantidade * cotacaoAtual, 2);
            valorAtualCarteira += valorAtual;

            var pl = decimal.Round((cotacaoAtual - p.PrecoMedio) * p.Quantidade, 2);
            var plPct = p.PrecoMedio > 0
                ? decimal.Round(((cotacaoAtual - p.PrecoMedio) / p.PrecoMedio) * 100m, 2)
                : 0m;

            ativos.Add(new CarteiraAtivoDto
            {
                Ticker = ticker,
                Quantidade = p.Quantidade,
                PrecoMedio = decimal.Round(p.PrecoMedio, 4),
                CotacaoAtual = decimal.Round(cotacaoAtual, 4),
                ValorAtual = valorAtual,
                Pl = pl,
                PlPercentual = plPct,
                ComposicaoCarteira = 0m
            });
        }

        // Composição %
        foreach (var a in ativos)
        {
            a.ComposicaoCarteira = valorAtualCarteira > 0
                ? decimal.Round((a.ValorAtual / valorAtualCarteira) * 100m, 2)
                : 0m;
        }

        // P/L e Rentabilidade devem comparar contra o custo da posição (Σ qtd * precoMedio),
        // não contra o total aportado (porque pode existir "caixa não investido" por truncamento).
        var totalAplicado = decimal.Round(ativos.Sum(a => a.Quantidade * a.PrecoMedio), 2);
        var plTotal = decimal.Round(valorAtualCarteira - totalAplicado, 2);

        var rentPct = totalAplicado > 0
            ? decimal.Round((plTotal / totalAplicado) * 100m, 2)
            : 0m;

        var resp = new CarteiraResponse
        {
            ClienteId = cliente.Id,
            Nome = cliente.Nome,
            ContaGrafica = cliente.ContaGrafica?.NumeroConta ?? "N/A",
            DataConsulta = DateTime.UtcNow,
            Resumo = new CarteiraResumoDto
            {
                // Mantemos como "total aportado" (contrato existente)
                ValorTotalInvestido = decimal.Round(totalAportado, 2),
                ValorAtualCarteira = decimal.Round(valorAtualCarteira, 2),
                PlTotal = plTotal,
                RentabilidadePercentual = rentPct
            },
            Ativos = ativos
                .OrderByDescending(x => x.ComposicaoCarteira)
                .ToList()
        };

        return Ok(resp);
    }

    // =========================================================
    // Rentabilidade detalhada
    // GET /api/clientes/{clienteId}/rentabilidade
    // =========================================================
    [HttpGet("{clienteId:long}/rentabilidade")]
    public async Task<ActionResult<RentabilidadeResponse>> Rentabilidade(long clienteId, CancellationToken ct)
    {
        var cliente = await db.Clientes.FirstOrDefaultAsync(c => c.Id == clienteId, ct);
        if (cliente is null)
            throw new DomainException("CLIENTE_NAO_ENCONTRADO", "Cliente nao encontrado.");

        var motorHttp = httpClientFactory.CreateClient("MotorCompraService");

        // Histórico de aportes
        var aportes = await motorHttp.GetFromJsonAsync<MotorAportesClientDtos>(
            $"/api/motor/aportes/{clienteId}", ct);

        var historico = (aportes?.HistoricoAportes ?? new List<MotorAporteItemDto>())
            .OrderBy(x => x.Data)
            .Select(x => new HistoricoAporteDto
            {
                Data = x.Data,
                Valor = x.Valor,
                Parcela = x.Parcela
            })
            .ToList();

        // Snapshot atual da carteira (reaproveita cálculo)
        var carteiraResult = await Carteira(clienteId, ct);
        if (carteiraResult.Result is not OkObjectResult ok || ok.Value is not CarteiraResponse carteira)
            throw new DomainException("CARTEIRA_INDISPONIVEL", "Nao foi possivel calcular carteira para rentabilidade.");

        var totalAportado = carteira.Resumo.ValorTotalInvestido;
        var valorAtual = carteira.Resumo.ValorAtualCarteira;

        // rentabilidade / PL devem bater com a regra do snapshot: custo da posição (Σ qtd * precoMedio)
        var totalAplicado = decimal.Round(carteira.Ativos.Sum(x => x.Quantidade * x.PrecoMedio), 2);
        var plTotal = decimal.Round(valorAtual - totalAplicado, 2);

        var rentPct = totalAplicado > 0
            ? decimal.Round((plTotal / totalAplicado) * 100m, 2)
            : 0m;

        // mantém snapshot atual repetido (não histórico real de mercado)
        var evolucao = new List<EvolucaoCarteiraDto>();
        decimal acumulado = 0m;

        foreach (var a in historico)
        {
            acumulado += a.Valor;

            evolucao.Add(new EvolucaoCarteiraDto
            {
                Data = a.Data,
                ValorInvestido = decimal.Round(acumulado, 2),
                ValorCarteira = decimal.Round(valorAtual, 2),
                Rentabilidade = rentPct
            });
        }

        var resp = new RentabilidadeResponse
        {
            ClienteId = cliente.Id,
            Nome = cliente.Nome,
            DataConsulta = DateTime.UtcNow,
            Rentabilidade = new RentabilidadeResumoDto
            {
                // Mantem o campo como "aportado" para não quebrar contrato
                ValorTotalInvestido = totalAportado,
                ValorAtualCarteira = valorAtual,
                PlTotal = plTotal,
                RentabilidadePercentual = rentPct
            },
            HistoricoAportes = historico,
            EvolucaoCarteira = evolucao
        };

        return Ok(resp);
    }
}