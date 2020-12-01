using System;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LottoXService.Exceptions
{
    public class ModelBuilderException : Exception
    {
        public ModelBuilderException(string message, IModelBuilder builder) : base(message) 
        {
            Builder = builder;
        }

        public IModelBuilder Builder { get; init; }
    }
}
