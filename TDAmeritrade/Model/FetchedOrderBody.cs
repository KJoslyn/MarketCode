using Core.Model.Constants;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDAmeritrade.Model
{
    internal class FetchedOrderBody : OrderBody
    {
        public FetchedOrderBody(
            string complexOrderStrategyType,
            string orderType,
            string session,
            string price,
            string duration,
            string orderStrategyType,
            IList<OrderLeg> orderLegCollection)
            : base(complexOrderStrategyType, orderType, session, price, duration, orderStrategyType, orderLegCollection) { }

        public string OrderId { get; init; }
        public string Status { get; init; }
        public string EnteredTime { get; init; }

        [JsonIgnore]
        public bool IsOpenOrder
        {
            get => Status == OrderStatus.ACCEPTED ||
                Status == OrderStatus.QUEUED ||
                Status == OrderStatus.WORKING;
        }

        [JsonIgnore]
        public DateTime EnteredDateTime { get => DateTime.Parse(EnteredTime); }
    }
}
