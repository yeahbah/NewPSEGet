﻿using pseget.Models;
using Serilog;
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
            var stocks = GetStocks(pdfText).ToList();
            var indeces = GetIndeces(pdfText);
            CalculateIndexNfb(indeces, pdfText);
            stocks.AddRange(indeces);
            return new PseDocumentModel
            {
                Stocks = stocks
            };
        }

        private IEnumerable<StockModel> GetStocks(string pdfText)
        {
            // match groups
            // group 0: matched line, ASIA UNITED AUB 46 46.9 46.75 46.75 46.05 46.05 6,000 277,200 -            
            // group 1: symbol
            var pattern = @"(\b[A-Z0-9]+\b)\s+((((\(?\d{1,3}(,\d{3})*(\.\d+)?\)?))|-)\s|\n){9}(-?)";
            var matches = new Regex(pattern).Matches(pdfText);
            if (matches.Count == 0) return null;

            var result = new List<StockModel>();
            foreach (var match in matches.AsEnumerable())
            {
                var line = match.Groups[0].Value.Trim();
                var stockSymbol = match.Groups[1].Value.Trim();
                var numbers = line
                    .Split(' ')
                    .Reverse()
                    .Take(9)
                    .Select(x => x.Trim())
                    .Select(x =>
                    {
                        if (x.Contains("\n"))
                        {
                            return x.Split("\n")[1];
                        }
                        return x;
                    })
                    .ToArray();

                // skip stocks that did not open
                if (numbers[6] == "-") continue;                

                decimal amount = 0;
                Log.Debug(line);

                var stock = new StockModel
                {
                    Description = GetStockName(stockSymbol, pdfText),
                    Symbol = stockSymbol,
                    NetForeignBuy = decimal.TryParse(numbers[0], NumberStyles.Any, null, out amount) ? amount : 0,
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

        private IEnumerable<StockModel> GetIndeces(string pdfText)
        {
            var pattern = @"(Financials|Industrials|Holding Firms|Property|Services|Mining & Oil|PSEi|All Shares)\s+(((((\(?\d{1,3}(,\d{3})*(\.\d+)?\)?))|-)\s|\n){8}|((((\(?\d{1,3}(,\d{3})*(\.\d+)?\)?))|-)\s|\n){6})";
            var matches = Regex.Matches(pdfText, pattern);
            if (matches.Count != 8) throw new Exception("Unable to parse index values");

            var result = new List<StockModel>();
            foreach (var match in matches.AsEnumerable())
            {
                var line = match.Groups[0].Value.Trim();
                var description = match.Groups[1].Value.Trim();
                var numbers = line
                        .Split(' ')
                        .Reverse()
                        .ToArray();
                var netForeign = 0m;
                if (description == "PSEi" || description == "All Shares")
                {
                    var temp = numbers
                        .Take(6)
                        .Select(x => x.Trim())
                        .ToList();

                    if (description == "PSEi")
                    {
                        var grandTotal = Regex.Match(pdfText, @"(GRAND TOTAL)\s+(((((\(?\d{1,3}(,\d{3})*(\.\d+)?\)?))|-)\s|\n){8}|((((\(?\d{1,3}(,\d{3})*(\.\d+)?\)?))|-)\s|\n){2})");
                        if (!grandTotal.Success) throw new Exception("Unable to find GRAND TOTAL");
                        var totals = grandTotal.Groups[2].Value.Split(' ');
                        temp.Insert(0, totals[1].Trim()); // psei value
                        temp.Insert(1, totals[0].Trim()); // pseri volume                        
                        netForeign = GetNetForeign(pdfText);
                    }
                    else
                    {
                        temp.Insert(0, "0.0");
                        temp.Insert(1, "0.0");
                    }
                    numbers = temp.ToArray();
                }
                else
                {
                    numbers = numbers
                        .Take(9)
                        .Select(x => x.Trim())
                        .ToArray();
                }
                
                Log.Debug(line);
                //decimal amount = 0;
                var stock = new StockModel
                {
                    Description = description,
                    Symbol = GetIndexSymbol(description),
                    NetForeignBuy = netForeign,
                    Value = decimal.Parse(numbers[0], NumberStyles.Any),
                    Volume = ulong.Parse(numbers[1], NumberStyles.Any),
                    Close = decimal.Parse(numbers[4], NumberStyles.Any),
                    Low = decimal.Parse(numbers[5], NumberStyles.Any),
                    High = decimal.Parse(numbers[6], NumberStyles.Any),
                    Open = decimal.Parse(numbers[7], NumberStyles.Any),
                };
                result.Add(stock);
            }

            return result;
        }

        private decimal GetNetForeign(string pdfText)
        {
            var pattern = @"(NET FOREIGN BUYING/\(SELLING\)\: Php)\s+(\S+)";
            var match = Regex.Match(pdfText, pattern);
            if (!match.Success) throw new Exception("Unable to find NFB");

            return decimal.Parse(match.Groups[2].Value.Trim(), NumberStyles.Any);
        }

        private const string Financials = "^FINANCIALS";
        private const string Industrials = "^INDUSTRIAL";
        private const string Holding = "^HOLDING";
        private const string Property = "^PROPERTY";
        private const string Services = "^SERVICE";
        private const string Mining = "^MINING-OIL";
        private const string AllShares = "^ALLSHARES";
        private const string PSEi = "^PSEi";

        private string GetIndexSymbol(string indexName)
        {
            return indexName switch
            {
                "Financials" => Financials,
                "Industrials" => Industrials,
                "Holding Firms" => Holding,
                "Property" => Property,
                "Services" => Services,
                "Mining & Oil" => Mining,
                "All Shares" => AllShares,
                "PSEi" => PSEi,
                _ => throw new InvalidOperationException($"{indexName} is unknown")
            };
        }

        private string GetStockName(string stockSymbol, string pdfText)
        {
            var pattern = @"(.+)\s+(" + stockSymbol + @")\s+((((\(?\d{1,3}(,\d{3})*(\.\d+)?\)?))|-)\s|\n){9}";
            var match = Regex.Match(pdfText, pattern);
            if (!match.Success) return string.Empty; // maybe throw exception?

            return match.Groups[1].Value;

        }

        private void CalculateIndexNfb(IEnumerable<StockModel> indeces, string pdfText)
        {            
            var pattern = @"F I N A N C I A L S((.|\n)+)FINANCIALS SECTOR TOTAL VOLUME";
            var matchText = Regex.Match(pdfText, pattern).Value;
            var sector = indeces.SingleOrDefault(index => index.Symbol == Financials);
            var stocksInSector = GetStocks(matchText);
            sector.NetForeignBuy = stocksInSector
                .Sum(stock => stock.NetForeignBuy);

            pattern = @"I N D U S T R I A L((.|\n)+)INDUSTRIAL SECTOR TOTAL VOLUME";
            matchText = Regex.Match(pdfText, pattern).Value;
            sector = indeces.SingleOrDefault(index => index.Symbol == Industrials);
            stocksInSector = GetStocks(matchText);
            sector.NetForeignBuy = stocksInSector
                .Sum(stock => stock.NetForeignBuy);

            pattern = @"H O L D I N G   F I R M S((.|\n)+)HOLDING FIRMS SECTOR TOTAL VOLUME";
            matchText = Regex.Match(pdfText, pattern).Value;
            sector = indeces.SingleOrDefault(index => index.Symbol == Holding);
            stocksInSector = GetStocks(matchText);
            sector.NetForeignBuy = stocksInSector
                .Sum(stock => stock.NetForeignBuy);

            pattern = @"P R O P E R T Y((.|\n)+)PROPERTY SECTOR TOTAL VOLUME";
            matchText = Regex.Match(pdfText, pattern).Value;
            sector = indeces.SingleOrDefault(index => index.Symbol == Property);
            stocksInSector = GetStocks(matchText);
            sector.NetForeignBuy = stocksInSector
                .Sum(stock => stock.NetForeignBuy);

            pattern = @"S E R V I C E S((.|\n)+)SERVICES SECTOR TOTAL VOLUME";
            matchText = Regex.Match(pdfText, pattern).Value;
            sector = indeces.SingleOrDefault(index => index.Symbol == Services);
            stocksInSector = GetStocks(matchText);
            sector.NetForeignBuy = stocksInSector
                .Sum(stock => stock.NetForeignBuy);

            pattern = @"M I N I N G   &   O I L((.|\n)+)MINING & OIL SECTOR TOTAL VOLUME";
            matchText = Regex.Match(pdfText, pattern).Value;
            sector = indeces.SingleOrDefault(index => index.Symbol == Mining);
            stocksInSector = GetStocks(matchText);
            sector.NetForeignBuy = stocksInSector
                .Sum(stock => stock.NetForeignBuy);
        }
    }

}
