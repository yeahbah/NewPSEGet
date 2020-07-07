using CommandLine;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using iText;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf;
using System.Text;

namespace pseget
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(async option =>
                {
                    var fromDate = DateTime.Today;
                    var toDate = DateTime.Today;

                    if (!option.DateRange.Equals("today", StringComparison.OrdinalIgnoreCase))
                    {
                        var matches = new Regex(@"^((0?[1-9]|[12][0-9]|3[01])[- /.](0?[1-9]|1[012])[- /.](20)\d\d):((0?[1-9]|[12][0-9]|3[01])[- /.](0?[1-9]|1[012])[- /.](20)\d\d)$")
                            .Matches(option.DateRange);
                        if (matches.Count == 0)
                        {
                            throw new Exception($"{option.DateRange} is not a valid date range.");
                        }
                        fromDate = DateTime.Parse(matches.First().Groups[1].Value);
                        toDate = DateTime.Parse(matches.First().Groups[5].Value);
                    }

                    await DownloadPdfReport(option.SourceUrl, fromDate, toDate);
                });
            Console.ReadLine();
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
                var response = await client.GetAsync(downloadUrl);
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    Console.WriteLine(response.ReasonPhrase);                    
                }
                else
                {
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

                }
                downloadDate = downloadDate.AddDays(1);
            }          
        }
        
    }
}
