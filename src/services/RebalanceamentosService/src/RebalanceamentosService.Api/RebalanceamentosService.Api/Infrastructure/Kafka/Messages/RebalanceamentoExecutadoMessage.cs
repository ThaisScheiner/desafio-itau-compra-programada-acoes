namespace RebalanceamentosService.Api.Infrastructure.Kafka.Messages;

public sealed class RebalanceamentoExecutadoMessage
{
    public string Tipo { get; init; } = "REBALANCEAMENTO_EXECUTADO";
    public long ClienteId { get; init; }
    public string Motivo { get; init; } = "MUDANCA_CESTA"; // ou DESVIO

    public string TickerVendido { get; init; } = string.Empty;
    public int QuantidadeVendida { get; init; }
    public decimal PrecoVenda { get; init; }
    public decimal ValorVenda { get; init; }

    public string TickerComprado { get; init; } = string.Empty;
    public int QuantidadeComprada { get; init; }
    public decimal PrecoCompra { get; init; }

    public DateTime DataExecucao { get; init; }
}