using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pseget.Models;

namespace Pseget
{
    public static class Converter
    {        
        public static async Task ToCsv(PseDocumentModel pseModel, string fileName, bool includeStockName = false)
        {
            var sb = new StringBuilder();
            var stocks = pseModel.Stocks.Where(stock => !stock.Symbol.Contains("^"));
            var tradeDate = pseModel.TradeDate.ToString("MM/dd/yyyy");
            foreach (var stock in stocks)
            {
                var line = $"{stock.Symbol}";
                if (includeStockName)
                {
                    line += $",{stock.Description}";
                }
                var nfb = Math.Truncate(stock.NetForeignBuy);
                line += $",{tradeDate},{ stock.Open},{ stock.High},{ stock.Low},{ stock.Close},{ stock.Volume},{nfb}";
                sb.AppendLine(line);
            }
            var indexes = pseModel.Stocks.Where(stock => stock.Symbol.Contains("^"));
            foreach(var index in indexes)
            {
                var line = $"{index.Symbol}";
                if (includeStockName)
                {
                    line += $",";
                }
                var volume = Math.Truncate(index.Value / 1000);
                var nfb = Math.Truncate(index.NetForeignBuy / 1000);

                line += $",{tradeDate},{ index.Open},{ index.High},{ index.Low},{ index.Close},{ volume },{ nfb }";
                sb.AppendLine(line);
            }

            await File.WriteAllTextAsync(fileName, sb.ToString());
        }
    }
}
