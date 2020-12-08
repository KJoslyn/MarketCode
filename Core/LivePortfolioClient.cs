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

            Log.Information("INSERTING POSITIONS");
            Database.InsertPosition(new Position("BR_201218C145", 5, (float)5.40));
            Database.InsertPosition(new Position("MARA_201218C6", 30, (float)0.90));
            Database.InsertPosition(new Position("Z_201218C105", 5, (float)5.70));
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
