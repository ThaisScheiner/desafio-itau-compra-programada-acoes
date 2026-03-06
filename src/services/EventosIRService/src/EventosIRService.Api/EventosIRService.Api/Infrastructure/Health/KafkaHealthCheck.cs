using Confluent.Kafka;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EventosIRService.Api.Infrastructure.Health;

public sealed class KafkaHealthCheck(IConfiguration config) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var bootstrap = config["Kafka:BootstrapServers"];
            if (string.IsNullOrWhiteSpace(bootstrap))
                return Task.FromResult(
                    HealthCheckResult.Unhealthy("Kafka:BootstrapServers nao configurado."));

            using var admin = new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = bootstrap
            }).Build();

            admin.GetMetadata(TimeSpan.FromSeconds(2));

            return Task.FromResult(HealthCheckResult.Healthy("Kafka OK"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Kafka FAIL", ex));
        }
    }
}