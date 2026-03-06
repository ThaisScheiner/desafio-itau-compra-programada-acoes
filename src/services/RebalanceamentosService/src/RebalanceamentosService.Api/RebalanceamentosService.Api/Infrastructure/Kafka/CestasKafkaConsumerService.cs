using System.Text;
using System.Text.Json;
using BuildingBlocks.Persistence.Inbox;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using RebalanceamentosService.Api.Application;
using RebalanceamentosService.Api.Infrastructure.Kafka.Messages;
using RebalanceamentosService.Api.Infrastructure.Persistence;

namespace RebalanceamentosService.Api.Infrastructure.Kafka;

public sealed class CestasKafkaConsumerService(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<CestasKafkaConsumerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bootstrap = config["Kafka:BootstrapServers"] ?? "localhost:9094";
        var topicIn = config["Kafka:TopicCestas"] ?? "cestas-eventos";
        var consumerName = "RebalanceamentosService.CestasConsumer";

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrap,
            GroupId = config["Kafka:GroupIdCestas"] ?? "rebalanceamentos-cestas-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            AllowAutoCreateTopics = true
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe(topicIn);

        logger.LogInformation(
            " CestasKafkaConsumerService consumindo {Topic} em {Bootstrap} com GroupId={GroupId}",
            topicIn,
            bootstrap,
            consumerConfig.GroupId);

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? cr = null;

            try
            {
                cr = consumer.Consume(stoppingToken);

                if (cr?.Message is null)
                    continue;

                var eventId =
                    GetHeaderAsString(cr.Message.Headers, "eventId")
                    ?? GetHeaderAsString(cr.Message.Headers, "event_id")
                    ?? $"{topicIn}:{cr.Partition.Value}:{cr.Offset.Value}";

                logger.LogInformation(
                    "Mensagem recebida. Topic={Topic}, Partition={Partition}, Offset={Offset}, EventId={EventId}",
                    cr.Topic,
                    cr.Partition.Value,
                    cr.Offset.Value,
                    eventId);

                if (string.IsNullOrWhiteSpace(cr.Message.Value))
                {
                    logger.LogWarning("Mensagem Kafka vazia. Commitando offset. EventId={EventId}", eventId);
                    consumer.Commit(cr);
                    continue;
                }

                var msg = JsonSerializer.Deserialize<CestaAlteradaMessage>(
                    cr.Message.Value,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (msg is null)
                {
                    logger.LogWarning("Mensagem Kafka inválida (desserialização null). Commitando offset. EventId={EventId}", eventId);
                    consumer.Commit(cr);
                    continue;
                }

                using var scope = scopeFactory.CreateScope();

                var db = scope.ServiceProvider.GetRequiredService<RebalanceamentosDbContext>();
                var executor = scope.ServiceProvider.GetRequiredService<IRebalanceamentoExecutor>();

                var jaProcessou = await db.InboxProcessedEvents
                    .AsNoTracking()
                    .AnyAsync(
                        x => x.EventId == eventId && x.Consumer == consumerName,
                        stoppingToken);

                if (jaProcessou)
                {
                    logger.LogInformation(
                        "Evento já processado anteriormente. EventId={EventId}. Commitando offset.",
                        eventId);

                    consumer.Commit(cr);
                    continue;
                }

                await executor.ExecutarMudancaCestaParaTodos(msg, stoppingToken);

                db.InboxProcessedEvents.Add(new InboxProcessedEvent
                {
                    EventId = eventId,
                    Consumer = consumerName,
                    ProcessedAt = DateTime.UtcNow
                });

                await db.SaveChangesAsync(stoppingToken);

                consumer.Commit(cr);

                logger.LogInformation(
                    "Evento processado com sucesso e offset commitado. EventId={EventId}",
                    eventId);
            }
            catch (ConsumeException ex) when (
                ex.Error.Code == ErrorCode.UnknownTopicOrPart)
            {
                logger.LogWarning(
                    "Topico Kafka ainda nao disponivel: {Topic}. Broker={Broker}. Motivo={Reason}. Aguardando para tentar novamente...",
                    topicIn,
                    bootstrap,
                    ex.Error.Reason);

                await Task.Delay(3000, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("CestasKafkaConsumerService finalizando por cancelamento.");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro no processamento da mensagem Kafka.");
                await Task.Delay(2000, stoppingToken);
            }
        }

        consumer.Close();
        logger.LogInformation("CestasKafkaConsumerService encerrado.");
    }

    private static string? GetHeaderAsString(Headers? headers, string name)
    {
        if (headers is null || headers.Count == 0)
            return null;

        var header = headers.FirstOrDefault(h =>
            h.Key.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (header is null)
            return null;

        var bytes = header.GetValueBytes();
        if (bytes is null || bytes.Length == 0)
            return null;

        return Encoding.UTF8.GetString(bytes);
    }
}