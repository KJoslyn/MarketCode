using Core.Model.Constants;
using System;
using System.Text.RegularExpressions;

namespace Core.Model
{
    public class Instrument
    {
        private static Regex _callRegex = new Regex(@"^[A-Z]{1,5}_\d{6}C\d+");
        private static Regex _putRegex = new Regex(@"^[A-Z]{1,5}_\d{6}P\d+");

        private Instrument(string assetType, string symbol, string putCall, string underlyingSymbol)
        {
            // TODO: What about Cusip???
            AssetType = assetType;
            Symbol = symbol;
            PutCall = putCall;
            UnderlyingSymbol = underlyingSymbol;
        }

        public virtual string AssetType { get; init; }
        public virtual string Cusip { get; init; }
        public virtual string Symbol { get; init; }
        public virtual string Description { get; init; }
        public virtual string PutCall { get; init; }
        public virtual string UnderlyingSymbol { get; init; }
        public virtual float OptionMultiplier { get; init; }

        public static Instrument CreateOptionFromSymbol(string symbol)
        {
            string putCall;
            if (_callRegex.IsMatch(symbol))
            {
                putCall = Constants.PutCall.CALL;
            } 
            else if (_putRegex.IsMatch(symbol))
            {
                putCall = Constants.PutCall.PUT;
            } 
            else
            {
                throw new ArgumentException(String.Format(
                    "Supplied symbol \"{0}\" is not a valid option symbol. Please ensure that an underscore succeeds the underlying symbol.", 
                    symbol));
            }

            string underlyingSymbol = symbol.Substring(0, symbol.IndexOf("_"));

            return new Instrument(Constants.AssetType.OPTION, symbol, putCall, underlyingSymbol);
        }
    }
}
