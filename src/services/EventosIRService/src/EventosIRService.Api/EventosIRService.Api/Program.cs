using BuildingBlocks.Correlation;
using BuildingBlocks.Extensions;
using EventosIRService.Api.Infrastructure.Kafka;
using EventosIRService.Api.Infrastructure.Health;
using EventosIRService.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddBuildingBlocks();

var cs = builder.Configuration.GetConnectionString("MySql")
         ?? throw new InvalidOperationException("ConnectionStrings:MySql nao configurada no appsettings.");

builder.Services.AddDbContext<EventosIRDbContext>(opt =>
    opt.UseMySql(cs, ServerVersion.AutoDetect(cs)));

// Kafka Producer 
builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();

// HEALTH (MySQL + Kafka) 
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