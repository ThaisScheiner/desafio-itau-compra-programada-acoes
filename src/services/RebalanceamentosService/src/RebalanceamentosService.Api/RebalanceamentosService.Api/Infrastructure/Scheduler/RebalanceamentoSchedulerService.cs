using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RebalanceamentosService.Api.Application;

namespace RebalanceamentosService.Api.Infrastructure.Scheduler;

public sealed class RebalanceamentoSchedulerService(
    IServiceScopeFactory scopeFactory,
    ILogger<RebalanceamentoSchedulerService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Scheduler de rebalanceamento iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var agora = DateTime.UtcNow.Date;

                if (EhDiaExecucao(agora))
                {
                    logger.LogInformation("Executando rebalanceamento automatico {Data}", agora);

                    using var scope = scopeFactory.CreateScope();

                    var executor = scope.ServiceProvider
                        .GetRequiredService<IRebalanceamentoExecutor>();

                    // exemplo: executa para todos clientes
                    await executor.ExecutarMudancaCestaParaTodos(
                        new Infrastructure.Kafka.Messages.CestaAlteradaMessage
                        {
                            CestaNovaId = 0,
                            NomeCestaNova = "Scheduler",
                            DataCriacaoCestaNova = agora
                        },
                        stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro no scheduler de rebalanceamento.");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private static bool EhDiaExecucao(DateTime data)
    {
        var dias = new[] { 5, 15, 25 };

        foreach (var dia in dias)
        {
            var alvo = AjustarDiaUtil(new DateTime(data.Year, data.Month, dia));

            if (data == alvo.Date)
                return true;
        }

        return false;
    }

    private static DateTime AjustarDiaUtil(DateTime data)
    {
        if (data.DayOfWeek == DayOfWeek.Saturday)
            return data.AddDays(2);

        if (data.DayOfWeek == DayOfWeek.Sunday)
            return data.AddDays(1);

        return data;
    }
}