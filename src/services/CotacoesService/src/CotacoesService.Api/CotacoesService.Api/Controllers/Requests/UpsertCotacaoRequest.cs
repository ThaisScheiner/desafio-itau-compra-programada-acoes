namespace CotacoesService.Api.Controllers.Requests;

public sealed class UpsertCotacaoRequest
{
    public DateTime DataPregao { get; set; }
    public string Ticker { get; set; } = string.Empty;

    public decimal PrecoAbertura { get; set; }
    public decimal PrecoFechamento { get; set; }
    public decimal PrecoMaximo { get; set; }
    public decimal PrecoMinimo { get; set; }
}