using Serilog;
using System;

namespace Core.Model
{
    public class PositionDelta : HasSymbolInStandardFormat, HasTime
    {
        public PositionDelta(string deltaType, string symbol, float quantity, float price, float percent, DateTime? time = null)
        {
            if (percent > 1)
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
            Time = time ?? DateTime.Now;
        }

        public string DeltaType { get; }
        public float Quantity { get; }
        public float Price { get; }

        // "Percent" depends on DeltaType. Value between 0 and 1.
        // "NEW": N/A
        // "ADD": Percent is amount that position was increased.
        // "SELL": Percent is amount of position that was sold.
        public float Percent { get; }
        public DateTime Time { get; init; }
        public TimeSpan Age { get => DateTime.Now - Time; }
    }
}
