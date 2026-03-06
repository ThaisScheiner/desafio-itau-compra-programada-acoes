namespace MotorCompraService.Api.Domain.Entities;

public sealed class OrdemCompra
{
    public long Id { get; set; }
    public string Ticker { get; set; } = string.Empty;  
    public int Quantidade { get; set; }                
    public decimal PrecoUnitario { get; set; }          
    public string TipoMercado { get; set; } = "FRACIONARIO"; 
    public DateTime DataExecucao { get; set; }          
}