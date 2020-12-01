using Core.Model;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#nullable enable

namespace Core
{
    public abstract class LivePortfolioClient
    {
        public LivePortfolioClient(PortfolioDatabase database, IMarketDataClient marketDataClient)
        {
            Database = database;
            MarketDataClient = marketDataClient;

            //Log.Information("INSERTING POSITIONS");
            //Position pos1 = new Position("ACB_201218C10", 10, (float)0.34);
            //database.InsertPosition(pos1);
            //Position pos3 = new Position("ACB_210115C10", 10, (float)0.76);
            //database.InsertPosition(pos3);
            //Position pos4 = new Position("ACB_201218C8", 10, (float)0.70);
            //database.InsertPosition(pos4);
            //Position pos5 = new Position("FIZZ_201218C115", 30, (float)1.25);
            //database.InsertPosition(pos5);
            //Position pos6 = new Position("FIZZ_201218C100", 10, (float)4.32);
            //database.InsertPosition(pos6);
            //Position pos7 = new Position("GLD_210219C180", 40, (float)2.33);
            //database.InsertPosition(pos7);
            //Position pos8 = new Position("IGC_201218C1.5", 50, (float)0.45);
            //database.InsertPosition(pos8);
            //Position pos9 = new Position("NFLX_201204C530", 60, (float)1.25);
            //database.InsertPosition(pos9);
            //Position pos10 = new Position("NFLX_201211C530", 10, (float)3.10);
            //database.InsertPosition(pos10);
            //Position pos11 = new Position("NIO_201204C55", 10, (float)2.99);
            //database.InsertPosition(pos11);

            //Position pos1 = new Position("NET_201127C65", 10, (float)2.62);
            //database.InsertPosition(pos1);
            //Position pos2 = new Position("OSTK_201127C65", 20, (float)1.07);
            //database.InsertPosition(pos2);
            //Position pos3 = new Position("SPWR_201127C20", 50, (float)1.05);
            //database.InsertPosition(pos3);
            //Position pos4 = new Position("TSLA_201127C500", 1, (float)16.14);
            //database.InsertPosition(pos4);

            //List<FilledOrder> orders = new List<FilledOrder>();
            //FilledOrder o1 = new FilledOrder("CGC_201120C25", (float)0.59, InstructionType.SELL_TO_CLOSE, OrderType.MARKET, 0, 30, new System.DateTime(2020, 11, 17, 10, 11, 56));
            //orders.Add(o1);
            //FilledOrder o2 = new FilledOrder("SFIX_201120C39", (float)0.30, InstructionType.SELL_TO_CLOSE, OrderType.MARKET, 0, 5, new System.DateTime(2020, 11, 13, 12, 23, 37));
            //orders.Add(o2);
            //FilledOrder o3 = new FilledOrder("SPWR_201120C20", (float)0.55, InstructionType.SELL_TO_CLOSE, OrderType.MARKET, 0, 20, new System.DateTime(2020, 11, 13, 12, 31, 13));
            //orders.Add(o3);
            //FilledOrder o4 = new FilledOrder("SFIX_201120C39", (float)0.22, InstructionType.SELL_TO_CLOSE, OrderType.MARKET, 0, 50, new System.DateTime(2020, 11, 13, 12, 33, 25));
            //orders.Add(o4);
            //FilledOrder o5 = new FilledOrder("SPWR_201120C20", (float)0.57, InstructionType.BUY_TO_OPEN, OrderType.MARKET, 0, 30, new System.DateTime(2020, 11, 13, 12, 35, 39));
            //orders.Add(o5);
            //FilledOrder o6 = new FilledOrder("CGC_201120C24", (float)1.16, InstructionType.SELL_TO_CLOSE, OrderType.MARKET, 0, 20, new System.DateTime(2020, 11, 13, 12, 53, 26));
            //orders.Add(o6);

            //database.InsertOrders(orders);
        }

        protected PortfolioDatabase Database { get; init; }

        protected IMarketDataClient MarketDataClient { get; init; }

        public abstract Task<bool> Login();

        public abstract Task<bool> Logout();

        public abstract Task<bool> HavePositionsChanged(bool? groundTruthChanged);

        public abstract Task<bool> HaveOrdersChanged(bool? groundTruthChanged);

        // This does not update the database.
        public abstract Task<IList<Position>> RecognizeLivePositions();

        protected abstract Task<UnvalidatedLiveOrdersResult> RecognizeLiveOrders();

        protected abstract Task<UnvalidatedLiveOrdersResult> RecognizeLiveOrders(string ordersFilename);

        public IEnumerable<Position> GetStoredPositions() => Database.GetStoredPositions();

        public async Task<bool> CheckLivePositionsAgainstDatabase()
        {

            IEnumerable<Position> livePositions = await RecognizeLivePositions();
            IEnumerable<Position> storedPositions = GetStoredPositions();
            bool result = true;

            if (livePositions.Count() != storedPositions.Count())
            {
                result = false;
            }
            foreach (Position livePos in livePositions)
            {
                Position? matchingStoredPos = storedPositions.FirstOrDefault(
                    pos => pos.Symbol == livePos.Symbol &&
                    pos.LongQuantity == livePos.LongQuantity);

                if (matchingStoredPos == null)
                {
                    result = false;
                }
            }
            if (!result)
            {
                Log.Warning("Stored positions do not match live positions. Stored Positions: {@StoredPositions}, Live Positions: {@LivePositions}", storedPositions, livePositions);
            }
            return result;
        }

        public async Task<LiveDeltasResult> GetLiveDeltasFromOrders()
        {
            UnvalidatedLiveOrdersResult unvalidatedLiveOrdersResult = await RecognizeLiveOrders();
            return GetLiveDeltasFromUnvalidatedOrders(unvalidatedLiveOrdersResult);
        }

        public async Task<LiveDeltasResult> GetLiveDeltasFromOrders(string ordersFilename)
        {
            UnvalidatedLiveOrdersResult unvalidatedLiveOrdersResult = await RecognizeLiveOrders(ordersFilename);
            return GetLiveDeltasFromUnvalidatedOrders(unvalidatedLiveOrdersResult);
        }

        // This does update the database so that the deltas remain accurate.
        // May throw InvalidPortfolioStateException if the portfolio is not in a valid state
        // (The portfolio may be offline, or its format may have changed.)
        public async Task<IList<PositionDelta>> GetLiveDeltasFromPositions()
        {
            IList<Position> livePositions = await RecognizeLivePositions();
            return Database.ComputeDeltasAndUpdateTables(livePositions);
        }

        private LiveDeltasResult GetLiveDeltasFromUnvalidatedOrders(UnvalidatedLiveOrdersResult unvalidatedLiveOrdersResult)
        {
            Dictionary<FilledOrder, OptionQuote> validatedLiveOrders = ValidateLiveOrders(unvalidatedLiveOrdersResult.LiveOrders);

            TimeSortedSet<PositionDelta> liveDeltas = new TimeSortedSet<PositionDelta>();
            if (validatedLiveOrders.Keys.Count > 0)
            {
                // TODO: Don't hardcode lookback
                NewAndUpdatedFilledOrders result = Database.IdentifyNewAndUpdatedOrders(new TimeSortedSet<FilledOrder>(validatedLiveOrders.Keys), 10);
                Database.UpdateOrders(result.UpdatedFilledOrders);
                liveDeltas = Database.ComputeDeltasAndUpdateTables(result.NewFilledOrders);
            }
            Dictionary<string, OptionQuote> quotes = BuildQuoteDictionary(validatedLiveOrders);

            return new LiveDeltasResult(liveDeltas, quotes, unvalidatedLiveOrdersResult.SkippedOrderDueToLowConfidence);
        }

        private Dictionary<string, OptionQuote> BuildQuoteDictionary(Dictionary<FilledOrder, OptionQuote> orderToQuoteDictionary)
        {
            Dictionary<string, OptionQuote> dict = new Dictionary<string, OptionQuote>();
            foreach((FilledOrder order, OptionQuote quote) in orderToQuoteDictionary)
            {
                if (!dict.ContainsKey(order.Symbol))
                {
                    dict.Add(order.Symbol, quote);
                }
            }
            return dict;
        }

        private Dictionary<FilledOrder, OptionQuote> ValidateLiveOrders(IEnumerable<FilledOrder> liveOrders)
        {
            Dictionary<FilledOrder, OptionQuote> validOrdersAndQuotes = new Dictionary<FilledOrder, OptionQuote>();
            foreach (FilledOrder order in liveOrders)
            {
                OptionQuote quote;
                try
                {
                    quote = MarketDataClient.GetOptionQuote(order.Symbol);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error getting quote for symbol {Symbol}", order.Symbol);
                    continue;
                }

                if (order.Price < quote.LowPrice ||
                    order.Price > quote.HighPrice)
                {
                    Log.Warning("Order price not within day's range- symbol {Symbol}, order {@Order}, quote {@Quote}", order.Symbol, order, quote);
                }
                else {
                    validOrdersAndQuotes.Add(order, quote);
                }
            }
            return validOrdersAndQuotes;
        }

        //// This does update the database so that the deltas remain accurate.
        //// May throw InvalidPortfolioStateException if the portfolio is not in a valid state
        //// (The portfolio may be offline, or its format may have changed.)
        //public async Task<(IList<Position>, IList<PositionDelta>)> GetLivePositionsAndDeltas()
        //{
        //    IList<Position> livePositions = await RecognizeLivePositions();
        //    IList<PositionDelta> deltas = database.ComputePositionDeltas(livePositions);
        //    database.UpdatePositionsAndDeltas(livePositions, deltas);
        //    return (livePositions, deltas);
        //}

        //// This does update the database so that the deltas remain accurate.
        //// May throw InvalidPortfolioStateException if the portfolio is not in a valid state
        //// (The portfolio may be offline, or its format may have changed.)
        //public async Task<(IList<Position>, IList<PositionDelta>)> GetLivePositionsAndDeltas(IList<PositionDelta> deltas)
        //{
        //    IList<Position> livePositions = await GetLivePositions();
        //    IList<PositionDelta> deltas = database.ComputePositionDeltas(livePositions);
        //    database.UpdatePositionsAndDeltas(null, deltas);
        //    return (null, deltas);
        //}
    }
}
