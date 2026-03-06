namespace EventosIRService.Api.Controllers.Requests;

public sealed class CriarVendaRebalanceamentoRequest
{
    public long ClienteId { get; set; }

    public string MesReferencia { get; set; } = string.Empty;

    public decimal TotalVendasMes { get; set; }

    public decimal LucroLiquido { get; set; }

    public decimal ValorIR { get; set; }

    public DateTime DataCalculo { get; set; }

    public List<CriarVendaRebalanceamentoDetalheRequest> Detalhes { get; set; } = new();
}

public sealed class CriarVendaRebalanceamentoDetalheRequest
{
    public string Ticker { get; set; } = string.Empty;

    public int Quantidade { get; set; }

    public decimal PrecoVenda { get; set; }

    public decimal PrecoMedio { get; set; }

    public decimal Lucro { get; set; }
}