namespace MotorCompraService.Api.Domain.Entities;

public sealed class CustodiaMasterSaldo
{
    public long Id { get; set; }
    public string Ticker { get; set; } = default!;
    public int Quantidade { get; set; }
    public DateTime AtualizadoEm { get; set; }
}