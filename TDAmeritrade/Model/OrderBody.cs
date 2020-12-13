using Core;
using Core.Model;
using Core.Model.Constants;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDAmeritrade.Model
{
    internal class OrderBody : HasSymbolInStandardFormat
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
            Quantity = orderLegCollection[0].Quantity;
            Symbol = OptionSymbolUtils.ConvertDateFormat(orderLegCollection[0].Instrument.Symbol, Constants.TDOptionDateFormat, OptionSymbolUtils.StandardDateFormat);
        }

        public string ComplexOrderStrategyType { get; init; }
        public string OrderType { get; init; }
        public string Session { get; init; }
        public string Price { get; init; }
        public string Duration { get; init; }
        public string OrderStrategyType { get; init; }
        public IList<OrderLeg> OrderLegCollection { get; init; }
        public float Quantity { get; init; }

        [JsonIgnore]
        public override string Symbol { get => base.Symbol; init => base.Symbol = value; }

        [JsonIgnore]
        public string Instruction { get => OrderLegCollection[0].Instruction; }

        public Order ToOrder()
        {
            bool success = float.TryParse(Price, out float price);
            price = success ? price : 0;
            return new Order(Symbol, (int)Quantity, Instruction, OrderType, price);
        }
    }
}
