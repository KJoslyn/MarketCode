using System;

namespace Core.Model
{
    public class PositionDelta
    {
        public PositionDelta(string deltaType, string symbol, float quantity, float price, float percent)
        {
            DeltaType = deltaType;
            Symbol = symbol;
            Quantity = quantity;
            Price = price;
            Percent = percent;
            Date = DateTime.Now;
        }

        public string DeltaType { get; }
        public string Symbol { get; }
        public float Quantity { get; }
        // TODO: Add sell price!!! (Get quote)
        public float Price { get; }

        // "Percent" depends on DeltaType. Value between 0 and 1.
        // "NEW": N/A
        // "ADD": Percent is amount that position was increased.
        // "SELL": Percent is amount of position that was sold.
        public float Percent { get; }
        public DateTime Date { get; init; }
    }
}
