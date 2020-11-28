using Core;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDAmeritrade.Model
{
    internal class TDOptionQuote : OptionQuote
    {
        private string _symbol;

        public TDOptionQuote(string symbol, float bid, float ask, float last, float mark, float mult, float hi, float low, long longTime)
            : base(symbol, bid, ask, last, mark, mult, hi, low, longTime) { }

        public override string Symbol
        {
            get => _symbol;
            init { _symbol = OptionSymbolUtils.ConvertToStandardDateFormat(value, Constants.TDOptionDateFormat); }
        }
    }
}
