using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class OptionQuote
    {
        public OptionQuote(string symbol, float bid, float ask, float last, float mark, float mult, float hi, float low, long longTime)
        {
            Symbol = symbol;
            Bid = bid;
            Ask = ask;
            Last = last;
            Mark = mark;
            Multiplier = mult;
            HighPrice = hi;
            LowPrice = low;
            QuoteTimeInLong = longTime;
        }

        private DateTime? _time;

        public string Symbol { get; init; }
        public float Bid { get; init; }
        public float Ask { get; init; }
        public float Last { get; init; }
        public float Mark { get; init; }
        public float Multiplier { get; init; }
        public float HighPrice { get; init; }
        public float LowPrice { get; init; }
        public long QuoteTimeInLong { get; init; }
        public DateTime Time { get
            {
                if (_time == null)
                {
                    DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    _time = start.AddMilliseconds(QuoteTimeInLong).ToLocalTime();
                }
                return (DateTime)_time;
            } 
        }
    }
}
