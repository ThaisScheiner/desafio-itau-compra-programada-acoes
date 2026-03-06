using RebalanceamentosService.Api.Domain.Enums;
using RebalanceamentosService.Api.Infrastructure.Kafka.Messages;

namespace RebalanceamentosService.Api.Application;

public interface IRebalanceamentoExecutor
{
    Task ExecutarMudancaCestaParaTodos(CestaAlteradaMessage msg, CancellationToken ct);

    // executa para 1 cliente (uso pelo consumer)
    Task<ExecucaoRebalanceamentoResult> ExecutarParaCliente(
        long clienteId,
        TipoRebalanceamento tipo,
        decimal limiteDesvioPercentual,
        decimal percentualMovimentacao,
        CancellationToken ct);
}