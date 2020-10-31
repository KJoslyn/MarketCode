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
    public class OrderManager
    {
        private IBrokerClient _client;
        private OrderConfig _config;

        public OrderManager(IBrokerClient client, OrderConfig config)
        {
            _client = client;
            _config = config;
        }

        public Order? DecideOrder(PositionDelta delta)
        {
            if (delta.DeltaType == DeltaType.ADD)
            {
                return DecideAdd(delta);
            }
            if (delta.DeltaType == DeltaType.NEW)
            {
                return DecideNew(delta);
            }
            if (delta.DeltaType == DeltaType.SELL)
            {
                return DecideSell(delta);
            }
            Log.Error("Unrecognized deltaType: {type}", delta.DeltaType);
            return null;
        }

        private Order? DecideAdd(PositionDelta delta)
        {
            Log.Information("ADD delta {@Delta}- taking no action", delta);
            return null;
        }

        private Order? DecideNew(PositionDelta delta)
        {
            OptionQuote quote = _client.GetQuote(delta.Symbol);
            Log.Information("NEW delta {@Delta}- current mark price {Mark}", delta, quote.Mark);

            float diff = quote.Mark - delta.Price;
            float absPercent = Math.Abs(diff / delta.Price);
            bool withinHighThreshold = Math.Sign(diff) >= 0 && absPercent <= _config.HighBuyThreshold;
            bool withinLowThreshold = Math.Sign(diff) <= 0 && absPercent <= _config.LowBuyThreshold;

            float quantity;
            string orderType;
            float limit = -1;

            if (withinHighThreshold && _config.HighBuyStrategy == BuyStrategyType.MARKET ||
                withinLowThreshold && _config.LowBuyStrategy == BuyStrategyType.MARKET)
            {
                orderType = OrderType.MARKET;
                quantity = DecideNewBuyQuantity(quote.AskPrice); // Assume we will pay the ask price
            }
            else if (withinHighThreshold && _config.HighBuyStrategy == BuyStrategyType.DELTA_LIMIT ||
                withinLowThreshold && _config.LowBuyStrategy == BuyStrategyType.DELTA_LIMIT)
            {
                orderType = OrderType.LIMIT;
                limit = delta.Price;
                quantity = DecideNewBuyQuantity(limit);
            }
            else if (withinHighThreshold && _config.HighBuyStrategy == BuyStrategyType.THRESHOLD_LIMIT)
            {
                orderType = OrderType.LIMIT;
                limit = delta.Price * (1 + _config.HighBuyThreshold);
                quantity = DecideNewBuyQuantity(limit);
            }
            else if (withinLowThreshold && _config.LowBuyStrategy == BuyStrategyType.THRESHOLD_LIMIT)
            {
                orderType = OrderType.LIMIT;
                limit = delta.Price * (1 - _config.LowBuyThreshold);
                quantity = DecideNewBuyQuantity(limit);
            }    
            else
            {
                Log.Information("Current mark price not within buy threshold. Skipping order.");
                return null;
            }
            Order order = new Order(delta.Symbol, quantity, TransactionType.BUY_TO_OPEN, orderType, limit);
            Log.Information("Decided new Order: {@Order}", order);
            return order;
        }

        private float DecideNewBuyQuantity(float price)
        {
            return (float)Math.Floor(_config.NewPositionSize / (price * 100));
        }

        // Currently, only market sell orders are supported
        private Order? DecideSell(PositionDelta delta)
        {
            Log.Information("SELL delta {@Delta}", delta);

            Position? currentPos = _client.GetPosition(delta.Symbol);
            if (currentPos == null)
            {
                Log.Information("No current position corresponding to delta {@Delta}", delta);
                return null;
            }
            float quantity = (float)Math.Ceiling(delta.Percent * currentPos.LongQuantity);

            Order order = new Order(delta.Symbol, quantity, TransactionType.SELL_TO_CLOSE, OrderType.MARKET, -1);
            Log.Information("Decided sell order {@Order}", order);
            return order;
        }
    }
}
