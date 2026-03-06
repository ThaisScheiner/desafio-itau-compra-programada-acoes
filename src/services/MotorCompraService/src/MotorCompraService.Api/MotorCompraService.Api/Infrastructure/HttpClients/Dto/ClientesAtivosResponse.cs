namespace MotorCompraService.Api.Infrastructure.HttpClients.Dto;

public sealed class ClientesAtivosResponse
{
    public List<ClienteAtivoDto> Clientes { get; set; } = new();
}

public sealed class ClienteAtivoDto
{
    public long ClienteId { get; set; }
    public string Cpf { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal ValorMensal { get; set; }
}