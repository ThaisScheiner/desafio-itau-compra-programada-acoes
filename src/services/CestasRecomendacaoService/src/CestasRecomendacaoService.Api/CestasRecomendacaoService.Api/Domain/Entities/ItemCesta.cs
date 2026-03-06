namespace CestasRecomendacaoService.Api.Domain.Entities;

public sealed class ItemCesta
{
    public long Id { get; set; }
    public long CestaId { get; set; }                
    public string Ticker { get; set; } = string.Empty; 
    public decimal Percentual { get; set; }          

    public CestaRecomendacao? Cesta { get; set; }
}