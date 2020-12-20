using Core.Model;
using System.Collections.Generic;
#nullable enable

namespace Core
{
    public interface IBrokerClient
    {
        public IEnumerable<Position> GetPositions();
        public Position? GetPosition(string symbol);
        public void PlaceOrder(Order order);
        public IEnumerable<Order> GetOpenOrdersForSymbol(string symbol);
        public void CancelExistingBuyOrders(string symbol);
    }
}
