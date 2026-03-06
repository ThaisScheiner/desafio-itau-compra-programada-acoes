namespace RebalanceamentosService.Api.Infrastructure.HttpClients.Dto;

public sealed class CriarIrVendaRebalanceamentoRequest
{
    public long ClienteId { get; set; }

    public string MesReferencia { get; set; } = string.Empty;

    public decimal TotalVendasMes { get; set; }

    public decimal LucroLiquido { get; set; }

    public decimal ValorIR { get; set; }

    public DateTime DataCalculo { get; set; }

    public List<IrVendaDetalheDto> Detalhes { get; set; } = new();
}

public sealed class IrVendaDetalheDto
{
    public string Ticker { get; set; } = string.Empty;

    public int Quantidade { get; set; }

    public decimal PrecoVenda { get; set; }

    public decimal PrecoMedio { get; set; }

    public decimal Lucro { get; set; }
}