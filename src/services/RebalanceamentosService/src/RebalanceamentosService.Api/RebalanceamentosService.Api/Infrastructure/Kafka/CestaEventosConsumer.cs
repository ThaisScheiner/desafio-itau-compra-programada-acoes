using System.Text.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using RebalanceamentosService.Api.Domain.Enums;
using RebalanceamentosService.Api.Infrastructure.Kafka.Messages;
using RebalanceamentosService.Api.Infrastructure.Persistence;

namespace RebalanceamentosService.Api.Infrastructure.Kafka;

public sealed class CestaEventosConsumer : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly IServiceScopeFactory _scopeFactory;
    private IConsumer<string, string>? _consumer;

    public CestaEventosConsumer(IConfiguration config, IServiceScopeFactory scopeFactory)
    {
        _config = config;
        _scopeFactory = scopeFactory;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bootstrap = _config["Kafka:BootstrapServers"]
            ?? throw new InvalidOperationException("Kafka:BootstrapServers nao configurado.");
        var topic = _config["Kafka:TopicCestas"] ?? "cestas-eventos";
        var groupId = _config["Kafka:GroupId"] ?? "rebalanceamentos-service";

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrap,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        _consumer.Subscribe(topic);

        return Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? cr = null;

                try
                {
                    cr = _consumer.Consume(stoppingToken);
                    if (cr?.Message?.Value is null) continue;

                    var evt = JsonSerializer.Deserialize<CestaAlteradaMessage>(
                        cr.Message.Value,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (evt is null || !string.Equals(evt.Tipo, "CESTA_ALTERADA", StringComparison.OrdinalIgnoreCase))
                    {
                        _consumer.Commit(cr);
                        continue;
                    }

                    // Disparo do rebalanceamento (MUDANCA_CESTA)
                    await ProcessarCestaAlterada(evt, stoppingToken);

                    _consumer.Commit(cr);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    await Task.Delay(500, stoppingToken);
                }
            }
        }, stoppingToken);
    }

    private async Task ProcessarCestaAlterada(CestaAlteradaMessage evt, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RebalanceamentosDbContext>();

        db.Rebalanceamentos.Add(new Domain.Entities.Rebalanceamento
        {
            ClienteId = 0, // marcador de processo global
            Tipo = TipoRebalanceamento.MUDANCA_CESTA,
            TickerVendido = string.Join(",", evt.AtivosRemovidos),
            TickerComprado = string.Join(",", evt.AtivosAdicionados),
            ValorVenda = 0m,
            DataRebalanceamento = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _consumer?.Close();
            _consumer?.Dispose();
        }
        catch { /* ignore */ }

        return base.StopAsync(cancellationToken);
    }
}