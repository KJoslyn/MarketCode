using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LottoXService
{
    public class FilledOrderParsingException : Exception
    {
        public FilledOrderParsingException(string message) : base(message) { }
    }
}
