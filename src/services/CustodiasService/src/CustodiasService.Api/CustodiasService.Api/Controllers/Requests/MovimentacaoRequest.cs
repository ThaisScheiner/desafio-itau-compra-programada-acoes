namespace CustodiasService.Api.Controllers.Requests;

public sealed class MovimentacaoRequest
{
    public string TipoConta { get; set; } = "FILHOTE"; // MASTER ou FILHOTE
    public long? ClienteId { get; set; }              
    public string Ticker { get; set; } = string.Empty;

    public string TipoOperacao { get; set; } = "COMPRA"; // COMPRA ou VENDA
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
}