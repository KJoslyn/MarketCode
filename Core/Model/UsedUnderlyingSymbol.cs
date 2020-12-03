using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Core.Model
{
    public class UsedUnderlyingSymbol
    {
        private static Regex _underlyingSymbolRegex = new Regex(@"^[A-Z]{1,5}$");

        public UsedUnderlyingSymbol(string symbol, DateTime lastUsedTime)
        {
            string underlyingSymbol = _underlyingSymbolRegex.IsMatch(symbol)
                ? symbol
                : OptionSymbolUtils.GetEquitySymbol(symbol);

            Symbol = underlyingSymbol;
            LastUsedTime = lastUsedTime;
        }

        public string Symbol { get; init; }
        public DateTime LastUsedTime { get; set; }
        public string Id { get => Symbol; }
    }
}
