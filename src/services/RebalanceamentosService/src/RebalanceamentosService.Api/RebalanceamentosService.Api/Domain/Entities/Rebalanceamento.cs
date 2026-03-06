using RebalanceamentosService.Api.Domain.Enums;

namespace RebalanceamentosService.Api.Domain.Entities;

public sealed class Rebalanceamento
{
    public long Id { get; set; }                 
    public long ClienteId { get; set; }          

    public TipoRebalanceamento Tipo { get; set; } 

    public string TickerVendido { get; set; } = default!;  
    public string TickerComprado { get; set; } = default!; 

    public decimal ValorVenda { get; set; }      

    public DateTime DataRebalanceamento { get; set; } 
}