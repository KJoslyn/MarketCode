using Core.Model;
using Core.Model.Constants;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Core
{
    public abstract class LivePortfolioClient
    {
        public LivePortfolioClient(IPositionDatabase positionDB)
        {
            PositionDB = positionDB;
        }

        private IPositionDatabase PositionDB { get; init; }

        public abstract Task<bool> Logout();

        // This does not update the database, but the method is not public.
        protected abstract Task<IList<Position>> GetLivePositions();

        // This does update the database so that the deltas remain accurate.
        public async Task<(IList<Position>, IList<PositionDelta>)> GetLivePositionsAndDeltas()
        {
            IList<Position> livePositions = await GetLivePositions();
            IList<PositionDelta> deltas = ComputePositionDeltas(livePositions);
            PositionDB.UpdatePositionsAndDeltas(livePositions, deltas);
            return (livePositions, deltas);
        }

        private IList<PositionDelta> ComputePositionDeltas(IList<Position> livePositions)
        {
            IList<PositionDelta> deltas = new List<PositionDelta>();
            IList<Position> oldPositions = PositionDB.GetStoredPositions();

            foreach (Position livePos in livePositions)
            {
                Position? oldPos = oldPositions.Where(pos => pos.Symbol == livePos.Symbol).FirstOrDefault();
                if (oldPos == null)
                {
                    PositionDelta delta = new PositionDelta(
                        DeltaType.NEW,
                        livePos.Symbol,
                        livePos.LongQuantity,
                        livePos.AveragePrice,
                        1);
                    deltas.Add(delta);
                }
            }
            foreach (Position oldPos in oldPositions)
            {
                PositionDelta? delta = null;
                Position? livePos = livePositions.Where(pos => pos.Symbol == oldPos.Symbol).FirstOrDefault();
                if (livePos == null)
                {
                    delta = new PositionDelta(
                        DeltaType.SELL,
                        oldPos.Symbol,
                        oldPos.LongQuantity,
                        0, // Sell price is unknown
                        1);
                }
                else if (livePos.LongQuantity > oldPos.LongQuantity)
                {
                    float deltaContracts = livePos.LongQuantity - oldPos.LongQuantity;
                    float addPrice = (livePos.AveragePrice * livePos.LongQuantity - oldPos.AveragePrice * oldPos.LongQuantity) / deltaContracts;
                    delta = new PositionDelta(
                        DeltaType.ADD,
                        oldPos.Symbol,
                        deltaContracts,
                        addPrice,
                        deltaContracts / oldPos.LongQuantity);
                }
                else if (livePos.LongQuantity < oldPos.LongQuantity)
                {
                    float deltaContracts = oldPos.LongQuantity - livePos.LongQuantity;
                    delta = new PositionDelta(
                        DeltaType.SELL,
                        oldPos.Symbol,
                        deltaContracts,
                        0, // Sell price is unknown
                        deltaContracts / oldPos.LongQuantity); ;
                }

                if (delta != null)
                {
                    deltas.Add(delta);
                }
            }
            return deltas;
        }
    }
}
