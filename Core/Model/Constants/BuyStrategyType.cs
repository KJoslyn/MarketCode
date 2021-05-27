using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model.Constants
{
    public class BuyStrategyType
    {
        public const string MARKET = nameof(MARKET);
        public const string DELTA_LIMIT = nameof(DELTA_LIMIT);
        public const string THRESHOLD_LIMIT = nameof(THRESHOLD_LIMIT);
    }
}
