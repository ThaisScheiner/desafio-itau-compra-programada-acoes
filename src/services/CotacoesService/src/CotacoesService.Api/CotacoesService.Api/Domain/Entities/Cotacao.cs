namespace CotacoesService.Api.Domain.Entities;

public sealed class Cotacao
{
    public long Id { get; set; }               
    public DateTime DataPregao { get; set; }   
    public string Ticker { get; set; } = "";   
    public decimal PrecoAbertura { get; set; } 
    public decimal PrecoFechamento { get; set; } 
    public decimal PrecoMaximo { get; set; }   
    public decimal PrecoMinimo { get; set; }   
}