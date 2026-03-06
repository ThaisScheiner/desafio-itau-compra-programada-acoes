namespace EventosIRService.Api.Infrastructure.Kafka.Messages;

public sealed class IrVendaRebalanceamentoMessage
{
    public string Tipo { get; init; } = "IR_VENDA";

    public long ClienteId { get; init; }

    public string MesReferencia { get; init; } = string.Empty;

    public decimal TotalVendasMes { get; init; }

    public decimal LucroLiquido { get; init; }

    public decimal Aliquota { get; init; } = 0.20m;

    public decimal ValorIR { get; init; }

    public List<IrVendaRebalanceamentoDetalheMessage> Detalhes { get; init; } = new();

    public DateTime DataCalculo { get; init; }
}

public sealed class IrVendaRebalanceamentoDetalheMessage
{
    public string Ticker { get; init; } = string.Empty;

    public int Quantidade { get; init; }

    public decimal PrecoVenda { get; init; }

    public decimal PrecoMedio { get; init; }

    public decimal Lucro { get; init; }
}