using Core.Model;
using System.Collections.Generic;
#nullable enable

namespace Core
{
    public interface IBrokerClient
    {
        public IList<Position> GetPositions();
        public Position? GetPosition(string symbol);
        public OptionQuote GetQuote(string symbol);
        public void PlaceOrder(Order order);
    }
}
