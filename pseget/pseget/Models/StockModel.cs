namespace pseget.Models
{
    public class StockModel
    {
		public string Symbol { get; set; }

		public string Description { get; set; }

		public decimal Open { get; set; }

		public decimal High { get; set; }

		public decimal Low { get; set; }

		public decimal Close { get; set; }

		public ulong Volume { get; set; }

		public decimal Value { get; set; }

		public decimal NetForeignBuy { get; set; }
	}
}
