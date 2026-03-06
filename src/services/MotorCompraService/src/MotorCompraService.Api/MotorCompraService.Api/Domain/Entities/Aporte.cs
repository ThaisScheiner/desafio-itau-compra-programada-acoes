namespace MotorCompraService.Api.Domain.Entities;

public sealed class Aporte
{
    public long Id { get; set; }
    public long ClienteId { get; set; }
    public DateTime DataReferencia { get; set; } 
    public decimal Valor { get; set; }           
    public string Parcela { get; set; } = "1/3"; 
    public DateTime CriadoEm { get; set; }
}