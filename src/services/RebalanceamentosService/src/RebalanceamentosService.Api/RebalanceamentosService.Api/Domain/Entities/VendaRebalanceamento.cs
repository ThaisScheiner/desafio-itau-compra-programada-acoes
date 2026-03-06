namespace RebalanceamentosService.Api.Domain.Entities;

public sealed class VendaRebalanceamento
{
    public long Id { get; set; }

    public long ClienteId { get; set; }

    public string Ticker { get; set; } = default!;

    public int Quantidade { get; set; }

    public decimal PrecoVenda { get; set; }

    public decimal PrecoMedio { get; set; }

    public decimal ValorVenda { get; set; }

    public decimal Lucro { get; set; }

    public DateTime DataOperacaoUtc { get; set; }
}