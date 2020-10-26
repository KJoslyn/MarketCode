namespace Core.Model
{
    public class Position
    {
        public Position(float shortQuantity, float averagePrice, float longQuantity, Instrument instrument, float marketValue)
        {
            ShortQuantity = shortQuantity;
            AveragePrice = averagePrice;
            LongQuantity = longQuantity;
            Instrument = instrument;
            MarketValue = marketValue;
        }

        public virtual float ShortQuantity { get; init; }
        public virtual float AveragePrice { get; init; }
        public virtual float LongQuantity { get; init; }
        public virtual Instrument Instrument { get; init; }
        public virtual float MarketValue { get; init; }
    }
}
