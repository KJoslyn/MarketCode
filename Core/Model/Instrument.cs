namespace Core.Model
{
    public class Instrument
    {
        public virtual string AssetType { get; init; }
        public virtual string Cusip { get; init; }
        public virtual string Symbol { get; init; }
        public virtual string Description { get; init; }
        public virtual string PutCall { get; init; }
        public virtual string UnderlyingSymbol { get; init; }
        public virtual float OptionMultiplier { get; init; }
    }
}
