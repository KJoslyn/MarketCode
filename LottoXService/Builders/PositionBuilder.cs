using AzureOCR;
using Core;
using Core.Model;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Serilog;
using System;
using System.Text.RegularExpressions;
#nullable enable

namespace LottoXService
{
    public class PositionBuilder : LtxModelBuilder<Position>
    {
        private BuildLevel _buildLevel = BuildLevel.SYMBOL;

        // Market value for options always ends in .00 (0 cents.)
        private Regex _marketValRegex = new Regex(@"[$S]\d+[., ]\d+[., ]00");

        public PositionBuilder(MarketDataClient client, PortfolioDatabase database) : base(client, database) { }

        public enum BuildLevel
        {
            SYMBOL = 0, QUANTITY, LAST, AVERAGE, PERCENT_SIGN, MARKET_VALUE, DONE
        }

        protected override bool Done => _buildLevel == BuildLevel.DONE;
        private double Last { get; set; }
        private double Average { get; set; }

        protected override void TakeNextWord(Word word)
        {
            word.Text = word.Text.Replace("|", "");

            switch (_buildLevel)
            {
                case BuildLevel.SYMBOL:
                    TakeSymbol(word);
                    break;
                case BuildLevel.QUANTITY:
                    TakeQuantity(word);
                    break;
                case BuildLevel.LAST:
                    TakePrice(word, SetLast);
                    break;
                case BuildLevel.AVERAGE:
                    TakePrice(word, SetAverage);
                    break;
                case BuildLevel.PERCENT_SIGN:
                    TakePercentSign(word);
                    break;
                case BuildLevel.MARKET_VALUE:
                    TakeMarketValueAndFixQuantityIfNecessary(word);
                    break;
                case BuildLevel.DONE:
                    Log.Information("Builder is already done and ready to build a Position!");
                    break;
            }
        }

        protected override Position Build()
        {
            Position? existingPosition = Database.GetPosition(Symbol);
            if (existingPosition?.LongQuantity == Quantity)
            {
                return existingPosition;
            }
            else
            {
                bool isValid = MarketDataClient.ValidateWithinTodaysRangeAndGetQuote(Symbol, (float)Last, out _);
                if (isValid)
                {
                    return new Position(Symbol, Quantity, (float)Average);
                }
                else
                {
                    Log.Information("Found invalid symbol/price in parsed position: Symbol {Symbol}. Builder {@Builder}", Symbol, this);
                    return BuildModelFromSimilarUsedSymbol();
                }
            }
        }

        protected override Position InstantiateWithSymbolOverride(string overrideSymbol, OptionQuote quote)
        {
            return new Position(overrideSymbol, Quantity, (float)Average);
        }

        protected override bool ValidateWithSymbolOverride(string overrideSymbol, out OptionQuote? quote)
        {
            return MarketDataClient.ValidateWithinTodaysRangeAndGetQuote(overrideSymbol, (float)Last, out quote);
        }

        protected override void FinishBuildLevel()
        {
            _currentStr = "";
            _buildLevel++;
        }
        
        protected override void Reset()
        {
            _currentStr = "";
            _buildLevel = BuildLevel.SYMBOL;

            Symbol = "";
            Quantity = 0;
            Last = 0;
            Average = 0;
        }

        private delegate void SetPriceDelegate(double price);
        private void SetLast(double price) { Last = price; }
        private void SetAverage(double price) { Average = price; }

        private void TakePrice(Word word, SetPriceDelegate del)
        {
            _currentStr += word.Text;
            if (_priceRegex.IsMatch(_currentStr))
            {
                del(double.Parse(ReplaceSpaceOrCommaWithPeriod(_currentStr)));
                FinishBuildLevel();
            }
            else
            {
                _currentStr += ".";
            }
        }

        private void TakePercentSign(Word word)
        {
            if (word.Text.Contains('%'))
            {
                FinishBuildLevel();
            }
        }

        private void TakeMarketValueAndFixQuantityIfNecessary(Word word)
        {
            _currentStr += word.Text;
            if (_marketValRegex.IsMatch(_currentStr))
            {
                // Take only digits
                string valueStr = string.Join("", Regex.Split(_currentStr, @"[^\d]"));
                // Chop off cents at end
                double value = double.Parse(valueStr.Substring(0, valueStr.Length - 2));

                double expectedValue = Quantity * Last * 100;
                if (expectedValue == 0 ||
                    Math.Abs(value - expectedValue) / expectedValue > 0.05)
                {
                    int correctedQuantity = (int)Math.Round(value / (Last * 100));
                    Log.Information("*** Expected value differs from detected live market value. Setting quantity to {NewQuantity}. Symbol {Symbol}, Quantity {Quantity}, Last {Last}, MarketValue {MarketValue}",
                        correctedQuantity, Symbol, Quantity, Last, value);
                    Quantity = correctedQuantity;
                }
                FinishBuildLevel();
            }
            else
            {
                _currentStr += " ";
            }
        }
    }
}
