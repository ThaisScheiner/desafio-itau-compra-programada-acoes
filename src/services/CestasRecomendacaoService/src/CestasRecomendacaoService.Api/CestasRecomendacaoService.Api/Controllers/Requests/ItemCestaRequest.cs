namespace CestasRecomendacaoService.Api.Controllers.Requests;

public sealed class ItemCestaRequest
{
    public string Ticker { get; set; } = string.Empty;
    public decimal Percentual { get; set; }
}