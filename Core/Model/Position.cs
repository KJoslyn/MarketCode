namespace Core.Model
{
    public class Position
    {
        public virtual float ShortQuantity { get; init; }
        public virtual float AveragePrice { get; init; }
        public virtual float LongQuantity { get; init; }
        public virtual Instrument Instrument { get; init; }
        public virtual float MarketValue { get; init; }
    }
}
