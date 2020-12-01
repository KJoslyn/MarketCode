using System;

namespace AzureOCR 
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
