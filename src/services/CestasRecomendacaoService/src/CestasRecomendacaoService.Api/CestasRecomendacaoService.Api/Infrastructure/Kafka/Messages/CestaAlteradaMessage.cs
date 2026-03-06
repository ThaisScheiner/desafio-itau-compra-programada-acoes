namespace CestasRecomendacaoService.Api.Infrastructure.Kafka.Messages;

public sealed class CestaAlteradaMessage
{
    public string Tipo { get; init; } = "CESTA_ALTERADA";

    public long CestaNovaId { get; init; }
    public string NomeCestaNova { get; init; } = string.Empty;
    public DateTime DataCriacaoCestaNova { get; init; }

    public long? CestaAnteriorId { get; init; }
    public string? NomeCestaAnterior { get; init; }
    public DateTime? DataDesativacaoCestaAnterior { get; init; }

    public List<CestaItemMessage> ItensNova { get; init; } = new();
    public List<string> AtivosRemovidos { get; init; } = new();
    public List<string> AtivosAdicionados { get; init; } = new();
}

public sealed class CestaItemMessage
{
    public string Ticker { get; init; } = string.Empty;
    public decimal Percentual { get; init; }
}