using BuildingBlocks.Correlation;
using BuildingBlocks.Extensions;
using CestasRecomendacaoService.Api.Infrastructure.Health;
using CestasRecomendacaoService.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddBuildingBlocks();

// ===== DB (MySQL) =====
var cs = builder.Configuration.GetConnectionString("MySql")
         ?? throw new InvalidOperationException("ConnectionStrings:MySql nao configurada no appsettings.");

builder.Services.AddDbContext<CestasRecomendacaoDbContext>(opt =>
    opt.UseMySql(cs, ServerVersion.AutoDetect(cs)));

// ===== Kafka Producer =====
builder.Services.AddSingleton<
    CestasRecomendacaoService.Api.Infrastructure.Kafka.IKafkaProducer,
    CestasRecomendacaoService.Api.Infrastructure.Kafka.KafkaProducer>();

// ===== HEALTH (MySQL + Kafka) =====
builder.Services.AddHealthChecks()
    .AddMySql(cs, name: "mysql")
    .AddCheck<KafkaHealthCheck>("kafka");

var app = builder.Build();

app.UseBuildingBlocks();

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<CorrelationIdMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();