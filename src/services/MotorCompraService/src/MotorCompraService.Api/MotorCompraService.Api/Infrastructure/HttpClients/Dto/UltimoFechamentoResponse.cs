namespace MotorCompraService.Api.Infrastructure.HttpClients.Dto;

public sealed class UltimoFechamentoResponse
{
    public string Ticker { get; set; } = string.Empty;
    public DateTime DataPregao { get; set; }
    public decimal PrecoFechamento { get; set; }
}