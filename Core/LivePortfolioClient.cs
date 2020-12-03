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
        public LivePortfolioClient(PortfolioDatabase database, MarketDataClient marketDataClient)
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

        protected MarketDataClient MarketDataClient { get; init; }

        public abstract Task<bool> Login();

        public abstract Task<bool> Logout();

        public abstract Task<bool> HavePositionsChanged(bool? groundTruthChanged);

        public abstract Task<bool> HaveOrdersChanged(bool? groundTruthChanged);

        // This does not update the database.
        public abstract Task<IEnumerable<Position>> RecognizeLivePositions();

        protected abstract Task<TimeSortedCollection<FilledOrder>> RecognizeLiveOrders(string? ordersFilename = null);

        public async Task<bool> CheckLivePositionsAgainstDatabasePositions()
        {

            IEnumerable<Position> livePositions = await RecognizeLivePositions();
            IEnumerable<Position> storedPositions = Database.GetStoredPositions();
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

        public async Task<IEnumerable<PositionDelta>> GetLiveDeltasFromPositions()
        {
            IEnumerable<Position> livePositions = await RecognizeLivePositions();
            return Database.ComputeDeltasAndUpdateTables(livePositions);
        }

        public async Task<TimeSortedCollection<PositionDelta>> GetLiveDeltasFromOrders(string? ordersFilename = null)
        {
            TimeSortedCollection<FilledOrder> liveOrders = await RecognizeLiveOrders(ordersFilename);

            if (liveOrders.Count == 0)
            {
                return new TimeSortedCollection<PositionDelta>();
            }

            // TODO: Don't hardcode lookback
            NewAndUpdatedFilledOrders result = Database.IdentifyNewAndUpdatedOrders(liveOrders, 10);
            Database.UpdateOrders(result.UpdatedFilledOrders);

            return Database.ComputeDeltasAndUpdateTables(result.NewFilledOrders);
        }
    }
}
