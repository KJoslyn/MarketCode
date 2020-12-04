using Core.Model;
using Core.Model.Constants;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#nullable enable

namespace Core
{
    public abstract class MarketDataClient
    {
        public abstract OptionQuote GetOptionQuote(string symbol);

        public abstract bool IsMarketOpenToday();

        public bool ValidateWithinTodaysRangeAndGetQuote(UnvalidatedFilledOrder order, out OptionQuote? quote)
        {
            return ValidateWithinTodaysRangeAndGetQuote(order.Symbol, order.Price, out quote);
        }

        public bool ValidateWithinTodaysRangeAndGetQuote(string symbol, float price, out OptionQuote? quote)
        {
            quote = null;
            try
            {
                quote = GetOptionQuote(symbol);
            }
            catch (Exception ex)
            {
                Log.Information(ex, "Error getting quote for symbol {Symbol}", symbol);
                return false;
            }

            if (price < quote.LowPrice ||
                price > quote.HighPrice)
            {
                Log.Information("Price not within day's range- symbol {Symbol}, quote {@Quote}", symbol, quote);
                return false;
            }
            return true;
        }
    }
}
