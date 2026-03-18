using System.Net;
using BuildingBlocks.Correlation;
using BuildingBlocks.Extensions;
using Microsoft.EntityFrameworkCore;
using MotorCompraService.Api.Infrastructure.Observability;
using MotorCompraService.Api.Infrastructure.Persistence;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddBuildingBlocks();

var cs = builder.Configuration.GetConnectionString("MySql")
         ?? throw new InvalidOperationException("ConnectionStrings:MySql nao configurada no appsettings.");

builder.Services.AddDbContext<MotorCompraDbContext>(opt =>
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
                logger.LogWarning(
                    "HTTP retry {Attempt} after {Delay}ms. Status={Status}",
                    attempt,
                    (int)delay.TotalMilliseconds,
                    status);
            });
}

static IAsyncPolicy<HttpResponseMessage> CircuitBreakerPolicy(ILogger logger)
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 10,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (outcome, breakDelay) =>
            {
                var status = outcome.Result?.StatusCode;
                logger.LogError(
                    "Circuit OPEN for {BreakDelay}s. Status={Status}",
                    breakDelay.TotalSeconds,
                    status);
            },
            onReset: () => logger.LogInformation("Circuit CLOSED (reset)."),
            onHalfOpen: () => logger.LogInformation("Circuit HALF-OPEN (testing)."));
}

static IAsyncPolicy<HttpResponseMessage> TimeoutPolicy()
{
    return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(15));
}

using var loggerFactory = LoggerFactory.Create(lb => lb.AddConsole());
var pollyLogger = loggerFactory.CreateLogger("PollyPolicies");

// HTTP CLIENTS (com Polly)
string GetRequired(string key) =>
    builder.Configuration[key] ?? throw new InvalidOperationException($"Config '{key}' nao configurada.");

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

builder.Services.AddHttpClient("EventosIRService", c =>
{
    c.BaseAddress = new Uri(GetRequired("Services:EventosIRService"));
})
.AddPolicyHandler(TimeoutPolicy())
.AddPolicyHandler(RetryWithBackoffPolicy(pollyLogger))
.AddPolicyHandler(CircuitBreakerPolicy(pollyLogger));

// OPEN TELEMETRY
var otlpEndpoint = builder.Configuration["OpenTelemetry:Otlp:Endpoint"] ?? "http://localhost:4318";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: Telemetry.ServiceName,
            serviceVersion: Telemetry.ServiceVersion)
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = builder.Environment.EnvironmentName,
            ["service.namespace"] = "CompraProgramadaAcoes"
        }))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
            options.Filter = httpContext =>
                !httpContext.Request.Path.StartsWithSegments("/health") &&
                !httpContext.Request.Path.StartsWithSegments("/swagger");
        })
        .AddHttpClientInstrumentation(options =>
        {
            options.RecordException = true;
        })
        .AddSource(Telemetry.ServiceName)
        .AddOtlpExporter(opt =>
        {
            opt.Endpoint = new Uri("http://localhost:4318/v1/traces");
            opt.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
        })
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddMeter(Telemetry.ServiceName)
        .AddOtlpExporter(opt =>
        {
            opt.Endpoint = new Uri("http://localhost:4318/v1/metrics");
            opt.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
        })
        .AddConsoleExporter());

// HEALTH
builder.Services.AddHealthChecks()
    .AddDbContextCheck<MotorCompraDbContext>();

var app = builder.Build();

app.UseBuildingBlocks();

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<CorrelationIdMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();