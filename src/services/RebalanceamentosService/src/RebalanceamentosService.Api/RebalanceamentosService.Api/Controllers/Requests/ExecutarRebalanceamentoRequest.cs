using RebalanceamentosService.Api.Domain.Enums;

namespace RebalanceamentosService.Api.Controllers.Requests;

public sealed class ExecutarRebalanceamentoRequest
{
    public TipoRebalanceamento Tipo { get; set; } = TipoRebalanceamento.DESVIO;

    public decimal LimiteDesvioPercentual { get; set; } = 2m;

    public decimal PercentualMovimentacao { get; set; } = 10m;
}