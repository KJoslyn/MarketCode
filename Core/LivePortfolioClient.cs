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

            //Position pos = new Position("SFIX_201120C39", 50, (float)0.90);
            //PositionDB.InsertPosition(pos);
            //Position pos2 = new Position("WKHS_201120C20", 30, (float)1.34);
            //PositionDB.InsertPosition(pos);

            //List<FilledOrder> orders = new List<FilledOrder>();
            //FilledOrder order = new FilledOrder("U_201120C120", (float)3.24, InstructionType.SELL_TO_CLOSE, OrderType.MARKET, 0, 10, new System.DateTime(2020, 11, 11, 9, 45, 14));
            //orders.Add(order);
            //PositionDB.InsertOrders(orders);
        }

        private PositionDatabase PositionDB { get; init; }

        public abstract Task<bool> Login();

        public abstract Task<bool> Logout();

        public abstract Task<bool> HavePositionsChanged(bool? groundTruthChanged);

        public abstract Task<bool> HaveOrdersChanged(bool? groundTruthChanged);

        protected abstract Task<IList<FilledOrder>> RecognizeLiveOrders();

        // This does not update the database, but the method is not public.
        protected abstract Task<IList<Position>> RecognizeLivePositions();

        public async Task<IList<PositionDelta>> GetLiveDeltasFromOrders()
        {
            IList<FilledOrder> filledOrders = await RecognizeLiveOrders();
            return PositionDB.ComputeDeltasAndUpdateTables(filledOrders);
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
