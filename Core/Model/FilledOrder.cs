using System;

namespace Core.Model
{
    public class FilledOrder : UnvalidatedFilledOrder
    {
        public FilledOrder(UnvalidatedFilledOrder o, OptionQuote quote)
            : base(o.Symbol, o.Price, o.Instruction, o.OrderType, o.Limit, o.Quantity, o.Time)
        {
            Quote = quote;
        }

        public FilledOrder(string symbol, float price, string instruction, string orderType, float limit, int quantity, DateTime time, OptionQuote quote)
            : base(symbol, price, instruction, orderType, limit, quantity, time)
        {
            Quote = quote;
        }

        public FilledOrder() { }

        public OptionQuote Quote { get; set; }
    }
}
