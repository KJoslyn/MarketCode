using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class FilledOrderMatchResult
    {
        public FilledOrderMatchResult(IEnumerable<UpdatedFilledOrder> updatedFilledOrders, TimeSortedSet<FilledOrder> newFilledOrders)
        {
            UpdatedFilledOrders = updatedFilledOrders;
            NewFilledOrders = newFilledOrders;
        }

        public IEnumerable<UpdatedFilledOrder> UpdatedFilledOrders { get; }
        public TimeSortedSet<FilledOrder> NewFilledOrders { get; }
    }
}
