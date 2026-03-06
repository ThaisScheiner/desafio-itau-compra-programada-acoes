namespace RebalanceamentosService.Api.Infrastructure.HttpClients.Dto;

public sealed class ClientesAtivosResponse
{
    public List<ClienteAtivoDto> Clientes { get; set; } = new();
}

public sealed class ClienteAtivoDto
{
    public long ClienteId { get; set; }
    public string Cpf { get; set; } = default!;
    public string Nome { get; set; } = default!;
    public decimal ValorMensal { get; set; }
}