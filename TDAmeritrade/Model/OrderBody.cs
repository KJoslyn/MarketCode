using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDAmeritrade.Model
{
    internal class OrderBody
    {
        public OrderBody(
            string complexOrderStrategyType,
            string orderType,
            string session,
            string price,
            string duration,
            string orderStrategyType,
            IList<OrderLeg> orderLegCollection) 
        {
            ComplexOrderStrategyType = complexOrderStrategyType;
            OrderType = orderType;
            Session = session;
            Price = price;
            Duration = duration;
            OrderStrategyType = orderStrategyType;
            OrderLegCollection = orderLegCollection;
        }

        public string ComplexOrderStrategyType { get; init; }

        public string OrderType { get; init; }

        public string Session { get; init; }

        public string Price { get; init; }

        public string Duration { get; init; }

        public string OrderStrategyType { get; init; }

        public IList<OrderLeg> OrderLegCollection { get; init; }
    }
}
