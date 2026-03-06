namespace ClientesService.Api.Infrastructure.HttpClients.Dto;

public sealed class CustodiasClientDtos
{
    public long ClienteId { get; set; }
    public List<CustodiaPosicaoDto> Posicoes { get; set; } = new();
}

public sealed class CustodiaPosicaoDto
{
    public string Ticker { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal PrecoMedio { get; set; }
}