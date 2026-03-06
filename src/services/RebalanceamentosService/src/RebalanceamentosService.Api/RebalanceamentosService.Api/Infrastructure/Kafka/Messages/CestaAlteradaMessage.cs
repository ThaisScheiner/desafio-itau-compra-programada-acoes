namespace RebalanceamentosService.Api.Infrastructure.Kafka.Messages;

public sealed class CestaAlteradaMessage
{
    public string Tipo { get; set; } = "CESTA_ALTERADA";

    public long CestaNovaId { get; set; }
    public string NomeCestaNova { get; set; } = string.Empty;
    public DateTime DataCriacaoCestaNova { get; set; }

    public long? CestaAnteriorId { get; set; }
    public string? NomeCestaAnterior { get; set; }
    public DateTime? DataDesativacaoCestaAnterior { get; set; }

    public List<CestaItemMessage> ItensNova { get; set; } = new();
    public List<string> AtivosRemovidos { get; set; } = new();
    public List<string> AtivosAdicionados { get; set; } = new();

    // compatibilidade com o executor (sem mudar o contrato publicado no Kafka)
    public long CestaId => CestaNovaId;
    public DateTime DataAlteracao => DataCriacaoCestaNova;
}

public sealed class CestaItemMessage
{
    public string Ticker { get; set; } = string.Empty;
    public decimal Percentual { get; set; }
}