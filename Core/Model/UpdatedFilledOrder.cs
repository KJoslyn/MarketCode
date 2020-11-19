using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class UpdatedFilledOrder
    {
        public UpdatedFilledOrder(FilledOrder oldOrder, FilledOrder newOrder)
        {
            OldOrder = oldOrder;
            NewOrder = newOrder;
        }

        public FilledOrder OldOrder { get; }
        public FilledOrder NewOrder { get; }
    }
}
