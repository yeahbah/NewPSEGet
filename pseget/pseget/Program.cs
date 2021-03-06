﻿using CommandLine;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace pseget
{
    class Program
    {
        private static Options pseGetOption;

        static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(option =>
                {
                    try
                    {
                        Run(option).GetAwaiter().GetResult();
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, e.Message);
                        throw;
                    }
                });
        }

        static async Task Run(Options option)
        {
            Program.pseGetOption = option;
            var fromDate = DateTime.Today;
            var toDate = DateTime.Today;

            if (!option.DateRange.Equals("today", StringComparison.OrdinalIgnoreCase))
            {
                var dateRange = option.DateRange.Split(':');
                if (dateRange.Length == 0)
                {
                    throw new Exception($"{option.DateRange} is not a valid date range.");
                }
                fromDate = DateTime.Parse(dateRange[0].Trim());
                toDate = DateTime.Parse(dateRange[1].Trim());
                if (toDate.Date < fromDate.Date)
                {
                    throw new Exception("Invalid date range.");
                }
            }

            var downloadDate = fromDate;
            while (downloadDate.Date <= toDate.Date)
            {
                var pdfBytes = await DownloadReport(downloadDate);
                if (pdfBytes != null)                
                {
                    Log.Information($"Converting to CSV...");
                    await ConvertPdfBytesToCsv(pdfBytes, downloadDate);
                    Log.Information("Done.");
                }
                downloadDate = downloadDate.AddDays(1);
            }

            Log.Information("Download complete.");           
        }

        private static async Task ConvertPdfBytesToCsv(byte[] pdfBytes, DateTime tradeDate)
        {
            var pdfStream = new MemoryStream(pdfBytes);

            var reader = new PdfReader(pdfStream);
            var pdf = new PdfDocument(reader);
            var sb = new StringBuilder();
            for (var i = 1; i <= pdf.GetNumberOfPages(); i++)
            {
                var pdfPage = pdf.GetPage(i);
                var pdfText = PdfTextExtractor.GetTextFromPage(pdfPage);
                sb.Append(pdfText);
            }
            var parser = new PseReportParser();
            var pseModel = parser.Parse(sb.ToString());
            pseModel.TradeDate = tradeDate;

            var fileName = Path.Combine(Program.pseGetOption.OutputLocation, $"stockQuotes_{pseModel.TradeDate:MMddyyyy}.csv");
            await Converter.ToCsv(pseModel, fileName, Program.pseGetOption.IncludeStockName);
        }

        private static async Task<byte[]> DownloadReport(DateTime reportDate)
        {
            var client = new HttpClient();
            var pdfFile = $"stockQuotes_{reportDate:MMddyyyy}.pdf";
            var downloadUrl = Path.Combine(pseGetOption.SourceUrl, pdfFile);

            Log.Information($"Downloading {downloadUrl}...");

            var response = await client.GetAsync(downloadUrl);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Log.Warning(response.ReasonPhrase);
                return null;
            }
            return await response.Content.ReadAsByteArrayAsync();
        }
    }
}
