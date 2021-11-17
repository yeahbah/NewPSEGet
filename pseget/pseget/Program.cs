using CommandLine;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Pseget
{
    internal static class Program
    {
        private static Options pseGetOption;

        private static void Main(string[] args)
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

        private static async Task Run(Options option)
        {
            Program.pseGetOption = option;
            var fromDate = DateOnly.FromDateTime(DateTime.Today);
            var toDate = DateOnly.FromDateTime(DateTime.Today);

            if (!option.DateRange.Equals("today", StringComparison.OrdinalIgnoreCase))
            {
                var dateRange = option.DateRange.Split(':');
                if (dateRange.Length == 0)
                {
                    throw new Exception($"{option.DateRange} is not a valid date range.");
                }
                fromDate = DateOnly.Parse(dateRange[0].Trim());
                toDate = DateOnly.Parse(dateRange[1].Trim());
                if (toDate < fromDate)
                {
                    throw new Exception("Invalid date range.");
                }
            }

            var downloadDate = fromDate;
            while (downloadDate <= toDate)
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

        private static async Task ConvertPdfBytesToCsv(byte[] pdfBytes, DateOnly tradeDate)
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
            var pseModel = parser.Parse(sb.ToString(), tradeDate);
            
            var fileName = Path.Combine(Program.pseGetOption.OutputLocation, $"stockQuotes_{pseModel.TradeDate:MMddyyyy}.csv");
            await Converter.ToCsv(pseModel, fileName, Program.pseGetOption.IncludeStockName);
        }

        private static async Task<byte[]> DownloadReport(DateOnly reportDate)
        {
            var client = new HttpClient();
            //var pdfFile = $"stockQuotes_{reportDate:MMddyyyy}.pdf";
            //new file name: August 24, 2021-EOD1.pdf
            var pdfFile = reportDate.ToString("MMMM dd, yyyy") + "-EOD.pdf";// "August 24, 2021-EOD1.pdf";
            var downloadUrl = Path.Combine(pseGetOption.SourceUrl, pdfFile);

            Log.Information($"Downloading {downloadUrl}...");

            var response = await client.GetAsync(downloadUrl);
            switch (response.StatusCode)
            {
                case HttpStatusCode.NotFound:
                    Log.Information($"I can't find {downloadUrl}. Trying again...");
                    pdfFile = reportDate.ToString("MMMM dd, yyyy") + "-EOD1.pdf";// "August 24, 2021-EOD1.pdf";
                    downloadUrl = Path.Combine(pseGetOption.SourceUrl, pdfFile);

                    Log.Information($"Downloading {downloadUrl}...");
                    break;
                case HttpStatusCode.OK:
                    return await response.Content.ReadAsByteArrayAsync();
            }

            Log.Warning(response.ReasonPhrase);
            return null;
        }
    }
}
