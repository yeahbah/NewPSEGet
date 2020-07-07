using CommandLine;

namespace pseget
{
    public class Options
    {
        [Option('r', "range", HelpText = "Specify the date range. e.g. -r 5/6/2020:7/1/2020. Use -r today to download today's csv file.", Required = true)]
        public string DateRange { get; set; }

        [Option('u', "url", Default = "http://www.pse.com.ph/resource/dailyquotationreport/file/", HelpText = "The source URL of the stock quote file. Defaults to http://www.pse.com.ph/resource/dailyquotationreport/file/")]
        public string SourceUrl { get; set; }

        [Option('j', "report", Required = false, Default = ".", HelpText = "Generate a JSON report file in the specified location, e.g. -j c:\\reports.")]
        public string JsonReportLocation { get; set; }

        [Option('n', "include-name", Default = false, HelpText = "Include the stock name in the result")]
        public bool IncludeStockName { get; set; }
    }
}
