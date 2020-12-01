using Core.Model;
using LottoXService.Exceptions;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Serilog;
using System;
using System.Text.RegularExpressions;
#nullable enable

namespace LottoXService
{
    public class PositionBuilder
    {
        public enum BuildLevel
        {
            SYMBOL = 0, QUANTITY, LAST, AVERAGE, PERCENT_SIGN, MARKET_VALUE, DONE
        }

        private BuildLevel _buildLevel = BuildLevel.SYMBOL;
        private string _currentStr = "";

        private Regex _optionSymbolRegexUnnormalized = new Regex(@"[A-Z]{1,5} \d{6}[CP]\d+([., ]\d)?$");
        private Regex _priceRegex = new Regex(@"\d+[., ]\d+");
        // Market value for options always ends in .00 (0 cents.)
        private Regex _marketValRegex = new Regex(@"[$S]\d+[., ]\d+[., ]00");
        private Regex _spaceOrComma = new Regex("[ ,]");

        public bool Done { get => _buildLevel == BuildLevel.DONE; }
        private string? Symbol { get; set; }
        private int? Quantity { get; set; }
        private double? Last { get; set; }
        private double? Average { get; set; }

        public Position BuildAndReset()
        {
            if (!Done)
            {
                throw new PositionBuilderException("Build() called too early!", this);
            }
            Position pos = new Position(Symbol, (float)Quantity, (float)Average);
            Reset();
            return pos;
        }

        public void TakeNextWord(Word word)
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
                    TakeMarketValue(word);
                    break;
                case BuildLevel.DONE:
                    Log.Information("Builder is already done and ready to build a Position!");
                    break;
            }
        }
        
        private void Reset()
        {
            Symbol = null;
            Quantity = null;
            Last = null;
            Average = null;
            _currentStr = "";
            _buildLevel = BuildLevel.SYMBOL;
        }

        private void TakeSymbol(Word word)
        {
            _currentStr += word.Text;
            Match match = _optionSymbolRegexUnnormalized.Match(_currentStr);
            if (match.Success)
            {
                string symbol = ReplaceFirst(match.Value, " ", "_");
                symbol = ReplaceSpaceOrCommaWithPeriod(symbol);

                Symbol = symbol;
                FinishBuildLevel();
            }
            else
            {
                _currentStr += " ";
            }
        }

        private void TakeQuantity(Word word)
        {
            string text = word.Text;
            bool isInt = int.TryParse(text, out int quantity);
            if (isInt)
            {
                int? width = (int)(word.BoundingBox[2] - word.BoundingBox[0]);
                // Single-digit quantities should not occupy more than 14 pixels of width on the screen.
                if (quantity > 9 && width < 15)
                {
                    if (text.StartsWith("1"))
                    {
                        string newQuantity = text[1..];
                        Log.Information("Quantity width {Width} too narrow for detected value {RawQuantity}. Assumed quantity {Quantity}. Symbol {Symbol}",
                            width, text, newQuantity, Symbol);
                        Quantity = int.Parse(newQuantity);
                    }
                    else
                    {
                        Log.Error("Quantity width too narrow for detected value {RawQuantity}. Symbol {Symbol}", text, Symbol);
                        // This is bad, but assume it is correct for now
                        Quantity = quantity;
                    }
                    FinishBuildLevel();
                }
                else if (quantity <= 0)
                {
                    Log.Warning("Quantity <= 0! Quantity {Quantity}. Symbol {Symbol}", quantity, Symbol);
                    Reset();
                }
                else
                {
                    Quantity = quantity;
                    FinishBuildLevel();
                }
            }
            else
            {
                Log.Information("Could not parse quantity from positions list. Assuming 1. Symbol {Symbol}", Symbol);
                Quantity = 1;
                FinishBuildLevel();

                // We did not use this word, so forward it to the next build function.
                TakeNextWord(word);
            }
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

        private void TakeMarketValue(Word word)
        {
            _currentStr += word.Text;
            if (_marketValRegex.IsMatch(_currentStr))
            {
                // Take only digits
                string valueStr = string.Join("", Regex.Split(_currentStr, @"[^\d]"));
                // Chop off cents at end
                double value = double.Parse(valueStr.Substring(0, valueStr.Length - 2));

                double expectedValue = (double)Quantity * (double)Last * 100;
                if (Math.Abs(value - expectedValue) / expectedValue > 0.05)
                {
                    throw new PositionBuilderException("Expected value differs from detected live market value", this);
                }
                FinishBuildLevel();
            }
            else
            {
                _currentStr += " ";
            }
        }

        private void FinishBuildLevel()
        {
            _currentStr = "";
            _buildLevel++;
        }

        private string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        private string ReplaceSpaceOrCommaWithPeriod(string input)
        {
            return _spaceOrComma.Replace(input, ".");
        }
    }
}
