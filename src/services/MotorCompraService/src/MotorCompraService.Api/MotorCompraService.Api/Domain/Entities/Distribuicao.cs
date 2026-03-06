namespace MotorCompraService.Api.Domain.Entities;

public sealed class Distribuicao
{
    public long Id { get; set; }
    public long OrdemCompraId { get; set; }             
    public long ClienteId { get; set; }                 
    public string Ticker { get; set; } = string.Empty;  
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public DateTime DataDistribuicao { get; set; }

    public OrdemCompra? OrdemCompra { get; set; }
}