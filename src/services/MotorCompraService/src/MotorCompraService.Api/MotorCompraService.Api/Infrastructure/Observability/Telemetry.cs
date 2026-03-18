using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace MotorCompraService.Api.Infrastructure.Observability;

public static class Telemetry
{
    public const string ServiceName = "MotorCompraService";
    public const string ServiceVersion = "1.0.0";

    public static readonly ActivitySource ActivitySource = new(ServiceName);
    public static readonly Meter Meter = new(ServiceName);

    public static readonly Counter<int> ComprasExecutadas =
        Meter.CreateCounter<int>("motor_compra_compras_executadas_total");

    public static readonly Counter<int> ComprasComErro =
        Meter.CreateCounter<int>("motor_compra_compras_erro_total");

    public static readonly Histogram<double> DuracaoExecucaoCompraMs =
        Meter.CreateHistogram<double>("motor_compra_execucao_compra_duration_ms");
}