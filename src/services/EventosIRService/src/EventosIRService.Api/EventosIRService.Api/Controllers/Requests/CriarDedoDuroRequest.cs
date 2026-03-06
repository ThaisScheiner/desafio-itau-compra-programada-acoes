namespace EventosIRService.Api.Controllers.Requests;

public sealed class CriarDedoDuroRequest
{
    public long ClienteId { get; set; }
    public string Ticker { get; set; } = string.Empty;

    public int Quantidade { get; set; } 
    public decimal PrecoUnitario { get; set; }

    public decimal ValorOperacao { get; set; }
    public decimal ValorIR { get; set; }

    public DateTime DataOperacao { get; set; }
}