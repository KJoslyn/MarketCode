using System;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LottoXService.Exceptions
{
    public class PositionBuilderException : Exception
    {
        public PositionBuilderException(string message, PositionBuilder builder) : base(message) 
        {
            Builder = builder;
        }

        public PositionBuilder Builder { get; init; }
    }
}
