namespace ClientesService.Api.Domain.Entities;

public sealed class ContaGrafica
{
    public long Id { get; set; } 
    public long ClienteId { get; set; } 
    public string NumeroConta { get; set; } = string.Empty; 
    public string Tipo { get; set; } = "FILHOTE"; 
    public DateTime DataCriacao { get; set; } 

    public Cliente? Cliente { get; set; }
}