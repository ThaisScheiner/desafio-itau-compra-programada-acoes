namespace RebalanceamentosService.Api.Application;

public sealed class ExecucaoRebalanceamentoResult
{
    public long ClienteId { get; init; }
    public bool Executou { get; init; }
    public string Mensagem { get; init; } = string.Empty;

    public string? TickerVendido { get; init; }
    public string? TickerComprado { get; init; }

    public int QuantidadeVendida { get; init; }
    public int QuantidadeComprada { get; init; }

    public decimal PrecoVenda { get; init; }
    public decimal PrecoCompra { get; init; }

    public decimal ValorVenda { get; init; }
    public decimal ValorCarteira { get; init; }

    public decimal MaiorDesvioAbsPercentual { get; init; }

    public decimal TotalVendasMes { get; init; }
    public decimal LucroLiquidoMes { get; init; }
    public decimal ValorIrVendaRebalanceamento { get; init; }
}