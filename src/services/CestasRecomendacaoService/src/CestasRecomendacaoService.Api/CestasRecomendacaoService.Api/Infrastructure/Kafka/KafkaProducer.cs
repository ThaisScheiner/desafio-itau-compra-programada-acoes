using System.Text.Json;
using Confluent.Kafka;

namespace CestasRecomendacaoService.Api.Infrastructure.Kafka;

public interface IKafkaProducer
{
    Task ProduceAsync(string topic, string key, object payload, CancellationToken ct);
}

public sealed class KafkaProducer : IKafkaProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;

    public KafkaProducer(IConfiguration config)
    {
        var bootstrap = config["Kafka:BootstrapServers"]
            ?? throw new InvalidOperationException("Kafka:BootstrapServers nao configurado.");

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = bootstrap,
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageSendMaxRetries = 3,
            RetryBackoffMs = 200
        };

        _producer = new ProducerBuilder<string, string>(producerConfig).Build();
    }

    public async Task ProduceAsync(string topic, string key, object payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await _producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = key,
            Value = json
        }, ct);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(3));
        _producer.Dispose();
    }
}