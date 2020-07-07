using pseget.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace pseget
{
    public interface IPseReportParser
    {
        PseDocumentModel Parse(string pdfText);
    }

    public class PseReportParser : IPseReportParser
    {
        public PseDocumentModel Parse(string pdfText)
        {
            // match groups
            // group 0: matched line, ASIA UNITED AUB 46 46.9 46.75 46.75 46.05 46.05 6,000 277,200 -
            // group 1: stock name
            // group 2: symbol
            var pattern = @"(.+)(\b[A-Z0-9]+\b\s)((((\(?\d{1,3}(,\d{3})*(\.\d+)?\)?))|-)\s|\n){9}";
            var matches = new Regex(pattern).Matches(pdfText);
            if (matches.Count == 0) return null;

            return new PseDocumentModel
            {
                Stocks = GetStocks(matches)
            };
        }

        private IEnumerable<StockModel> GetStocks(MatchCollection matches)
        {
            var result = new List<StockModel>();
            for (var i = 0; i < matches.Count - 1; i++)
            {
                var match = matches[i];
                var line = matches[i].Groups[0].Value;
                var numbers = line                    
                    .Split(' ')
                    .Reverse()
                    .Take(9)
                    .Select(x => x.Trim())
                    .ToArray();

                // skip stocks that did not open
                if (numbers[6] == "-") continue;

                decimal amount = 0;
                var stock = new StockModel
                {
                    Description = match.Groups[1].Value.Trim(),
                    Symbol = match.Groups[2].Value.Trim(),
                    NetForeignBuy = decimal.TryParse(numbers[0], NumberStyles.Any, null, out amount) ? amount : (decimal?)null,
                    Value = decimal.Parse(numbers[1], NumberStyles.Any),
                    Volume = ulong.Parse(numbers[2], NumberStyles.Any),
                    Close = decimal.Parse(numbers[3], NumberStyles.Any),
                    Low = decimal.Parse(numbers[4], NumberStyles.Any),
                    High = decimal.Parse(numbers[5], NumberStyles.Any),
                    Open = decimal.Parse(numbers[6], NumberStyles.Any),
                };
                result.Add(stock);
            }
            return result;
        }
    }

}
