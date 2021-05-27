using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public class PortfolioDatabaseException : Exception
    {
        public PortfolioDatabaseException(string message) : base(message) { }
    }
}
