using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#nullable enable

namespace Core
{
    public class InvalidPortfolioStateException : Exception
    {
        public InvalidPortfolioStateException(string message) : base(message) { }

        public InvalidPortfolioStateException(string message, Exception subException) : base(message) 
        {
            SubException = subException;
        }

        public Exception? SubException { get; }
    }
}
