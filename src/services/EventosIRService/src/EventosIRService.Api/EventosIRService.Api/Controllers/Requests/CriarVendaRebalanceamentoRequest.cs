namespace EventosIRService.Api.Controllers.Requests;

public sealed class CriarVendaRebalanceamentoRequest
{
    public long ClienteId { get; set; }
    public string MesReferencia { get; set; } = string.Empty;
    public decimal TotalVendasMes { get; set; }
    public decimal LucroLiquido { get; set; }
    public decimal ValorIR { get; set; }
    public DateTime DataCalculo { get; set; }
}