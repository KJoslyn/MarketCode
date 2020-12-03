using Core.Model.Constants;
using System;
using System.Text.RegularExpressions;

namespace Core.Model
{
    public class Position : HasSymbolInStandardFormat
    {
        public Position(string symbol, float longQuantity, float averagePrice) : this()
        {
            Symbol = symbol;
            LongQuantity = longQuantity;
            AveragePrice = averagePrice;
            DateUpdated = DateTime.Now;
        }

        public Position()
        {
            DateUpdated = DateTime.Now;
        }

        public virtual float LongQuantity { get; init; }
        public virtual float AveragePrice { get; init; }
        public DateTime DateUpdated { get; set; }

        public string Type
        {
            get
            {
                if (OptionSymbolUtils.IsCall(Symbol))
                {
                    return AssetType.CALL;
                }
                else if (OptionSymbolUtils.IsPut(Symbol))
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
