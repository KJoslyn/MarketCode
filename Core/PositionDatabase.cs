using Core.Model;
using Core.Model.Constants;
using System.Collections.Generic;
using System.Linq;
#nullable enable

namespace Core
{
    public abstract class PositionDatabase
    {
        public abstract IList<Position> GetStoredPositions();

        public abstract IList<PositionDelta> GetStoredDeltas();

        public abstract void UpdatePositionsAndDeltas(IList<Position> livePositions, IList<PositionDelta> positionDeltas);

        public abstract void InsertDelta(PositionDelta delta);

        public abstract void InsertPosition(Position position);

        public abstract void DeletePosition(Position position);

        public IList<PositionDelta> ComputePositionDeltas(IList<Position> livePositions)
        {
            IList<PositionDelta> deltas = new List<PositionDelta>();
            IList<Position> oldPositions = GetStoredPositions();

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
