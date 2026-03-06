namespace RebalanceamentosService.Api.Infrastructure.HttpClients.Dto;

public sealed class CestaHistoricoResponse
{
    public List<CestaDto> Cestas { get; set; } = new();
}

public sealed class CestaDto
{
    public long CestaId { get; set; }
    public string Nome { get; set; } = default!;
    public bool Ativa { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataDesativacao { get; set; }
    public List<CestaItemDto> Itens { get; set; } = new();
}

public sealed class CestaItemDto
{
    public string Ticker { get; set; } = default!;
    public decimal Percentual { get; set; }
}