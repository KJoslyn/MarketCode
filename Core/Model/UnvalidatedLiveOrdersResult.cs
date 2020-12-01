using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class UnvalidatedLiveOrdersResult
    {
        public UnvalidatedLiveOrdersResult(TimeSortedCollection<FilledOrder> liveOrders, bool skippedOrderDueToLowConfidence)
        {
            LiveOrders = liveOrders;
            SkippedOrderDueToLowConfidence = skippedOrderDueToLowConfidence;
        }

        public TimeSortedCollection<FilledOrder> LiveOrders { get; }
        public bool SkippedOrderDueToLowConfidence { get; }
    }
}
