using Core.Model;
using Core.Model.Constants;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#nullable enable

namespace Core
{
    public class PaperTradeBrokerClient : IBrokerClient
    {
        public PaperTradeBrokerClient(PortfolioDatabase positionDB, MarketDataClient marketDataClient)
        {
            Database = positionDB;
            MarketDataClient = marketDataClient;
        }

        public PortfolioDatabase Database { get; init; }
        private MarketDataClient MarketDataClient { get; init; }

        public Position? GetPosition(string symbol)
        {
            IEnumerable<Position> positions = GetPositions();
            return positions.Where(pos => pos.Symbol == symbol).FirstOrDefault();
        }

        public IEnumerable<Position> GetPositions()
        {
            return Database.GetStoredPositions();
        }

        public void PlaceOrder(Order order)
        {
            PlaceOrder(order, 0);
        }

        public void PlaceOrder(Order order, float price = 0)
        {
            if (order.Instruction == InstructionType.SELL_TO_CLOSE)
            {
                PlaceSellOrder(order, price);
            }
            else if (order.Instruction == InstructionType.BUY_TO_OPEN)
            {
                PlaceBuyOrder(order, price);
            }
            else
            {
                Exception ex = new OrderException("Unrecognized transaction type: " + order.Instruction);
                Log.Error(ex, "Unrecognized transaction type in order {@Order}", order);
                throw ex;
            }
            OrderBody orderBody = CreateOrderBody(order);
            string orderBodyStr = JsonConvert.SerializeObject(orderBody, new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Ignore,
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                }
            });
            Log.Information("TDAm Order: {@Order}, string: {OrderStr}, Symbol {Symbol}", orderBody, orderBodyStr, order.Symbol);
        }

        public IEnumerable<Order> GetOpenOrdersForSymbol(string symbol)
        {
            return new List<Order>();
        }

        public void CancelExistingBuyOrders(string symol) { }

        public float GetAvailableFundsForTrading()
        {
            return float.MaxValue;
        }

        private void PlaceBuyOrder(Order order, float price = 0)
        {
            Position? currentPos = GetPosition(order.Symbol);

            float percent = 0;
            string deltaType;
            if (currentPos == null)
            {
                deltaType = DeltaType.NEW;
            } else
            {
                percent = order.Quantity / currentPos.LongQuantity;
                deltaType = DeltaType.ADD;
                Database.DeletePosition(currentPos);
            }

            if (price <= 0)
            {
                if (order.OrderType == OrderType.LIMIT)
                {
                    price = order.Limit;
                }
                else
                {
                    OptionQuote quote = MarketDataClient.GetOptionQuote(order.Symbol);
                    price = quote.AskPrice;
                }
            }

            PositionDelta delta = new PositionDelta(deltaType, order.Symbol, order.Quantity, price, percent);
            Database.InsertDeltaAndUpsertUsedUnderlyingSymbol(delta);

            float oldQuantity = currentPos?.LongQuantity ?? 0;
            float oldAveragePrice = currentPos?.AveragePrice ?? 0;
            float newQuantity = oldQuantity + delta.Quantity;
            float newAvgPrice = (oldQuantity * oldAveragePrice + delta.Quantity * price) / newQuantity;
            Position newPos = new Position(order.Symbol, newQuantity, newAvgPrice);
            Database.InsertPosition(newPos);
        }

        private void PlaceSellOrder(Order order, float price = 0)
        {
            Position? currentPos = GetPosition(order.Symbol);
            if (currentPos == null)
            {
                Exception ex = new OrderException("No current position corresponding to order");
                Log.Error(ex, "No current position corresponding to order {@Order}", order);
                throw ex;
            }
            float percent = order.Quantity / currentPos.LongQuantity;

            if (price <= 0)
            {
                if (order.OrderType == OrderType.LIMIT)
                {
                    price = order.Limit;
                }
                else
                {
                    OptionQuote quote = MarketDataClient.GetOptionQuote(order.Symbol);
                    price = quote.BidPrice;
                }
            }

            PositionDelta delta = new PositionDelta(DeltaType.SELL, order.Symbol, order.Quantity, price, percent);

            Database.InsertDeltaAndUpsertUsedUnderlyingSymbol(delta);
            Database.DeletePosition(currentPos);

            float newQuantity = currentPos.LongQuantity - delta.Quantity;
            if (newQuantity > 0)
            {
                Position newPos = new Position(order.Symbol, newQuantity, currentPos.AveragePrice);
                Database.InsertPosition(newPos);
            }
        }

        private OrderBody CreateOrderBody(Order order)
        {
            Instrument instrument = new Instrument(order.Symbol, AssetType.OPTION);
            OrderLeg orderLeg = new OrderLeg(order.Instruction, order.Quantity, instrument);
            List<OrderLeg> orderLegCollection = new List<OrderLeg>();
            orderLegCollection.Add(orderLeg);
            string? priceStr = null;
            if (order.OrderType == OrderType.LIMIT)
            {
                double doublePrice = Math.Round(order.Limit, 2);
                priceStr = doublePrice.ToString();
            }

            OrderBody orderBody = new OrderBody(
                "NONE",
                order.OrderType,
                "NORMAL",
                priceStr,
                "DAY",
                "SINGLE",
                orderLegCollection);

            return orderBody;
        }
    }

    internal class Instrument
    {
        #pragma warning disable CS8618
        public Instrument(string symbol, string assetType)
        {
            Symbol = symbol;
            AssetType = assetType;
        }
        #pragma warning restore CS8618

        public string AssetType { get; init; }
        public string Cusip { get; init; }
        public string Symbol { get; init; }
        public string Description { get; init; }
        public string PutCall { get; init; }
        public string UnderlyingSymbol { get; init; }
        public float OptionMultiplier { get; init; }
    }

    internal class OrderLeg
    {
        public OrderLeg (string instruction, int quantity, Instrument instrument) 
        {
            Instruction = instruction;
            Quantity = quantity;
            Instrument = instrument;
        }

        public string Instruction { get; init; }
        public int Quantity { get; init; }
        public Instrument Instrument { get; init; }
    }

    internal class OrderBody
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
        }

        public string ComplexOrderStrategyType { get; init; }

        public string OrderType { get; init; }

        public string Session { get; init; }

        public string? Price { get; init; }

        public string Duration { get; init; }

        public string OrderStrategyType { get; init; }

        public IList<OrderLeg> OrderLegCollection { get; init; }
    }
}
