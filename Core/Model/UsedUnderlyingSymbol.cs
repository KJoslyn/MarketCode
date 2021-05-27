using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Core.Model
{
    public class UsedUnderlyingSymbol : HasTime
    {
        private static Regex _underlyingSymbolRegex = new Regex(@"^[A-Z]{1,5}$");

        public UsedUnderlyingSymbol(string symbol, DateTime lastUsedTime)
        {
            string underlyingSymbol = _underlyingSymbolRegex.IsMatch(symbol)
                ? symbol
                : OptionSymbolUtils.GetUnderlyingSymbol(symbol);

            Symbol = underlyingSymbol;
            Time = lastUsedTime;
        }

        public UsedUnderlyingSymbol() { }

        public string Symbol { get; init; }
        public DateTime Time { get; init; }
        public string Id { get => Symbol; }
    }
}
