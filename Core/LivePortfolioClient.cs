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
            //Database.InsertPosition(new Position("ACB_201218C8", 5, (float)0.70));
            //Database.InsertPosition(new Position("ACB_210115C10", 10, (float)0.76));
            //Database.InsertPosition(new Position("AMZN_201204C3220", 1, (float)26.41));
            //Database.InsertPosition(new Position("EXPE_201204C125", 10, (float)2.32));
            //Database.InsertPosition(new Position("EXPE_201211C125", 5, (float)3.84));
            //Database.InsertPosition(new Position("FIZZ_201218C100", 15, (float)4.41));
            //Database.InsertPosition(new Position("FIZZ_201218C115", 30, (float)1.25));
            //Database.InsertPosition(new Position("NFLX_201204C530", 60, (float)0.78));
            //Database.InsertPosition(new Position("NFLX_201211C530", 20, (float)2.32));
            //Database.InsertPosition(new Position("NIO_201204C55", 20, (float)1.94));
            //Database.InsertPosition(new Position("WKHS_201204C28", 40, (float)1.15));
            //Database.InsertPosition(new Position("WMT_201204C155", 50, (float)0.45));
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

        // TODO: Remove
        public NewAndUpdatedFilledOrders IdentifyNewAndUpdatedOrders(TimeSortedCollection<FilledOrder> liveOrders, double lookbackMinutes)
        {
            return Database.IdentifyNewAndUpdatedOrders(liveOrders, lookbackMinutes);
        }

        public async Task<bool> CheckLivePositionsAgainstDatabasePositions()
        {

            IEnumerable<Position> livePositions = await RecognizeLivePositions();
            IEnumerable<Position> storedPositions = GetStoredPositions();
            bool result = true;

            if (livePositions.Count() != storedPositions.Count())
            {
                Log.Warning("Stored position count does not match live position count. Stored Positions: {@StoredPositions}, Live Positions: {@LivePositions}", storedPositions, livePositions);
                result = false;
            }
            foreach (Position livePos in livePositions)
            {
                Position? matchingStoredPos = storedPositions.FirstOrDefault(
                    pos => pos.Symbol == livePos.Symbol &&
                    pos.LongQuantity == livePos.LongQuantity);

                if (matchingStoredPos == null)
                {
                    Log.Warning("Stored position not found for live position {@LivePosition}. Stored Positions: {@StoredPositions}, Live Positions: {@LivePositions}", 
                        livePos, storedPositions, livePositions);
                    result = false;
                }
            }
            return result;
        }

        public async Task<IList<PositionDelta>> GetLiveDeltasFromPositions()
        {
            IList<Position> livePositions = await RecognizeLivePositions();
            return Database.ComputeDeltasAndUpdateTables(livePositions);
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

        private LiveDeltasResult GetLiveDeltasFromUnvalidatedOrders(UnvalidatedLiveOrdersResult unvalidatedLiveOrdersResult)
        {
            TimeSortedCollection<PositionDelta> liveDeltas = new TimeSortedCollection<PositionDelta>();
            Dictionary<string, OptionQuote> quotes = new Dictionary<string, OptionQuote>();

            if (unvalidatedLiveOrdersResult.LiveOrders.Count > 0)
            {
                // TODO: Don't hardcode lookback
                NewAndUpdatedFilledOrders result = Database.IdentifyNewAndUpdatedOrders(new TimeSortedCollection<FilledOrder>(unvalidatedLiveOrdersResult.LiveOrders), 10);
                Database.UpdateOrders(result.UpdatedFilledOrders);

                TimeSortedCollection<FilledOrder> validNewOrders;
                (validNewOrders, quotes) = ValidateNewOrdersAndGetQuotes(result.NewFilledOrders);
                liveDeltas = Database.ComputeDeltasAndUpdateTables(validNewOrders);
            }

            return new LiveDeltasResult(liveDeltas, quotes, unvalidatedLiveOrdersResult.SkippedOrderDueToLowConfidence);
        }

        private (TimeSortedCollection<FilledOrder>, Dictionary<string, OptionQuote>) ValidateNewOrdersAndGetQuotes(IEnumerable<FilledOrder> unvalidatedNewOrders)
        {
            TimeSortedCollection<FilledOrder> validNewFilledOrders = new TimeSortedCollection<FilledOrder>();
            Dictionary<string, OptionQuote> newOrderQuotes = new Dictionary<string, OptionQuote>();

            foreach (FilledOrder order in unvalidatedNewOrders)
            {
                bool valid = ValidateOrderAndGetQuote(order, out OptionQuote? quote);
                if (valid && quote != null)
                {
                    validNewFilledOrders.Add(order);
                    if (!newOrderQuotes.ContainsKey(order.Symbol))
                    {
                        newOrderQuotes.Add(order.Symbol, quote);
                    }
                }
            }

            return (validNewFilledOrders, newOrderQuotes);
        }

        private bool ValidateOrderAndGetQuote(FilledOrder order, out OptionQuote? quote)
        {
            quote = null;
            try
            {
                quote = MarketDataClient.GetOptionQuote(order.Symbol);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error getting quote for symbol {Symbol}", order.Symbol);
                return false;
            }

            if (order.Price < quote.LowPrice ||
                order.Price > quote.HighPrice)
            {
                Log.Warning("Order price not within day's range- symbol {Symbol}, order {@Order}, quote {@Quote}", order.Symbol, order, quote);
                return false;
            }
            return true;
        }
    }
}
