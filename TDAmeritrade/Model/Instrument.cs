using Core;
using System;

namespace TDAmeritrade.Model
{
    internal class Instrument
    {
        private string _symbol;

        public Instrument() { }

        public Instrument(string symbol, string assetType)
        {
            bool isOptionSymbol = OptionSymbolUtils.IsOptionSymbol(symbol);
            bool isAssetTypeOption = assetType == Core.Model.Constants.AssetType.OPTION;

            if (isOptionSymbol || isAssetTypeOption)
            {
                throw new ArgumentException(string.Format("Input option date format must be provided via another constructor. Received symbol {0}, assetType {1}",
                    symbol,
                    assetType));
            }

            Symbol = symbol;
            AssetType = assetType;
        }

        public Instrument(string symbol, string assetType, string inputOptionDateFormat)
        {
            bool isOptionSymbol = OptionSymbolUtils.IsOptionSymbol(symbol);
            bool isAssetTypeOption = assetType == Core.Model.Constants.AssetType.OPTION;

            if (!isOptionSymbol || !isAssetTypeOption)
            {
                throw new ArgumentException(string.Format("Cannot create OPTION instrument from received arguments. Received symbol {0}, assetType {1}",
                    symbol,
                    assetType));
            }

            Symbol = OptionSymbolUtils.ConvertDateFormat(symbol, inputOptionDateFormat, Constants.TDOptionDateFormat);
            AssetType = assetType;
        }

        public string AssetType { get; init; }
        public string Cusip { get; init; }

        public string Symbol {
            get => _symbol;
            init
            {
                if (OptionSymbolUtils.IsOptionSymbol(value))
                {
                    OptionSymbolUtils.ValidateDateIsFormatAndInNearFuture(value, Constants.TDOptionDateFormat);
                }
                _symbol = value;
            }
        }

        public string Description { get; init; }
        public string PutCall { get; init; }
        public string UnderlyingSymbol { get; init; }
        public float OptionMultiplier { get; init; }
    }
}
