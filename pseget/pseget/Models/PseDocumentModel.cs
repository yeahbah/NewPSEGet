using System;
using System.Collections.Generic;

namespace Pseget.Models
{
    public record PseDocumentModel
    {
        public IEnumerable<StockModel> Stocks { get; init; }
        public DateOnly TradeDate { get; init; }
    }
}
