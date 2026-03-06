namespace ClientesService.Api.Controllers.Dto;

public sealed class CarteiraResponse
{
    public long ClienteId { get; set; }
    public string Nome { get; set; } = default!;
    public string ContaGrafica { get; set; } = default!;
    public DateTime DataConsulta { get; set; }

    public CarteiraResumoDto Resumo { get; set; } = new();
    public List<CarteiraAtivoDto> Ativos { get; set; } = new();
}

public sealed class CarteiraResumoDto
{
    public decimal ValorTotalInvestido { get; set; }
    public decimal ValorAtualCarteira { get; set; }
    public decimal PlTotal { get; set; }
    public decimal RentabilidadePercentual { get; set; }
}

public sealed class CarteiraAtivoDto
{
    public string Ticker { get; set; } = default!;
    public int Quantidade { get; set; }

    public decimal PrecoMedio { get; set; }
    public decimal CotacaoAtual { get; set; }

    public decimal ValorAtual { get; set; }
    public decimal Pl { get; set; }
    public decimal PlPercentual { get; set; }

    public decimal ComposicaoCarteira { get; set; }
}