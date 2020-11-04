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
        private OrderConfig _config;

        public OrderManager(IBrokerClient brokerClient, IMarketDataClient marketDataClient, OrderConfig config)
        {
            BrokerClient = brokerClient;
            MarketDataClient = marketDataClient;
            _config = config;
        }

        private IBrokerClient BrokerClient { get; }
        private IMarketDataClient MarketDataClient { get; }

        public Order? DecideOrder(PositionDelta delta)
        {
            Position? currentPos = BrokerClient.GetPosition(delta.Symbol);
            if (delta.DeltaType == DeltaType.ADD)
            {
                return DecideAdd(delta, currentPos);
            }
            if (delta.DeltaType == DeltaType.NEW)
            {
                return DecideNew(delta, currentPos);
            }
            if (delta.DeltaType == DeltaType.SELL)
            {
                return DecideSell(delta, currentPos);
            }
            Log.Error("Unrecognized deltaType: {type}", delta.DeltaType);
            return null;
        }

        private Order? DecideAdd(PositionDelta delta, Position? currentPos)
        {
            Log.Information("ADD delta {@Delta}- taking no action. Symbol {Symbol}", delta, delta.Symbol);
            return null;
        }

        private Order? DecideNew(PositionDelta delta, Position? currentPos)
        {
            OptionQuote quote = MarketDataClient.GetQuote(delta.Symbol);
            // TODO: Remove after testing
            //Log.Warning("Not getting real quote");
            //OptionQuote quote = new OptionQuote(delta.Symbol, delta.Price * (float).99, delta.Price * (float)1.01, delta.Price, delta.Price * (float)1.06, (float)1.0);
            Log.Information("NEW delta {@Delta}- current mark price {Mark}. Symbol {Symbol}", delta, quote.Mark.ToString("0.00"), delta.Symbol);

            if (currentPos != null)
            {
                Log.Warning("Current position exists {@CurrentPosition} for new delta {@Delta}. Taking no action for Symbol {Symbol}", currentPos, delta, delta.Symbol);
                return null;
            }

            float diff = quote.Mark - delta.Price;
            float absPercent = Math.Abs(diff / delta.Price);
            bool withinHighThreshold = Math.Sign(diff) >= 0 && absPercent <= _config.HighBuyThreshold;
            bool withinLowThreshold = Math.Sign(diff) <= 0 && absPercent <= _config.LowBuyThreshold;

            int quantity;
            string orderType;
            float limit = -1;

            if (withinHighThreshold && _config.HighBuyStrategy == BuyStrategyType.MARKET ||
                withinLowThreshold && _config.LowBuyStrategy == BuyStrategyType.MARKET)
            {
                orderType = OrderType.MARKET;
                quantity = DecideNewBuyQuantity(quote.Ask); // Assume we will pay the ask price
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
                Log.Information("Current mark price not within buy threshold. Skipping order. Symbol {Symbol}, Mark={Mark}", delta.Symbol, quote.Mark.ToString("0.00"));
                return null;
            }
            Order order = new Order(delta.Symbol, quantity, InstructionType.BUY_TO_OPEN, orderType, limit);
            Log.Information("Decided new Order: {@Order} for Symbol {Symbol}", order, order.Symbol);
            return order;
        }

        private int DecideNewBuyQuantity(float price)
        {
            return (int)Math.Floor(_config.NewPositionSize / (price * 100));
        }

        // Currently, only market sell orders are supported
        private Order? DecideSell(PositionDelta delta, Position? currentPos)
        {
            Log.Information("SELL delta {@Delta}, Symbol {Symbol}", delta, delta.Symbol);

            if (currentPos == null)
            {
                Log.Information("No current position corresponding to delta {@Delta}. Symbol {Symbol}", delta, delta.Symbol);
                return null;
            }
            int quantity = (int)Math.Ceiling(delta.Percent * currentPos.LongQuantity);

            Order order = new Order(delta.Symbol, quantity, InstructionType.SELL_TO_CLOSE, OrderType.MARKET, -1);
            Log.Information("Decided sell order {@Order} for Symbol {Symbol}", order, order.Symbol);
            return order;
        }
    }
}
