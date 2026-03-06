namespace RebalanceamentosService.Api.Infrastructure.HttpClients.Dto;

public sealed class CustodiasClientResponse
{
    public long ClienteId { get; set; }
    public List<CustodiaPosicaoDto> Posicoes { get; set; } = new();
}

public sealed class CustodiaPosicaoDto
{
    public string Ticker { get; set; } = default!;
    public int Quantidade { get; set; }
    public decimal PrecoMedio { get; set; }
}