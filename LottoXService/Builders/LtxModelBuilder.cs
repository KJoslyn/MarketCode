using AzureOCR;
using Core;
using Core.Model;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LottoXService
{
    public abstract class LtxModelBuilder<T> : ModelBuilder<T> where T : HasSymbolInStandardFormat
    {
        protected string _currentStr = "";
        protected Regex _optionSymbolRegexUnnormalized = new Regex(@"[A-Z]{1,5} \d{6}[CP]\d+([., ]\d)?$");
        protected Regex _priceRegex = new Regex(@"\d+[., ]\d+");
        protected Regex _spaceOrComma = new Regex("[ ,]");

        public LtxModelBuilder(MarketDataClient marketDataClient, PortfolioDatabase database) 
        {
            Symbol = "";
            MarketDataClient = marketDataClient;
            Database = database;
        }

        protected string Symbol { get; set; }
        protected int Quantity { get; set; }
        protected MarketDataClient MarketDataClient { get; init; }
        protected PortfolioDatabase Database { get; init; }

        protected abstract T InstantiateWithSymbolOverride(string symbol, OptionQuote quote);
        protected abstract bool ValidateWithSymbolOverride(string symbol, out OptionQuote quote);

        protected T BuildModelFromSimilarUsedSymbol()
        {
            string searchSymbol = OptionSymbolUtils.GetUnderlyingSymbol(Symbol);
            TimeSortedCollection<UsedUnderlyingSymbol> candidates = new TimeSortedCollection<UsedUnderlyingSymbol>(
                Database.GetUsedUnderlyingSymbols(usedSymbol => 
                    searchSymbol.Length == usedSymbol.Symbol.Length
                    && StringUtils.HammingDistance(searchSymbol, usedSymbol.Symbol) == 1));

            T model = default(T);
            List<UsedUnderlyingSymbol> validCloseSymbols = new List<UsedUnderlyingSymbol>();
            // Look at most recent used symbols first
            foreach (UsedUnderlyingSymbol usedSymbol in candidates.Reverse())
            {
                string newOptionSymbol = OptionSymbolUtils.ChangeUnderlyingSymbol(usedSymbol.Symbol, Symbol);

                bool isNewSymbolValid = ValidateWithSymbolOverride(newOptionSymbol, out OptionQuote quote);
                if (isNewSymbolValid)
                {
                    // If this happens more than once, we will error and log all validCloseSymbols
                    model = InstantiateWithSymbolOverride(newOptionSymbol, quote);
                    validCloseSymbols.Add(usedSymbol);
                }
            }

            if (model != default(T) && validCloseSymbols.Count == 1)
            {
                Log.Information("Found closely related symbol {Symbol} to replace invalid symbol {OldSymbol}", model.Symbol, Symbol);
                return model;
            }
            else if (validCloseSymbols.Count == 0)
            {
                ModelBuilderException ex = new ModelBuilderException("No closely related symbol found in database", this);
                throw ex;
            }
            else // (validCloseSymbols.Count > 1)
            {
                ModelBuilderException ex = new ModelBuilderException("Multiple valid and closely related symbols exist", this);
                Log.Error(ex, "Multiple valid and closely related symbols exist: {@ValidCloseSymbols}", validCloseSymbols);
                throw ex;
            }
        }

        protected void TakeSymbol(Word word)
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

        protected void TakeQuantity(Word word)
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
                //ModelBuilderException ex = new ModelBuilderException("Could not parse quantity.", this);
                //Log.Error(ex, "Could not parse quantity from positions list. Symbol {Symbol}. BuilderType {BuilderType}", Symbol, this.GetType().Name);

                // The builder subclass may wish to override quantity in this method or in a subsequent method using other detected fields.
                Log.Information("*** Could not parse quantity- assuming 1. Symbol {Symbol}. Word {@Word}. BuilderType {BuilderType}", Symbol, word, this.GetType().Name);
                Quantity = quantity;
                FinishBuildLevel();
            }
        }

        protected string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        protected string ReplaceSpaceOrCommaWithPeriod(string input)
        {
            return _spaceOrComma.Replace(input, ".");
        }
    }
}
