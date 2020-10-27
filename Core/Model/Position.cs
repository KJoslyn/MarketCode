using Core.Model.Constants;
using System;
using System.Text.RegularExpressions;

namespace Core.Model
{
    public class Position
    {
        private static Regex _callRegex = new Regex(@"^[A-Z]{1,5}[_ ]?\d{6}C\d+");
        private static Regex _putRegex = new Regex(@"^[A-Z]{1,5}[_ ]?\d{6}P\d+");

        public Position(string symbol, float longQuantity, float averagePrice) : this()
        {
            Symbol = symbol;
            LongQuantity = longQuantity;
            AveragePrice = averagePrice;
        }

        public Position() 
        {
            DateUpdated = DateTime.Now;
        }

        public virtual string Symbol { get; init; }
        public virtual float LongQuantity { get; init; }
        public virtual float AveragePrice { get; init; }
        public DateTime DateUpdated { get; init; }

        public string Type
        {
            get
            {
                if (_callRegex.IsMatch(Symbol))
                {
                    return AssetType.CALL; 
                }
                else if (_putRegex.IsMatch(Symbol))
                {
                    return AssetType.PUT;
                }
                else if (Symbol == "MMDA1")
                {
                    return AssetType.CASH;
                }
                return AssetType.EQUITY;
            }
        }
    }
}
