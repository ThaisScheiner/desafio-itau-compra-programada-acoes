using EventosIRService.Api.Domain.Enums;

namespace EventosIRService.Api.Domain.Entities;

public sealed class EventoIR
{
    public long Id { get; set; }

    public long ClienteId { get; set; }

    public TipoEventoIR Tipo { get; set; }  

    public string Ticker { get; set; } = string.Empty;

    public int Quantidade { get; set; }     
    public decimal PrecoUnitario { get; set; }

    public decimal ValorBase { get; set; }  
    public decimal ValorIR { get; set; }

    public bool PublicadoKafka { get; set; }
    public DateTime DataEvento { get; set; }
}