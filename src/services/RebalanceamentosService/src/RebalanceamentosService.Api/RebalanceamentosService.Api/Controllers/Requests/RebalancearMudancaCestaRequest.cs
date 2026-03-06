namespace RebalanceamentosService.Api.Controllers.Requests;

public sealed class RebalancearMudancaCestaRequest
{
    public DateTime DataRebalanceamento { get; set; } 
    public List<ClienteRebalanceamentoItem> Clientes { get; set; } = new();
}

public sealed class ClienteRebalanceamentoItem
{
    public long ClienteId { get; set; }
    public List<OperacaoRebalanceamentoItem> Operacoes { get; set; } = new();
}

public sealed class OperacaoRebalanceamentoItem
{
    public string TickerVendido { get; set; } = default!;
    public string TickerComprado { get; set; } = default!;
    public decimal ValorVenda { get; set; } 
}