using BuildingBlocks.Correlation;
using BuildingBlocks.Extensions;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;
using RebalanceamentosService.Api.Application;
using RebalanceamentosService.Api.Infrastructure.Health;
using RebalanceamentosService.Api.Infrastructure.Kafka;
using RebalanceamentosService.Api.Infrastructure.Persistence;
using RebalanceamentosService.Api.Infrastructure.Scheduler;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddBuildingBlocks();

var cs = builder.Configuration.GetConnectionString("MySql")
         ?? throw new InvalidOperationException("ConnectionStrings:MySql nao configurada no appsettings.");

builder.Services.AddDbContext<RebalanceamentosDbContext>(opt =>
    opt.UseMySql(cs, ServerVersion.AutoDetect(cs)));

// POLLY POLICIES
static IAsyncPolicy<HttpResponseMessage> RetryWithBackoffPolicy(ILogger logger)
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            retryCount: 5,
            sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt)),
            onRetry: (outcome, delay, attempt, ctx) =>
            {
                var status = outcome.Result?.StatusCode;
                logger.LogWarning("HTTP retry {Attempt} after {Delay}ms. Status={Status}",
                    attempt, (int)delay.TotalMilliseconds, status);
            });
}

static IAsyncPolicy<HttpResponseMessage> CircuitBreakerPolicy(ILogger logger)
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 10,
            durationOfBreak: TimeSpan.FromSeconds(30));
}

static IAsyncPolicy<HttpResponseMessage> TimeoutPolicy()
{
    return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(5));
}

using var loggerFactory = LoggerFactory.Create(lb => lb.AddConsole());
var pollyLogger = loggerFactory.CreateLogger("PollyPolicies");

string GetRequired(string key) =>
    builder.Configuration[key] ?? throw new InvalidOperationException($"Config '{key}' nao configurada.");

// HTTP CLIENTS (com Polly)
builder.Services.AddHttpClient("ClientesService", c =>
{
    c.BaseAddress = new Uri(GetRequired("Services:ClientesService"));
})
.AddPolicyHandler(TimeoutPolicy())
.AddPolicyHandler(RetryWithBackoffPolicy(pollyLogger))
.AddPolicyHandler(CircuitBreakerPolicy(pollyLogger));

builder.Services.AddHttpClient("CestasRecomendacaoService", c =>
{
    c.BaseAddress = new Uri(GetRequired("Services:CestasRecomendacaoService"));
})
.AddPolicyHandler(TimeoutPolicy())
.AddPolicyHandler(RetryWithBackoffPolicy(pollyLogger))
.AddPolicyHandler(CircuitBreakerPolicy(pollyLogger));

builder.Services.AddHttpClient("CotacoesService", c =>
{
    c.BaseAddress = new Uri(GetRequired("Services:CotacoesService"));
})
.AddPolicyHandler(TimeoutPolicy())
.AddPolicyHandler(RetryWithBackoffPolicy(pollyLogger))
.AddPolicyHandler(CircuitBreakerPolicy(pollyLogger));

builder.Services.AddHttpClient("CustodiasService", c =>
{
    c.BaseAddress = new Uri(GetRequired("Services:CustodiasService"));
})
.AddPolicyHandler(TimeoutPolicy())
.AddPolicyHandler(RetryWithBackoffPolicy(pollyLogger))
.AddPolicyHandler(CircuitBreakerPolicy(pollyLogger));

// App services
builder.Services.AddScoped<IRebalanceamentoExecutor, RebalanceamentoExecutor>();

// Kafka
builder.Services.AddHostedService<CestasKafkaConsumerService>();
builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();

//Health
builder.Services.AddHealthChecks()
    .AddDbContextCheck<RebalanceamentosDbContext>()
    .AddCheck<KafkaHealthCheck>("kafka");


builder.Services.AddHostedService<RebalanceamentoSchedulerService>();

var app = builder.Build();

app.UseBuildingBlocks();

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<CorrelationIdMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();