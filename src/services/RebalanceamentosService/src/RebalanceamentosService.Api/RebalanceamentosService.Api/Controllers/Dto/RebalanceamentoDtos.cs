using RebalanceamentosService.Api.Domain.Enums;

namespace RebalanceamentosService.Api.Controllers.Dto;

public sealed class ExecutarRebalanceamentoResponse
{
    public long ClienteId { get; set; }
    public TipoRebalanceamento Tipo { get; set; }

    public DateTime DataExecucao { get; set; }
    public decimal LimiteDesvioPercentual { get; set; }

    public decimal ValorCarteira { get; set; }
    public decimal PercentualMovimentacao { get; set; }
    public decimal ValorMovimentado { get; set; }

    public string? TickerVendido { get; set; }
    public int QuantidadeVendida { get; set; }
    public decimal PrecoVenda { get; set; }
    public decimal ValorVenda { get; set; }

    public string? TickerComprado { get; set; }
    public int QuantidadeComprada { get; set; }
    public decimal PrecoCompra { get; set; }

    public decimal MaiorDesvioAbsPercentual { get; set; }
    public List<DiagnosticoTickerDto> Diagnostico { get; set; } = new();

    public bool Executou { get; set; }
    public string Mensagem { get; set; } = string.Empty;
}

public sealed class DiagnosticoTickerDto
{
    public string Ticker { get; set; } = default!;
    public decimal PercentualAlvo { get; set; }
    public decimal PercentualAtual { get; set; }
    public decimal DesvioPercentual { get; set; }
    public decimal ValorAtual { get; set; }
}