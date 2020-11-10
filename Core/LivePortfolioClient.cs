using Core.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core
{
    public abstract class LivePortfolioClient
    {
        public LivePortfolioClient(PositionDatabase positionDB)
        {
            PositionDB = positionDB;
        }

        private PositionDatabase PositionDB { get; init; }

        public abstract Task<bool> Logout();

        // This does not update the database, but the method is not public.
        protected abstract Task<IList<Position>> GetLivePositions();

        public abstract Task<bool> HasPortfolioChanged(bool? groundTruthChanged);

        // This does update the database so that the deltas remain accurate.
        // May throw InvalidPortfolioStateException if the portfolio is not in a valid state
        // (The portfolio may be offline, or its format may have changed.)
        public async Task<(IList<Position>, IList<PositionDelta>)> GetLivePositionsAndDeltas()
        {
            IList<Position> livePositions = await GetLivePositions();
            IList<PositionDelta> deltas = PositionDB.ComputePositionDeltas(livePositions);
            PositionDB.UpdatePositionsAndDeltas(livePositions, deltas);
            return (livePositions, deltas);
        }

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
