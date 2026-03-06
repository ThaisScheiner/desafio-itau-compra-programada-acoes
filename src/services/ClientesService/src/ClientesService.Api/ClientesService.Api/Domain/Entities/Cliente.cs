namespace ClientesService.Api.Domain.Entities;

public sealed class Cliente
{
    public long Id { get; set; }                      
    public string Nome { get; set; } = string.Empty;  
    public string CPF { get; set; } = string.Empty;   
    public string Email { get; set; } = string.Empty; 
    public decimal ValorMensal { get; set; }          
    public bool Ativo { get; set; } = true;           
    public DateTime DataAdesao { get; set; }          
    public DateTime? DataSaida { get; set; }          

    public ContaGrafica? ContaGrafica { get; set; }   
}