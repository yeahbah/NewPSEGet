using System;
using System.Collections.Generic;

namespace pseget.Models
{
    public class PseDocumentModel
    {
        public IEnumerable<StockModel> Stocks { get; set; }
        public DateTime TradeDate { get; set; }

        //public int NumAdvance { get; set; }

        //public int NumDeclines { get; set; }
        
        //public int NumUnchanged { get; set; }
        
        //public int NumTraded { get; set; }
        
        //public int NumTrades { get; set; }
        
        //public ulong OddLotVolume { get; set; }
        
        //public decimal OddLotValue { get; set; }
        
        //public ulong BlockSaleVolume { get; set; }
        
        //public decimal BlockSaleValue { get; set; }
        
        //public ulong MainCrossVolume { get; set; }
        
        //public decimal MainCrossValue { get; set; }
        
        //public decimal BondsVolume { get; set; }
        
        //public decimal BondsValue { get; set; }
        
        //public string ExchangeNotice { get; set; }
        
        //public decimal TotalForeignBuying { get; set; }
        
        //public decimal TotalForeignSelling { get; set; }
        
        //public decimal NetForeignBuying => this.TotalForeignBuying - this.TotalForeignSelling;
        
        //public string BlockSales { get; set; }
        
    }
}
