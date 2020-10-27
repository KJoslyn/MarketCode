﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDAmeritrade.Model
{
    internal class Instrument
    {
        public string AssetType { get; init; }
        public string Cusip { get; init; }
        public string Symbol { get; init; }
        public string Description { get; init; }
        public string PutCall { get; init; }
        public string UnderlyingSymbol { get; init; }
        public float OptionMultiplier { get; init; }
    }
}
