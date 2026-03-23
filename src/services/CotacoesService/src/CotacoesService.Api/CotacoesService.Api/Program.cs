using BuildingBlocks.Correlation;
using BuildingBlocks.Extensions;
using BuildingBlocks.Observability;
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
// fim caminho

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddBuildingBlocks();

// DbContext + HealthCheck MySQL
var cs = builder.Configuration.GetConnectionString("MySql")
         ?? throw new InvalidOperationException("ConnectionStrings:MySql nao configurada no appsettings.");

builder.Services.AddDbContext<CotacoesDbContext>(opt =>
    opt.UseMySql(cs, new MySqlServerVersion(new Version(8, 0, 36))));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<CotacoesDbContext>();

builder.Services.AddSingleton<CotahistParser>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularFront",
        policy => policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("X-Trace-Id", "X-Span-Id", "X-Correlation-Id", "X-Request-Id"));
});

var app = builder.Build();

// Middlewares base
app.UseBuildingBlocks();

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<TelemetryHeadersMiddleware>();

app.UseCors("AngularFront");

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();