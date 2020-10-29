using Core.Model;
using System.Collections.Generic;

namespace Core
{
    public interface IBrokerClient
    {
        public IList<Position> GetPositions();
    }
}
