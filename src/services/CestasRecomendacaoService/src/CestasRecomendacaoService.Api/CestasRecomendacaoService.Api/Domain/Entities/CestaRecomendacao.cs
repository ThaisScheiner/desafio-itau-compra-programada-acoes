namespace CestasRecomendacaoService.Api.Domain.Entities;

public sealed class CestaRecomendacao
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty; 
    public bool Ativa { get; set; } = true;          
    public DateTime DataCriacao { get; set; }        
    public DateTime? DataDesativacao { get; set; }   

    public List<ItemCesta> Itens { get; set; } = new();
}