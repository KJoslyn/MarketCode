using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class LiveDeltasResult
    {
        public LiveDeltasResult(TimeSortedSet<PositionDelta> liveDeltas, Dictionary<string, OptionQuote> quotes, bool skippedDeltaDueToLowConfidence)
        {
            LiveDeltas = liveDeltas;
            Quotes = quotes;
            SkippedDeltaDueToLowConfidence = skippedDeltaDueToLowConfidence;
        }

        public TimeSortedSet<PositionDelta> LiveDeltas { get; }
        public Dictionary<string, OptionQuote> Quotes { get; }
        public bool SkippedDeltaDueToLowConfidence { get; }
    }
}
