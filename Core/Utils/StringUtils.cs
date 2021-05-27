using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public static class StringUtils
    {
        public static int HammingDistance(string first, string second)
        {
            if (first.Length > second.Length)
            {
                second.Concat(new string('!', first.Length - second.Length));
            }
            else if (second.Length > first.Length)
            {
                first.Concat(new string('!', second.Length - first.Length));
            }
            return first.Zip(second, (a, b) => a != b).Count(diff => diff);
        }
    }
}
