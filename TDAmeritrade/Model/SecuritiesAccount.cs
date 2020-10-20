using Core.Model;
using System.Collections.Generic;

namespace TDAmeritrade.Model
{
    internal class SecuritiesAccount
    {
        public IList<Position> Positions { get; init; }
    }
}
