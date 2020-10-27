using Core.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core
{
    public interface ILivePortfolioClient
    {
        public IList<Position> GetPositions();

        public Task<bool> Logout();
    }
}
