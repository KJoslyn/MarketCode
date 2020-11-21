using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class LiveDeltasResult
    {
        public LiveDeltasResult(TimeSortedSet<PositionDelta> liveDeltas, bool skippedDeltaDueToLowConfidence)
        {
            LiveDeltas = liveDeltas;
            SkippedDeltaDueToLowConfidence = skippedDeltaDueToLowConfidence;
        }

        public TimeSortedSet<PositionDelta> LiveDeltas { get; }
        public bool SkippedDeltaDueToLowConfidence { get; }
    }
}
