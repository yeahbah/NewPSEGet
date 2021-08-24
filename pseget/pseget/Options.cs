using CommandLine;

namespace pseget
{
    public class Options
    {
        [Option('r', "range", HelpText = "Specify the date range. e.g. -r 5/6/2020:7/1/2020. Use -r today to download today's csv file.", Required = true)]
        public string DateRange { get; set; }

        [Option('u', "url", Default = "https://documents.pse.com.ph/market_report/", HelpText = "The source URL of the stock quote file. Defaults to http://www.pse.com.ph/resource/dailyquotationreport/file/")]
        public string SourceUrl { get; set; }

        [Option('j', "report", Required = false, Default = ".", HelpText = "Generate a JSON report file in the specified location, e.g. -j c:\\reports.")]
        public string JsonReportLocation { get; set; }

        [Option('n', "include-name", Default = false, HelpText = "Include the stock name in the CSV file")]
        public bool IncludeStockName { get; set; }

        [Option('o', "output-location", Default = ".", HelpText = "The location where the csv file will be saved.")]
        public string OutputLocation { get; set; }
    }
}
