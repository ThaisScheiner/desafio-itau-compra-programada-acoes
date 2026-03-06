namespace CustodiasService.Api.Domain.Entities;

public sealed class Custodia
{
    public long Id { get; set; }

    // MASTER (conta master da corretora)
    // FILHOTE (conta gráfica do cliente)
    public string TipoConta { get; set; } = "FILHOTE"; 

    // FILHOTE: referencia o ClienteId (do ClientesService)
    // MASTER: null
    public long? ClienteId { get; set; }

    public string Ticker { get; set; } = string.Empty; 

    public int Quantidade { get; set; } 
    public decimal PrecoMedio { get; set; } 

    public DateTime AtualizadoEm { get; set; } 
}