using Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public class PaperTradeBrokerClient : IBrokerClient
    {
        public PaperTradeBrokerClient(PositionDatabase positionDB)
        {
            PositionDB = positionDB;
        }

        private PositionDatabase PositionDB { get; init; }

        public IList<Position> GetPositions()
        {
            return PositionDB.GetStoredPositions();
        }
    }
}
