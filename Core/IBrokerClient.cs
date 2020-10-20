using Core.Model;
using System.Collections.Generic;

namespace Core
{
    public interface IBrokerClient
    {
        IList<Position> GetPositions();
    }
}
