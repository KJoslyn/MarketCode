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

        public IList<Order> DecideOrdersTimeSorted(IList<PositionDelta> deltas)
        {
            IEnumerable<Order> orders = deltas.OrderBy(delta => delta.Time)
                .Select(delta => DecideOrder(delta))
                .OfType<Order>(); // filter out nulls
            return RemoveBuysIfSellExistsForSameSymbol(orders);
        }

        private Order? DecideOrder(PositionDelta delta)
        {
            // TODO: Remove after testing
            //Log.Warning("Not getting real quote");
            //OptionQuote quote = new OptionQuote(delta.Symbol, delta.Price * (float).99, delta.Price * (float)1.01, delta.Price, delta.Price * (float)1.06, (float)1.0);
            OptionQuote quote = MarketDataClient.GetQuote(delta.Symbol);
            Log.Information("{DeltaType} delta {@Delta}- current mark price {Mark}. Symbol {Symbol}", delta.DeltaType, delta, quote.Mark.ToString("0.00"), delta.Symbol);

            Position? currentPos = BrokerClient.GetPosition(delta.Symbol);
            if (delta.DeltaType == DeltaType.SELL)
            {
                return DecideSell(delta, currentPos);
            }
            else if (delta.DeltaType == DeltaType.ADD ||
                delta.DeltaType == DeltaType.NEW)
            {
                return DecideBuy(delta, currentPos, quote);
            }
            else
            {
                Log.Error("Unrecognized deltaType: {type}", delta.DeltaType);
                return null;
            }
        }

        private IList<Order> RemoveBuysIfSellExistsForSameSymbol(IEnumerable<Order> orders)
        {
            IList<Order> filteredOrders = orders.ToList();
            IEnumerable<string> symbols = filteredOrders.Select(order => order.Symbol)
                .Distinct();

            foreach(string symbol in symbols)
            {
                IEnumerable<Order> buyOrders = filteredOrders.Where(order => order.Instruction == InstructionType.BUY_TO_OPEN);
                Order? firstSellOrder = filteredOrders.Where(order => order.Instruction == InstructionType.SELL_TO_CLOSE).FirstOrDefault();
                if (firstSellOrder != null)
                {
                    foreach (Order buyOrder in buyOrders)
                    {
                        Log.Warning("Removing buy order {@BuyOrder} because corresponding sell order exists: {@SellOrder}", buyOrder, firstSellOrder);
                        filteredOrders.Remove(buyOrder);
                    }
                }
            }
            return filteredOrders;
        }

        private Order? DecideBuy(PositionDelta delta, Position? currentPos, OptionQuote quote)
        {
            if (delta.DeltaType == DeltaType.ADD && currentPos == null)
            {
                Log.Information("No current position corresponding to {DeltaType} delta {@Delta}. Symbol {Symbol}", delta.DeltaType, delta, delta.Symbol);
            }
            else if (delta.DeltaType == DeltaType.NEW && currentPos != null)
            {
                Log.Warning("Current position exists {@CurrentPosition} for {DeltaType} delta {@Delta}. Taking no action for Symbol {Symbol}", currentPos, delta.DeltaType, delta, delta.Symbol);
                return null;
            }

            if (delta.Age.TotalMinutes > _config.MinutesUntilBuyOrderExpires)
            {
                Log.Warning("New/Add delta expired after {Minutes} minutes for delta {@Delta}. Symbol {Symbol}", delta.Age.TotalMinutes.ToString("0"), delta, delta.Symbol);
                return null;
            }

            if (delta.Price > _config.MaxBuyPrice)
            {
                Log.Warning("Buy price higher than buy max limit. Skipping order. Symbol {Symbol}, Price={Price}", delta.Symbol, delta.Price);
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
                quantity = DecideBuyQuantity(quote.Ask, delta, currentPos); // Assume we will pay the ask price
            }
            else if (withinHighThreshold && _config.HighBuyStrategy == BuyStrategyType.DELTA_LIMIT ||
                withinLowThreshold && _config.LowBuyStrategy == BuyStrategyType.DELTA_LIMIT)
            {
                orderType = OrderType.LIMIT;
                limit = delta.Price;
                quantity = DecideBuyQuantity(limit, delta, currentPos);
            }
            else if (withinHighThreshold && _config.HighBuyStrategy == BuyStrategyType.THRESHOLD_LIMIT)
            {
                orderType = OrderType.LIMIT;
                limit = delta.Price * (1 + _config.HighBuyThreshold);
                quantity = DecideBuyQuantity(limit, delta, currentPos);
            }
            else if (withinLowThreshold && _config.LowBuyStrategy == BuyStrategyType.THRESHOLD_LIMIT)
            {
                orderType = OrderType.LIMIT;
                limit = delta.Price * (1 - _config.LowBuyThreshold);
                quantity = DecideBuyQuantity(limit, delta, currentPos);
            }
            else
            {
                Log.Information("Current mark price not within buy threshold. Skipping order. Symbol {Symbol}, Mark={Mark}", delta.Symbol, quote.Mark.ToString("0.00"));
                return null;
            }

            if (quantity == 0)
            {
                Log.Information("Decided buy quantity of 0. Skipping Order. Symbol {Symbol}, CurrentPosition = {@CurrentPosition}, Delta = {@Delta}",
                    delta.Symbol, currentPos, delta);
                return null;
            } else
            {
                Order order = new Order(delta.Symbol, quantity, InstructionType.BUY_TO_OPEN, orderType, limit);
                Log.Information("Decided new Order: {@Order} for Symbol {Symbol}", order, order.Symbol);
                return order;
            }
        }

        private int DecideBuyQuantity(float price, PositionDelta delta, Position? currentPos)
        {
            float currentPosTotalAlloc = currentPos != null
                ? currentPos.LongQuantity * currentPos.AveragePrice * 100
                : 0;

            int buyQuantity;
            if (delta.DeltaType == DeltaType.NEW ||
                delta.DeltaType == DeltaType.ADD && currentPos == null)
            {
                float deltaMarketValue = delta.Price * delta.Quantity * 100;
                float percentOfMaxSize = deltaMarketValue / _config.LivePortfolioPositionMaxSize;
                if (percentOfMaxSize > 1)
                {
                    Log.Warning("New position in live portfolio exceeds expected max size. Delta {@Delta}", delta);
                    percentOfMaxSize = 1;
                }
                buyQuantity = (int)Math.Floor((percentOfMaxSize * _config.MyPositionMaxSize) / (price * 100));
            }
            else if (delta.DeltaType == DeltaType.ADD && currentPos != null)
            {
                float addAlloc = currentPosTotalAlloc * delta.Percent;
                buyQuantity = (int)Math.Floor(addAlloc / (price * 100));
            }
            else
            {
                Log.Warning("Invalid delta type supplied to DecideBuyQuantity function. Delta {@Delta}", delta);
                return 0;
            }

            float newTotalAlloc = currentPosTotalAlloc + buyQuantity * price * 100;
            if (newTotalAlloc > _config.MyPositionMaxSize)
            {
                Log.Information("Buying " + buyQuantity + " {Symbol} would exceed maximum allocation of " + _config.MyPositionMaxSize.ToString("0.00"), delta.Symbol);
                return 0;
            }
            else
            {
                return buyQuantity;
            }
        }

        // Currently, only market sell orders are supported
        private Order? DecideSell(PositionDelta delta, Position? currentPos)
        {
            if (currentPos == null)
            {
                Log.Information("No current position corresponding to delta {@Delta}. Symbol {Symbol}", delta, delta.Symbol);
                return null;
            }
            if (delta.Age.TotalMinutes > _config.MinutesUntilWarnOldSellOrder)
            {
                Log.Warning("Sell delta OLD after {Minutes} minutes for delta {@Delta}. Symbol {Symbol}", delta.Age.TotalMinutes.ToString("0"), delta, delta.Symbol);
            }
            int quantity = (int)Math.Ceiling(delta.Percent * currentPos.LongQuantity);

            Order order = new Order(delta.Symbol, quantity, InstructionType.SELL_TO_CLOSE, OrderType.MARKET, -1);
            Log.Information("Decided sell order {@Order} for Symbol {Symbol}", order, order.Symbol);
            return order;
        }
    }
}
