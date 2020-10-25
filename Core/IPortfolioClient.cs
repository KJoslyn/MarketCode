using Core.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core
{
    public interface IPortfolioClient
    {
        public IList<Position> GetPositions();

        public Task<bool> Logout();
    }
}
