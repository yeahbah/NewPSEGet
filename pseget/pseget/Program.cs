using CommandLine;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
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
                    Program.pseGetOption = option;
                    var fromDate = DateTime.Today;
                    var toDate = DateTime.Today;

                    if (!option.DateRange.Equals("today", StringComparison.OrdinalIgnoreCase))
                    {
                        //var matches = new Regex(@"^((0?[1-9]|[12][0-9]|3[01])[- /.](0?[1-9]|1[012])[- /.](20)\d\d):((0?[1-9]|[12][0-9]|3[01])[- /.](0?[1-9]|1[012])[- /.](20)\d\d)$")
                        //    .Matches(option.DateRange);
                        var dateRange = option.DateRange.Split(':');
                        if (dateRange.Length == 0)
                        {
                            throw new Exception($"{option.DateRange} is not a valid date range.");
                        }
                        fromDate = DateTime.Parse(dateRange[0].Trim());
                        toDate = DateTime.Parse(dateRange[1].Trim());
                    }

                    try
                    {
                        DownloadPdfReport(option.SourceUrl, fromDate, toDate).GetAwaiter().GetResult();
                        Log.Information("Download complete.");
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, e.Message);
                        throw;
                    }
                    
                });
        }

        static async Task DownloadPdfReport(string baseUrl, DateTime fromDate, DateTime toDate)
        {
            if (toDate.Date < fromDate.Date)
            {
                throw new Exception("Invalid date range.");
            }
            var client = new HttpClient();
            var downloadDate = fromDate;
            while (downloadDate.Date <= toDate.Date)
            {
                var pdfFile = $"stockQuotes_{downloadDate:MMddyyyy}.pdf";
                var downloadUrl = Path.Combine(baseUrl, pdfFile);

                Log.Information($"Downloading from {baseUrl}...");

                var response = await client.GetAsync(downloadUrl);
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    Log.Warning(response.ReasonPhrase);                    
                }
                else
                {
                    Log.Information($"Converting {pdfFile} to CSV...");
                    var bytes = await response.Content.ReadAsByteArrayAsync();
                    var pdfStream = new MemoryStream(bytes);

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
                    pseModel.TradeDate = downloadDate;
                    
                    var fileName = Path.Combine(Program.pseGetOption.OutputLocation, $"stockQuotes_{pseModel.TradeDate:MMddyyyy}.csv");
                    await Converter.ToCsv(pseModel, fileName, Program.pseGetOption.IncludeStockName);
                    Log.Information("Done.");

                }
                downloadDate = downloadDate.AddDays(1);
            }          
        }
        
    }
}
