using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class NewAndUpdatedFilledOrders
    {
        public NewAndUpdatedFilledOrders(TimeSortedSet<FilledOrder> newFilledOrders, IEnumerable<UpdatedFilledOrder> updatedFilledOrders)
        {
            NewFilledOrders = newFilledOrders;
            UpdatedFilledOrders = updatedFilledOrders;
        }

        public TimeSortedSet<FilledOrder> NewFilledOrders { get; }
        public IEnumerable<UpdatedFilledOrder> UpdatedFilledOrders { get; }
    }
}
