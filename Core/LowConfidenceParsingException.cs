using System;

namespace Core
{
    public class LowConfidenceParsingException : Exception
    {
        public LowConfidenceParsingException(string message) : base(message) { }
    }
}
