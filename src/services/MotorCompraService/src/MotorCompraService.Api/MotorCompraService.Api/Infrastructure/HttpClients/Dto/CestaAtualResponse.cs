namespace MotorCompraService.Api.Infrastructure.HttpClients.Dto;

public sealed class CestaAtualResponse
{
    public long CestaId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Ativa { get; set; }
    public DateTime DataCriacao { get; set; }
    public List<ItemCestaDto> Itens { get; set; } = new();
}

public sealed class ItemCestaDto
{
    public string Ticker { get; set; } = string.Empty;
    public decimal Percentual { get; set; }
}