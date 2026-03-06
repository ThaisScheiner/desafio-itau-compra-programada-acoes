using BuildingBlocks.Correlation;
using BuildingBlocks.Extensions;
using ClientesService.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddBuildingBlocks();

builder.Services.AddHealthChecks();

var cs = builder.Configuration.GetConnectionString("MySql")!;

builder.Services.AddDbContext<ClientesDbContext>(opt =>
    opt.UseMySql(cs, ServerVersion.AutoDetect(cs)));

builder.Services.AddHealthChecks()
    .AddMySql(cs);

var cotacoesBase = builder.Configuration["Services:CotacoesService"]
                  ?? throw new InvalidOperationException("Services:CotacoesService nao configurado.");
var custodiasBase = builder.Configuration["Services:CustodiasService"]
                   ?? throw new InvalidOperationException("Services:CustodiasService nao configurado.");

builder.Services.AddHttpClient("CustodiasService", c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Services:CustodiasService"]!);
    c.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient("CotacoesService", c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Services:CotacoesService"]!);
    c.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient("MotorCompraService", c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Services:MotorCompraService"]!);
    c.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

app.UseBuildingBlocks();

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<CorrelationIdMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();