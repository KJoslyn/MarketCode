using Core.Model.Constants;
using Serilog;
using System;
#nullable enable

namespace Core.Model
{
    public class PositionDelta : HasSymbolInStandardFormat, HasTime
    {
        public PositionDelta(string deltaType, string symbol, float quantity, float price, float percent, OptionQuote? quote = null, DateTime? time = null)
        {
            if (percent > 1 && deltaType == Constants.DeltaType.SELL)
            {
                ArgumentException ex = new ArgumentException("Percent should not be greater than 1!");
                Log.Fatal(ex, "Percent should not be greater than 1! Symbol = {Symbol}, DeltaType={DeltaType}, Percent={Percent}, Quantity={Quantity}, Price={Price}",
                    symbol, deltaType, percent.ToString("0.00"), quantity.ToString("0"), Price.ToString("0.00"));
                throw ex;
            }
            DeltaType = deltaType;
            Symbol = symbol;
            Quantity = quantity;
            Price = price;
            Percent = percent;
            Quote = quote;
            // If we don't have a time for this position delta, we will default to yesterday, to indicate that it may be old.
            Time = time ?? DateTime.Now.AddDays(-1);
        }

        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public PositionDelta() { }
        #pragma warning restore CS8618

        public string DeltaType { get; }
        public float Quantity { get; }
        public float Price { get; }

        // "Percent" depends on DeltaType. Value between 0 and 1.
        // "NEW": N/A
        // "ADD": Percent is amount that position was increased.
        // "SELL": Percent is amount of position that was sold.
        public float Percent { get; }
        public OptionQuote? Quote { get; set; }
        public DateTime Time { get; init; }
        public TimeSpan Age { get => DateTime.Now - Time; }
    }
}
