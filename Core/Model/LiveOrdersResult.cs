using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class LiveOrdersResult
    {
        public LiveOrdersResult(TimeSortedCollection<FilledOrderAndQuote> liveOrdersAndQuotes, bool skippedOrderDueToLowConfidence)
        {
            LiveOrdersAndQuotes = liveOrdersAndQuotes;
            SkippedOrderDueToLowConfidence = skippedOrderDueToLowConfidence;
        }

        public TimeSortedCollection<FilledOrderAndQuote> LiveOrdersAndQuotes { get; }
        public bool SkippedOrderDueToLowConfidence { get; }
    }
}
