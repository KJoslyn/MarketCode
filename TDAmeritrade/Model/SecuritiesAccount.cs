using System.Collections.Generic;

namespace TDAmeritrade.Model
{
    internal class SecuritiesAccount
    {
        public IList<TDPosition> Positions { get; init; }
        public CurrentBalances CurrentBalances { get; init; }
    }
}
