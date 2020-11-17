using Core.Model;
using Core.Model.Constants;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core
{
    public abstract class LivePortfolioClient
    {
        public LivePortfolioClient(PositionDatabase positionDB)
        {
            PositionDB = positionDB;

            //Position pos1 = new Position("CGC_201120C24", 20, (float)0.93);
            //PositionDB.InsertPosition(pos1);
            //Position pos2 = new Position("SPWR_201120C20", 30, (float)0.57);
            //PositionDB.InsertPosition(pos2);

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

        protected PositionDatabase PositionDB { get; init; }

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
            IList<FilledOrder> sortedOrders = SortFilledOrdersByTime(filledOrders);
            return (topOrderDateTime, PositionDB.ComputeDeltasAndUpdateTables(sortedOrders));
        }

        // This does update the database so that the deltas remain accurate.
        // May throw InvalidPortfolioStateException if the portfolio is not in a valid state
        // (The portfolio may be offline, or its format may have changed.)
        public async Task<IList<PositionDelta>> GetLiveDeltasFromPositions()
        {
            IList<Position> livePositions = await RecognizeLivePositions();
            return PositionDB.ComputeDeltasAndUpdateTables(livePositions);
        }

        private IList<FilledOrder> SortFilledOrdersByTime(IList<FilledOrder> orders)
        {
            return orders.ToList().OrderBy(o => o.Time).ToList();
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
