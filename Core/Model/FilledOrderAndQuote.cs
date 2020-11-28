using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class FilledOrderAndQuote : HasTime
    {
        public FilledOrderAndQuote(FilledOrderAndQuote order, OptionQuote quote)
        {
            Order = order;
            Quote = quote;
            Time = order.Time;
        }

        public FilledOrderAndQuote Order { get; init; }
        public DateTime Time { get; init; }
        public OptionQuote Quote { get; set; }
    }
}
