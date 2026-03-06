namespace RebalanceamentosService.Api.Infrastructure.HttpClients.Dto;

public sealed class MovimentacaoCustodiaRequest
{
    public string TipoConta { get; set; } = default!; 
    public long? ClienteId { get; set; }              
    public string Ticker { get; set; } = default!;
    public string TipoOperacao { get; set; } = default!; 
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public DateTime DataExecucao { get; set; }
    public string Origem { get; set; } = default!;
}