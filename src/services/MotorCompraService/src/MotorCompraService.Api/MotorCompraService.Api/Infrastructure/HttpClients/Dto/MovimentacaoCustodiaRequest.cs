namespace MotorCompraService.Api.Infrastructure.HttpClients.Dto;

public sealed class MovimentacaoCustodiaRequest
{
    public string TipoConta { get; set; } = default!;   // MASTER | FILHOTE
    public long? ClienteId { get; set; }                // null p/ MASTER
    public string Ticker { get; set; } = default!;
    public string TipoOperacao { get; set; } = default!; // COMPRA | VENDA
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public DateTime DataExecucao { get; set; }
    public string Origem { get; set; } = default!;
}