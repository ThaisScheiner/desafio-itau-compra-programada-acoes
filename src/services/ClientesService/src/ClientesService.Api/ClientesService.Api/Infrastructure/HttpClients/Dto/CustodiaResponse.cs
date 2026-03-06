namespace ClientesService.Api.Infrastructure.HttpClients.Dto;

public sealed class CustodiaResponse
{
    public string TipoConta { get; set; } = string.Empty;
    public long? ClienteId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public long Quantidade { get; set; }
    public decimal PrecoMedio { get; set; }
}