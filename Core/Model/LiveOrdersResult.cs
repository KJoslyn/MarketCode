﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class LiveOrdersResult
    {
        public LiveOrdersResult(TimeSortedSet<FilledOrderAndQuote> liveOrdersAndQuotes, bool skippedOrderDueToLowConfidence)
        {
            LiveOrdersAndQuotes = liveOrdersAndQuotes;
            SkippedOrderDueToLowConfidence = skippedOrderDueToLowConfidence;
        }

        public TimeSortedSet<FilledOrderAndQuote> LiveOrdersAndQuotes { get; }
        public bool SkippedOrderDueToLowConfidence { get; }
    }
}
