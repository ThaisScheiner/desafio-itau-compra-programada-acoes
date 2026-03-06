namespace EventosIRService.Api.Infrastructure.Kafka.Messages;

public sealed class IrDedoDuroMessage
{
    public string Tipo { get; init; } = "IR_DEDO_DURO";
    public long ClienteId { get; init; }
    public string Ticker { get; init; } = string.Empty;
    public int Quantidade { get; init; }
    public decimal PrecoUnitario { get; init; }
    public decimal ValorOperacao { get; init; }
    public decimal Aliquota { get; init; } = 0.00005m;
    public decimal ValorIR { get; init; }
    public DateTime DataOperacao { get; init; }
}