using System.Globalization;
using System.Text;

namespace CotacoesService.Api.Infrastructure.Cotahist;

public sealed class CotahistRegistro
{
    public DateTime DataPregao { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string CodigoBDI { get; set; } = string.Empty; 
    public int TipoMercado { get; set; } 
    public decimal PrecoAbertura { get; set; }
    public decimal PrecoMaximo { get; set; }
    public decimal PrecoMinimo { get; set; }
    public decimal PrecoFechamento { get; set; }
}

public sealed class CotahistParser
{
    public IEnumerable<CotahistRegistro> ParseArquivo(string caminhoArquivo)
    {
        // COTAHIST ISO-8859-1
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = Encoding.GetEncoding("ISO-8859-1");

        foreach (var linha in File.ReadLines(caminhoArquivo, encoding))
        {
            if (linha.Length < 245) continue;

            // TIPREG 1-2
            var tipreg = linha.Substring(0, 2);
            if (tipreg != "01") continue;

            // DATPRE 3-10
            var dataPregao = DateTime.ParseExact(
                linha.Substring(2, 8), "yyyyMMdd", CultureInfo.InvariantCulture);

            // CODBDI 11-12
            var bdi = linha.Substring(10, 2).Trim();

            // CODNEG 13-24
            var ticker = linha.Substring(12, 12).Trim().ToUpperInvariant();

            // TPMERC 25-27 (010 vista, 020 fracionario)
            var tpmercStr = linha.Substring(24, 3).Trim();
            if (!int.TryParse(tpmercStr, out var tpmerc)) continue;

            // Filtrar apenas BDI 02 (lote) e 96 (fracionário)
            if (bdi != "02" && bdi != "96") continue;

            // Filtrar apenas 010 e 020
            if (tpmerc != 10 && tpmerc != 20) continue;

            yield return new CotahistRegistro
            {
                DataPregao = dataPregao,
                CodigoBDI = bdi,
                Ticker = ticker,
                TipoMercado = tpmerc,
                PrecoAbertura = ParsePreco(linha.Substring(56, 13)),
                PrecoMaximo = ParsePreco(linha.Substring(69, 13)),
                PrecoMinimo = ParsePreco(linha.Substring(82, 13)),
                PrecoFechamento = ParsePreco(linha.Substring(108, 13)) //(fechamento)
            };
        }
    }

    private static decimal ParsePreco(string bruto)
    {
        if (!long.TryParse(bruto.Trim(), out var v)) return 0m;
        return v / 100m;
    }
}