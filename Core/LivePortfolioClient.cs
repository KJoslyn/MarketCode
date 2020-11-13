using Core.Model;
using Core.Model.Constants;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core
{
    public abstract class LivePortfolioClient
    {
        public LivePortfolioClient(PositionDatabase positionDB)
        {
            PositionDB = positionDB;

            Position pos1 = new Position("SFIX_201120C39", 100, (float)0.76);
            PositionDB.InsertPosition(pos1);
            Position pos2 = new Position("SPWR_201120C20", 90, (float)0.90);
            PositionDB.InsertPosition(pos2);

            List<FilledOrder> orders = new List<FilledOrder>();
            FilledOrder o1 = new FilledOrder("WKHS_201120C20", (float)1.24, InstructionType.BUY_TO_OPEN, OrderType.LIMIT, (float)1.25, 10, new System.DateTime(2020, 11, 12, 10, 03, 29));
            orders.Add(o1);
            FilledOrder o2 = new FilledOrder("WKHS_201120C20", (float)1.25, InstructionType.BUY_TO_OPEN, OrderType.LIMIT, (float)1.25, 10, new System.DateTime(2020, 11, 12, 10, 03, 30));
            orders.Add(o2);
            FilledOrder o3 = new FilledOrder("WKHS_201120C20", (float)1.60, InstructionType.SELL_TO_CLOSE, OrderType.MARKET, 0, 70, new System.DateTime(2020, 11, 12, 10, 31, 30));
            orders.Add(o3);
            FilledOrder o4 = new FilledOrder("SPWR_201120C20", (float)0.63, InstructionType.BUY_TO_OPEN, OrderType.MARKET, 0, 20, new System.DateTime(2020, 11, 12, 11, 00, 29));
            orders.Add(o4);
            FilledOrder o5 = new FilledOrder("SFIX_201120C39", (float)0.53, InstructionType.BUY_TO_OPEN, OrderType.LIMIT, (float)0.54, 20, new System.DateTime(2020, 11, 12, 11, 37, 17));
            orders.Add(o5);
            FilledOrder o6 = new FilledOrder("SPWR_201120C20", (float)0.84, InstructionType.BUY_TO_OPEN, OrderType.LIMIT, (float)0.84, 10, new System.DateTime(2020, 11, 12, 12, 11, 07));
            orders.Add(o6);
            FilledOrder o7 = new FilledOrder("SPWR_201120C20", (float)0.74, InstructionType.BUY_TO_OPEN, OrderType.LIMIT, (float)0.74, 10, new System.DateTime(2020, 11, 12, 12, 25, 38));
            orders.Add(o7);
            FilledOrder o8 = new FilledOrder("SPWR_201120C20", (float)0.74, InstructionType.BUY_TO_OPEN, OrderType.LIMIT, (float)0.74, 10, new System.DateTime(2020, 11, 12, 12, 46, 58));
            orders.Add(o8);
            PositionDB.InsertOrders(orders);
        }

        private PositionDatabase PositionDB { get; init; }

        public abstract Task<bool> Login();

        public abstract Task<bool> Logout();

        public abstract Task<bool> HavePositionsChanged(bool? groundTruthChanged);

        public abstract Task<bool> HaveOrdersChanged(bool? groundTruthChanged);

        // TODO: Remove first part of tuple
        protected abstract Task<(string, IList<FilledOrder>)> RecognizeLiveOrders();

        // This does not update the database, but the method is not public.
        protected abstract Task<IList<Position>> RecognizeLivePositions();

        // TODO: Remove first part of tuple
        public async Task<(string, IList<PositionDelta>)> GetLiveDeltasFromOrders()
        {
            (string topOrderDateTime, IList<FilledOrder> filledOrders) = await RecognizeLiveOrders();
            return (topOrderDateTime, PositionDB.ComputeDeltasAndUpdateTables(filledOrders));
        }

        // This does update the database so that the deltas remain accurate.
        // May throw InvalidPortfolioStateException if the portfolio is not in a valid state
        // (The portfolio may be offline, or its format may have changed.)
        public async Task<IList<PositionDelta>> GetLiveDeltasFromPositions()
        {
            IList<Position> livePositions = await RecognizeLivePositions();
            return PositionDB.ComputeDeltasAndUpdateTables(livePositions);
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
