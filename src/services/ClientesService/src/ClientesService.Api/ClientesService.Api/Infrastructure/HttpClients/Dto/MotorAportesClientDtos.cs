namespace ClientesService.Api.Infrastructure.HttpClients.Dto;

public sealed class MotorAportesClientDtos
{
    public long ClienteId { get; set; }
    public List<MotorAporteItemDto> HistoricoAportes { get; set; } = new();
}

public sealed class MotorAporteItemDto
{
    public string Data { get; set; } = default!; 
    public decimal Valor { get; set; }
    public string Parcela { get; set; } = default!;
}