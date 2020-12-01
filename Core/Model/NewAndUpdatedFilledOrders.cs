using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class NewAndUpdatedFilledOrders
    {
        public NewAndUpdatedFilledOrders(TimeSortedCollection<FilledOrder> newFilledOrders, IEnumerable<UpdatedFilledOrder> updatedFilledOrders)
        {
            NewFilledOrders = newFilledOrders;
            UpdatedFilledOrders = updatedFilledOrders;
        }

        public TimeSortedCollection<FilledOrder> NewFilledOrders { get; }
        public IEnumerable<UpdatedFilledOrder> UpdatedFilledOrders { get; }
    }
}
