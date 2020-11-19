using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public interface HasTime
    {
        public DateTime Time { get; init; }
    }
}
