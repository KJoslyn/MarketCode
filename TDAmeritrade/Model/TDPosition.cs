using Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDAmeritrade.Model
{
    internal class TDPosition : Position
    {
        public TDPosition(string symbol, float longQuantity, float averagePrice) : base(symbol, longQuantity, averagePrice) { }

        public float ShortQuantity { get; init; }
        public Instrument Instrument { get; init; }
        public float MarketValue { get; init; }
        public override string Symbol { get => Instrument.Symbol; }
    }
}
