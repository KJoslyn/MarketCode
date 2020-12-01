using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class TimeSortedSet<T> : SortedSet<T> where T : HasTime 
    {
        public TimeSortedSet() : base(new ByTime()) { }
        public TimeSortedSet(IEnumerable<T> set) : base(set, new ByTime()) { }

        class ByTime : IComparer<T>
        {
            public int Compare(T x, T y)
            {
                int result = x.Time.CompareTo(y.Time);

                // Allow duplicate times! Different orders may have the same fill time.
                return result == 0
                    ? 1
                    : result;
            }
        }
    }
}
