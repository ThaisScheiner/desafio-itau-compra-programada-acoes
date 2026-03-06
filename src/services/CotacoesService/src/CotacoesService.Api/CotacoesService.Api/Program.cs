using BuildingBlocks.Correlation;
using BuildingBlocks.Extensions;
using CotacoesService.Api.Infrastructure.Cotahist;
using CotacoesService.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

// diretorio cotacoes
var pastaCotacoes = Path.GetFullPath(
    Path.Combine(builder.Environment.ContentRootPath, "..", "..", "..", "..", "..", "..", "cotacoes")
);

builder.Configuration["Cotacoes:PastaCotacoes"] = pastaCotacoes;
//fim caminho


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddBuildingBlocks();

// DbContext + HealthCheck MySQL
var cs = builder.Configuration.GetConnectionString("MySql")
         ?? throw new InvalidOperationException("ConnectionStrings:MySql nao configurada no appsettings.");

builder.Services.AddDbContext<CotacoesDbContext>(opt =>
    opt.UseMySql(cs, ServerVersion.AutoDetect(cs)));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<CotacoesDbContext>();

builder.Services.AddSingleton<CotahistParser>();



var app = builder.Build();

// Middlewares base
app.UseBuildingBlocks();

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<CorrelationIdMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();