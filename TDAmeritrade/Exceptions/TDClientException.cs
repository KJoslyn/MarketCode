using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDAmeritrade
{
    internal class TDClientException : Exception
    {
        public TDClientException(string message): base(message) { }
    }
}
