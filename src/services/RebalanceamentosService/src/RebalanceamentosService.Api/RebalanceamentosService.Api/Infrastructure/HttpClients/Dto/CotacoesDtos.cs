namespace RebalanceamentosService.Api.Infrastructure.HttpClients.Dto;

public sealed class UltimoFechamentoResponse
{
    public string Ticker { get; set; } = default!;
    public DateTime DataPregao { get; set; }
    public decimal PrecoFechamento { get; set; }
}