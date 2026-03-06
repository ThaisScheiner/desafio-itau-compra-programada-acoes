namespace MotorCompraService.Api.Infrastructure.HttpClients.Dto
{
    public sealed class EventoIrDedoDuroRequest
    {
        public long ClienteId { get; set; }
        public string Ticker { get; set; } = default!;
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        public decimal ValorOperacao { get; set; }
        public decimal Aliquota { get; set; } = 0.00005m; // 0,005%
        public decimal ValorIR { get; set; }
        public DateTime DataEvento { get; set; }
    }
}
