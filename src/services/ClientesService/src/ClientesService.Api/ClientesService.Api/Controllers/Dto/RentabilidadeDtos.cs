namespace ClientesService.Api.Controllers.Dto;

public sealed class RentabilidadeResponse
{
    public long ClienteId { get; set; }
    public string Nome { get; set; } = default!;
    public DateTime DataConsulta { get; set; }

    public RentabilidadeResumoDto Rentabilidade { get; set; } = new();
    public List<HistoricoAporteDto> HistoricoAportes { get; set; } = new();
    public List<EvolucaoCarteiraDto> EvolucaoCarteira { get; set; } = new();
}

public sealed class RentabilidadeResumoDto
{
    public decimal ValorTotalInvestido { get; set; }
    public decimal ValorAtualCarteira { get; set; }
    public decimal PlTotal { get; set; }
    public decimal RentabilidadePercentual { get; set; }
}

public sealed class HistoricoAporteDto
{
    public string Data { get; set; } = default!; 
    public decimal Valor { get; set; }
    public string Parcela { get; set; } = default!;
}

public sealed class EvolucaoCarteiraDto
{
    public string Data { get; set; } = default!; 
    public decimal ValorInvestido { get; set; }
    public decimal ValorCarteira { get; set; }
    public decimal Rentabilidade { get; set; }
}