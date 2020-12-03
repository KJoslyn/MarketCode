using Core.Model;
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

        public bool ValidateOrderAndGetQuote(UnvalidatedFilledOrder order, out OptionQuote quote)
        {
            quote = null;
            try
            {
                quote = GetOptionQuote(order.Symbol);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error getting quote for symbol {Symbol}", order.Symbol);
                return false;
            }

            if (order.Price < quote.LowPrice ||
                order.Price > quote.HighPrice)
            {
                Log.Warning("Order price not within day's range- symbol {Symbol}, order {@Order}, quote {@Quote}", order.Symbol, order, quote);
                return false;
            }
            return true;
        }
    }
}
