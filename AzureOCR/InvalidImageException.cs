using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureOCR
{
    public class InvalidImageException : Exception
    {
        public InvalidImageException(string message) : base(message) { } 
    }
}
