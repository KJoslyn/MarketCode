using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public class InvalidPortfolioStateException : Exception
    {
        public InvalidPortfolioStateException(string message) : base(message) { }
    }
}
