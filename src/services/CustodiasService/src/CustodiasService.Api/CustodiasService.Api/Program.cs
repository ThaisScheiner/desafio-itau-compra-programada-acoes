using BuildingBlocks.Correlation;
using BuildingBlocks.Extensions;
using CustodiasService.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// BuildingBlocks (ProblemDetails + ExceptionHandler)
builder.Services.AddBuildingBlocks();

var cs = builder.Configuration.GetConnectionString("MySql")
         ?? throw new InvalidOperationException("ConnectionStrings:MySql nao configurada no appsettings.");

builder.Services.AddDbContext<CustodiasDbContext>(opt =>
    opt.UseMySql(cs, ServerVersion.AutoDetect(cs)));

// HealthCheck  DbContext
builder.Services.AddHealthChecks()
    .AddDbContextCheck<CustodiasDbContext>();

var app = builder.Build();

app.UseBuildingBlocks();

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<CorrelationIdMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();