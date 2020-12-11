using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model.Constants
{
    public static class OrderStatus
    {
        public const string QUEUED = nameof(QUEUED);
        public const string WORKING = nameof(WORKING);
        public const string ACCEPTED = nameof(ACCEPTED);
        public const string FILLED = nameof(FILLED);
        public const string REJECTED = nameof(REJECTED);
    }
}
