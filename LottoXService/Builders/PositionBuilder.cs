using AzureOCR;
using Core.Model;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Serilog;
using System;
using System.Text.RegularExpressions;

namespace LottoXService
{
    public class PositionBuilder : LtxModelBuilder<Position>
    {
        private BuildLevel _buildLevel = BuildLevel.SYMBOL;

        // Market value for options always ends in .00 (0 cents.)
        private Regex _marketValRegex = new Regex(@"[$S]\d+[., ]\d+[., ]00");

        public enum BuildLevel
        {
            SYMBOL = 0, QUANTITY, LAST, AVERAGE, PERCENT_SIGN, MARKET_VALUE, DONE
        }

        public override bool Done => _buildLevel == BuildLevel.DONE;
        private double Last { get; set; }
        private double Average { get; set; }

        public override void TakeNextWord(Word word)
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

        protected override Position Build() => new Position(Symbol, Quantity, (float)Average);

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
                if (Math.Abs(value - expectedValue) / expectedValue > 0.05)
                {
                    //ModelBuilderException ex = new ModelBuilderException("Expected value differs from detected live market value", this);
                    //Log.Warning(ex, "Expected value differs from detected live market value. Symbol {Symbol}, Quantity {Quantity}, Last {Last}, MarketValue {MarketValue}",
                    //    Symbol, Quantity, Last, value);
                    //throw ex;
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
