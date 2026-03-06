namespace CestasRecomendacaoService.Api.Controllers.Requests;

public sealed class CriarCestaRequest
{
    public string Nome { get; set; } = string.Empty;
    public List<ItemCestaRequest> Itens { get; set; } = new();
}