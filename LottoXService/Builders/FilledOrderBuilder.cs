using AzureOCR;
using Core;
using Core.Model;
using Core.Model.Constants;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
#nullable enable

namespace LottoXService
{
    public class FilledOrderBuilder : LtxModelBuilder<FilledOrder>
    {
        private BuildLevel _buildLevel = BuildLevel.SYMBOL;
        private int _thisLevelCounter = 0;
        private Regex _dateTimeRegexUnnormalized = new Regex(@"(\d{2}/\d{2}/\d{2}) (\d{2}[: .,]\d{2}[: .,]\d{2}) (AM|PM)");

        public FilledOrderBuilder(MarketDataClient client, PortfolioDatabase database) : base(client, database)
        {
            Instruction = "";
            OrderType = "";
        }

        public enum BuildLevel
        {
            SYMBOL = 0, FILLED_PRICE, INSTRUCTION, LIMIT_OR_MARKET, QUANTITY, SPREAD, ACCT_ALIAS, TIME, DONE
        }

        protected override bool Done => _buildLevel == BuildLevel.DONE;
        private double FilledPrice { get; set; }
        private string Instruction { get; set; }
        private string OrderType { get; set; }
        private double Limit { get; set; }
        private DateTime Time { get; set; }

        protected override void TakeNextWord(Word word)
        {
            _thisLevelCounter++;
            word.Text = word.Text.Replace("|", "");

            switch (_buildLevel)
            {
                case BuildLevel.SYMBOL:
                    TakeSymbol(word);
                    break;
                case BuildLevel.FILLED_PRICE:
                    TakeFilledPrice(word);
                    break;
                case BuildLevel.INSTRUCTION:
                    TakeInstruction(word);
                    break;
                case BuildLevel.LIMIT_OR_MARKET:
                    TakeLimitOrMarket(word);
                    break;
                case BuildLevel.QUANTITY:
                    TakeQuantity(word);
                    break;
                case BuildLevel.SPREAD:
                    TakeSpread(word);
                    break;
                case BuildLevel.ACCT_ALIAS:
                    TakeAcctAlias(word);
                    break;
                case BuildLevel.TIME:
                    TakeTime(word);
                    break;
                case BuildLevel.DONE:
                    Log.Information("Builder is already done and ready to build a FilledOrder!");
                    break;
            }
        }

        protected override FilledOrder Build()
        {
            UnvalidatedFilledOrder unvalidatedOrder = new UnvalidatedFilledOrder(Symbol, (float)FilledPrice, Instruction, OrderType, (float)Limit, Quantity, Time);
            FilledOrder? existingOrder = Database.GetTodaysFilledOrders().FirstOrDefault(order => unvalidatedOrder.StrictEquals(order));

            if (existingOrder != null)
            {
                return existingOrder;
            }
            else
            {
                bool isValid = MarketDataClient.ValidateOrderAndGetQuote(unvalidatedOrder, out OptionQuote? quote);
                if (isValid)
                {
                    return new FilledOrder(unvalidatedOrder, quote);
                }
                else
                {
                    Log.Information("Found invalid symbol/price in parsed order: Symbol {Symbol}. Builder {@Builder}", this);
                    return CreateFilledOrderFromCloseUsedSymbol();
                }
            }
        }

        private FilledOrder CreateFilledOrderFromCloseUsedSymbol()
        {
            string searchSymbol = OptionSymbolUtils.GetUnderlyingSymbol(Symbol);
            TimeSortedCollection<UsedUnderlyingSymbol> candidates = new TimeSortedCollection<UsedUnderlyingSymbol>(
                Database.GetUsedUnderlyingSymbols(usedSymbol => StringUtils.HammingDistance(searchSymbol, usedSymbol.Symbol) == 1));

            FilledOrder? filledOrder = null;
            List<UsedUnderlyingSymbol> validCloseSymbols = new List<UsedUnderlyingSymbol>();
            // Look at most recent used symbols first
            foreach (UsedUnderlyingSymbol usedSymbol in candidates.Reverse())
            {
                string newOptionSymbol = OptionSymbolUtils.ChangeUnderlyingSymbol(usedSymbol.Symbol, Symbol);
                UnvalidatedFilledOrder unvalidatedOrder = new UnvalidatedFilledOrder(newOptionSymbol, (float)FilledPrice, Instruction, OrderType, (float)Limit, Quantity, Time);

                bool isNewSymbolValid = MarketDataClient.ValidateOrderAndGetQuote(unvalidatedOrder, out OptionQuote? quote);
                if (isNewSymbolValid)
                {
                    // If this happens more than once, we will error and log all validCloseSymbols
                    filledOrder = new FilledOrder(unvalidatedOrder, quote);
                    validCloseSymbols.Add(usedSymbol);
                }
            }

            if (filledOrder != null && validCloseSymbols.Count == 1)
            {
                Log.Information("Found closely related symbol {Symbol} to replace invalid symbol {OldSymbol}", filledOrder.Symbol, Symbol);
                return filledOrder;
            }
            else if (validCloseSymbols.Count == 0)
            {
                ModelBuilderException ex = new ModelBuilderException("No closely related symbol found in database", this);
                Log.Error(ex, "No closely related symbol found in database");
                throw ex;
            }
            else // (validCloseSymbols.Count > 1)
            {
                ModelBuilderException ex = new ModelBuilderException("Multiple valid and closely related symbols exist", this);
                Log.Error(ex, "Multiple valid and closely related symbols exist: {@ValidCloseSymbols}", validCloseSymbols);
                throw ex;
            }
        }

        protected override void FinishBuildLevel()
        {
            _currentStr = "";
            _buildLevel++;
            _thisLevelCounter = 0;
        }

        protected override void Reset()
        {
            _currentStr = "";
            _buildLevel = BuildLevel.SYMBOL;
            _thisLevelCounter = 0;

            Symbol = "";
            Quantity = 0;
            FilledPrice = 0;
            Instruction = "";
            OrderType = "";
            Limit = 0;
            Time = new DateTime();
        }

        private void TakeFilledPrice(Word word)
        {
            _currentStr += word.Text;
            if (_priceRegex.IsMatch(_currentStr))
            {
                FilledPrice = double.Parse(ReplaceSpaceOrCommaWithPeriod(_currentStr));
                FinishBuildLevel();
            }
            else if (_thisLevelCounter == 1)
            {
                _currentStr += ".";
            }
            else
            {
                // This order has not been filled. Do not throw an error, but reset.
                Reset();
            }
        }

        private void TakeInstruction(Word word)
        {
            _currentStr += word.Text;
            if (_thisLevelCounter == 3)
            {
                string? instruction = GetInstruction(_currentStr);
                if (instruction != null)
                {
                    Instruction = instruction;
                    FinishBuildLevel();
                }
                else
                {
                    throw new ModelBuilderException("Could not parse instruction from order", this);
                }
            }
            else
            {
                _currentStr += " ";
            }
        }

        private string? GetInstruction(string fuzzyInstructionStr)
        {
            if (StringUtils.HammingDistance(fuzzyInstructionStr, "Buy to Open") <= 2)
            {
                return InstructionType.BUY_TO_OPEN;
            }
            else if (StringUtils.HammingDistance(fuzzyInstructionStr, "Sell to Close") <= 2)
            {
                return InstructionType.SELL_TO_CLOSE;
            }
            else
            {
                return null;
            }
        }

        private void TakeLimitOrMarket(Word word)
        {
            _currentStr += word.Text;
            if (StringUtils.HammingDistance(_currentStr, "Market") <= 1)
            {
                OrderType = Core.Model.Constants.OrderType.MARKET;
                FinishBuildLevel();
            }
            else if (_priceRegex.IsMatch(_currentStr))
            {
                OrderType = Core.Model.Constants.OrderType.LIMIT;
                Limit = double.Parse(ReplaceSpaceOrCommaWithPeriod(_currentStr));
                FinishBuildLevel();
            }
            else if (_thisLevelCounter == 1)
            {
                _currentStr += ".";
            }
            else
            {
                throw new ModelBuilderException("Could not parse limit/market from order", this);
            }
        }

        private void TakeSpread(Word word)
        {
            if (StringUtils.HammingDistance(word.Text, "Vertical") <= 2 ||
                StringUtils.HammingDistance(word.Text, "Butterfly") <= 2)
            {
                Reset();
            }
            else
            {
                FinishBuildLevel();
                // We did not use this word, so forward it to the next build function.
                TakeNextWord(word);
            }
        }

        private void TakeAcctAlias(Word word)
        {
            if (word.Text == "LTX")
            {
                FinishBuildLevel();
            }
            else if (word.Text == "WMM")
            {
                IEnumerable<string> currentPositionSymbols = Database.GetStoredPositions().Select(pos => pos.Symbol);
                if (currentPositionSymbols.Contains(Symbol))
                {
                    Log.Warning("WMM order encountered for existing LottoX position. Treating as LTX order. Symbol {Symbol}, Builder {@Builder}", Symbol, this);
                    FinishBuildLevel();
                }
            }
            else
            {
                Reset();
            }
        }

        private void TakeTime(Word word)
        {
            _currentStr += word.Text;
            if (word.Text == "AM" || word.Text == "PM")
            {
                string? timeStr = TryNormalizeDateTime(_currentStr);
                if (timeStr != null)
                {
                    Time = DateTime.Parse(timeStr);
                    FinishBuildLevel();
                }
                else
                {
                    throw new ModelBuilderException("Could not parse time from order", this);
                }
            }
            else if (_thisLevelCounter < 5)
            {
                _currentStr += " ";
                _thisLevelCounter++;
            }
            else
            {
                throw new ModelBuilderException("Could not parse time from order", this);
            }
        }

        private string? TryNormalizeDateTime(string orig)
        {
            Match dateTimeMatch = _dateTimeRegexUnnormalized.Match(orig);

            if (! dateTimeMatch.Success) return null;

            Regex fixTimeRegex = new Regex("[ .,]");
            string[] groups = dateTimeMatch.Groups.Values.Select(group => group.Value).ToArray();
            return string.Format("{0} {1} {2}",
                groups[1],
                fixTimeRegex.Replace(groups[2], ":"),
                groups[3]);
        }
    }
}
