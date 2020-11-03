using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class OptionQuote
    {
        public OptionQuote(string symbol, float bid, float ask, float last, float mark, float mult)
        {
            Symbol = symbol;
            Bid = bid;
            Ask = ask;
            Last = last;
            Mark = mark;
            Multiplier = mult;
        }

        public string Symbol { get; init; }
        public float Bid { get; init; }
        public float Ask { get; init; }
        public float Last { get; init; }
        public float Mark { get; init; }
        public float Multiplier { get; init; }
    }
}
