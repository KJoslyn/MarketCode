using Core.Model;
using System.Collections.Generic;

namespace Core
{
    public interface IPositionDatabase
    {
        public IList<Position> GetStoredPositions();

        public IList<PositionDelta> GetStoredDeltas();

        public void UpdatePositionsAndDeltas(IList<Position> livePositions, IList<PositionDelta> positionDeltas);
    }
}
