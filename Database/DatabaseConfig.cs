using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database
{
    public class DatabaseConfig
    {
        public string LottoxDatabasePath { get; init; }
        public string PaperTradeDatabasePath { get; init; }
        public bool UsePaperTrade { get; init; }
    }
}
