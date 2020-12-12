using Core;
using Core.Model;
using Core.Model.Constants;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#nullable enable

namespace TDAmeritrade.Model
{
    internal class OrderBody : HasSymbolInStandardFormat
    {
        public OrderBody(
            string complexOrderStrategyType,
            string orderType,
            string session,
            string? price,
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
            Quantity = orderLegCollection[0].Quantity;
            Symbol = OptionSymbolUtils.ConvertDateFormat(orderLegCollection[0].Instrument.Symbol, Constants.TDOptionDateFormat, OptionSymbolUtils.StandardDateFormat);
        }

        public string ComplexOrderStrategyType { get; init; }
        public string OrderType { get; init; }
        public string Session { get; init; }
        public string? Price { get; init; }
        public string Duration { get; init; }
        public string OrderStrategyType { get; init; }
        public IList<OrderLeg> OrderLegCollection { get; init; }
        public float Quantity { get; init; }

        // Set by TD Ameritrade when an order is received, not by our constructor
        public string? OrderId { get; init; }
        public string? Status { get; init; }
        public string? EnteredTime { get; init; }

        [JsonIgnore]
        public string Instruction { get => OrderLegCollection[0].Instruction; }

        [JsonIgnore]
        public bool IsOpenOrder
        {
            get => Status == OrderStatus.ACCEPTED ||
                Status == OrderStatus.QUEUED ||
                Status == OrderStatus.WORKING;
        }

        [JsonIgnore]
        public override string Symbol { get => base.Symbol; init => base.Symbol = value; }
    }
}
