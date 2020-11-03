using Core.Model;
using Core.Model.Constants;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#nullable enable

namespace Core
{
    public class PaperTradeBrokerClient : IBrokerClient
    {
        public PaperTradeBrokerClient(PositionDatabase positionDB, IMarketDataClient marketDataClient)
        {
            PositionDB = positionDB;
            MarketDataClient = marketDataClient;
        }

        private PositionDatabase PositionDB { get; init; }
        private IMarketDataClient MarketDataClient { get; init; }

        public Position? GetPosition(string symbol)
        {
            IList<Position> positions = GetPositions();
            return positions.Where(pos => pos.Symbol == symbol).FirstOrDefault();
        }

        public IList<Position> GetPositions()
        {
            return PositionDB.GetStoredPositions();
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
                PositionDB.DeletePosition(currentPos);
            }

            if (price <= 0)
            {
                if (order.OrderType == OrderType.LIMIT)
                {
                    price = order.Limit;
                }
                else
                {
                    OptionQuote quote = MarketDataClient.GetQuote(order.Symbol);
                    price = quote.Ask;
                }
            }

            PositionDelta delta = new PositionDelta(deltaType, order.Symbol, order.Quantity, price, percent);
            PositionDB.InsertDelta(delta);

            float oldQuantity = currentPos?.LongQuantity ?? 0;
            float oldAveragePrice = currentPos?.AveragePrice ?? 0;
            float newQuantity = oldQuantity + delta.Quantity;
            float newAvgPrice = (oldQuantity * oldAveragePrice + delta.Quantity * price) / newQuantity;
            Position newPos = new Position(order.Symbol, newQuantity, newAvgPrice);
            PositionDB.InsertPosition(newPos);
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
                    OptionQuote quote = MarketDataClient.GetQuote(order.Symbol);
                    price = quote.Bid;
                }
            }

            PositionDelta delta = new PositionDelta(DeltaType.SELL, order.Symbol, order.Quantity, price, percent);

            PositionDB.InsertDelta(delta);
            PositionDB.DeletePosition(currentPos);

            float newQuantity = currentPos.LongQuantity - delta.Quantity;
            if (newQuantity > 0)
            {
                Position newPos = new Position(order.Symbol, newQuantity, currentPos.AveragePrice);
                PositionDB.InsertPosition(newPos);
            }
        }
    }
}
