using Core.Model;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;
#nullable enable

namespace Core
{
    public abstract class LivePortfolioClient
    {
        public LivePortfolioClient(PortfolioDatabase database)
        {
            Database = database;

            //Log.Information("INSERTING POSITIONS");
            //Position pos1 = new Position("CVNA_201127C230", 2, (float)7.13);
            //PositionDB.InsertPosition(pos1);
            //Position pos2 = new Position("MARA_201218C2.5", 30, (float)1.09);
            //PositionDB.InsertPosition(pos2);
            //Position pos3 = new Position("MARA_201218C4", 50, (float)0.75);
            //PositionDB.InsertPosition(pos3);
            //Position pos4 = new Position("SFIX_201204C37", 30, (float)1.13);
            //PositionDB.InsertPosition(pos4);
            //Position pos5 = new Position("SPWR_201204C21", 20, (float)1.50);
            //PositionDB.InsertPosition(pos5);
            //Position pos6 = new Position("TSLA_201127C500", 2, (float)16.80);
            //PositionDB.InsertPosition(pos6);

            //Position pos1 = new Position("NET_201127C65", 10, (float)2.62);
            //PositionDB.InsertPosition(pos1);
            //Position pos2 = new Position("OSTK_201127C65", 20, (float)1.07);
            //PositionDB.InsertPosition(pos2);
            //Position pos3 = new Position("SPWR_201127C20", 50, (float)1.05);
            //PositionDB.InsertPosition(pos3);
            //Position pos4 = new Position("TSLA_201127C500", 1, (float)16.14);
            //PositionDB.InsertPosition(pos4);

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

            //PositionDB.InsertOrders(orders);
        }

        protected PortfolioDatabase Database { get; init; }

        public abstract Task<bool> Login();

        public abstract Task<bool> Logout();

        public abstract Task<bool> HavePositionsChanged(bool? groundTruthChanged);

        public abstract Task<bool> HaveOrdersChanged(bool? groundTruthChanged);

        protected abstract Task<LiveOrdersResult> RecognizeLiveOrders();

        protected abstract Task<LiveOrdersResult> RecognizeLiveOrders(string ordersFilename);

        // This does not update the database, but the method is not public.
        protected abstract Task<IList<Position>> RecognizeLivePositions();

        public async Task<LiveDeltasResult> GetLiveDeltasFromOrders()
        {
            LiveOrdersResult liveOrdersResult = await RecognizeLiveOrders();
            TimeSortedSet<PositionDelta> liveDeltas = new TimeSortedSet<PositionDelta>();
            if (liveOrdersResult.LiveOrders.Count > 0)
            {
                NewAndUpdatedFilledOrders result = Database.IdentifyNewAndUpdatedOrders(liveOrdersResult.LiveOrders, 10);
                Database.UpdateOrders(result.UpdatedFilledOrders);
                liveDeltas = Database.ComputeDeltasAndUpdateTables(result.NewFilledOrders);
            }
            return new LiveDeltasResult(liveDeltas, liveOrdersResult.SkippedOrderDueToLowConfidence);
        }

        public async Task<LiveDeltasResult> GetLiveDeltasFromOrders(string ordersFilename)
        {
            LiveOrdersResult liveOrdersResult = await RecognizeLiveOrders(ordersFilename);
            TimeSortedSet<PositionDelta> liveDeltas = new TimeSortedSet<PositionDelta>();
            if (liveOrdersResult.LiveOrders.Count > 0)
            {
                NewAndUpdatedFilledOrders result = Database.IdentifyNewAndUpdatedOrders(liveOrdersResult.LiveOrders, 10);
                Database.UpdateOrders(result.UpdatedFilledOrders);
                liveDeltas = Database.ComputeDeltasAndUpdateTables(result.NewFilledOrders);
            }
            return new LiveDeltasResult(liveDeltas, liveOrdersResult.SkippedOrderDueToLowConfidence);
        }

        // This does update the database so that the deltas remain accurate.
        // May throw InvalidPortfolioStateException if the portfolio is not in a valid state
        // (The portfolio may be offline, or its format may have changed.)
        public async Task<IList<PositionDelta>> GetLiveDeltasFromPositions()
        {
            IList<Position> livePositions = await RecognizeLivePositions();
            return Database.ComputeDeltasAndUpdateTables(livePositions);
        }

        //// This does update the database so that the deltas remain accurate.
        //// May throw InvalidPortfolioStateException if the portfolio is not in a valid state
        //// (The portfolio may be offline, or its format may have changed.)
        //public async Task<(IList<Position>, IList<PositionDelta>)> GetLivePositionsAndDeltas()
        //{
        //    IList<Position> livePositions = await RecognizeLivePositions();
        //    IList<PositionDelta> deltas = PositionDB.ComputePositionDeltas(livePositions);
        //    PositionDB.UpdatePositionsAndDeltas(livePositions, deltas);
        //    return (livePositions, deltas);
        //}

        //// This does update the database so that the deltas remain accurate.
        //// May throw InvalidPortfolioStateException if the portfolio is not in a valid state
        //// (The portfolio may be offline, or its format may have changed.)
        //public async Task<(IList<Position>, IList<PositionDelta>)> GetLivePositionsAndDeltas(IList<PositionDelta> deltas)
        //{
        //    IList<Position> livePositions = await GetLivePositions();
        //    IList<PositionDelta> deltas = PositionDB.ComputePositionDeltas(livePositions);
        //    PositionDB.UpdatePositionsAndDeltas(null, deltas);
        //    return (null, deltas);
        //}
    }
}
