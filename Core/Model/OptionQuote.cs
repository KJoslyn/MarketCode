using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class OptionQuote
    {
        public string Symbol { get; init; }
        public float BidPrice { get; init; }
        public float AskPrice { get; init; }
        public float LastPrice { get; init; }
        public float Mark { get; init; }
        public float Multiplier { get; init; }
    }
}
