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
                bool isValid = MarketDataClient.ValidateWithinTodaysRangeAndGetQuote(unvalidatedOrder, out OptionQuote? quote);
                if (isValid)
                {
                    return new FilledOrder(unvalidatedOrder, quote);
                }
                else
                {
                    Log.Information("Found invalid symbol/price in parsed order: Symbol {Symbol}. Builder {@Builder}", Symbol, this);
                    return BuildModelFromSimilarUsedSymbol();
                }
            }
        }

        protected override FilledOrder InstantiateWithSymbolOverride(string overrideSymbol, OptionQuote quote)
        {
            return new FilledOrder(overrideSymbol, (float)FilledPrice, Instruction, OrderType, (float)Limit, Quantity, Time, quote);
        }

        protected override bool ValidateWithSymbolOverride(string overrideSymbol, out OptionQuote? quote)
        {
            return MarketDataClient.ValidateWithinTodaysRangeAndGetQuote(overrideSymbol, (float)FilledPrice, out quote);
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
            Match match = _priceRegex.Match(_currentStr);
            if (match.Success)
            {
                FilledPrice = double.Parse(ReplaceSpaceOrCommaWithPeriod(match.Groups[0].Value));
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
                if (instruction != null &&
                    (instruction == InstructionType.BUY_TO_OPEN || instruction == InstructionType.SELL_TO_CLOSE))
                {
                    Instruction = instruction;
                    FinishBuildLevel();
                }
                else if (instruction != null &&
                    (instruction == InstructionType.SELL_TO_OPEN || instruction == InstructionType.BUY_TO_CLOSE))
                {
                    Reset();
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
            else if (StringUtils.HammingDistance(fuzzyInstructionStr, "Sell to Open") <= 2)
            {
                return InstructionType.SELL_TO_OPEN;
            }
            else if (StringUtils.HammingDistance(fuzzyInstructionStr, "Buy to Close") <= 2)
            {
                return InstructionType.BUY_TO_CLOSE;
            }
            else
            {
                return null;
            }
        }

        private void TakeLimitOrMarket(Word word)
        {
            _currentStr += word.Text;

            Match priceMatch = _priceRegex.Match(_currentStr);

            if (StringUtils.HammingDistance(_currentStr, "Market") <= 1)
            {
                OrderType = Core.Model.Constants.OrderType.MARKET;
                FinishBuildLevel();
            }
            else if (priceMatch.Success)
            {
                OrderType = Core.Model.Constants.OrderType.LIMIT;
                Limit = double.Parse(ReplaceSpaceOrCommaWithPeriod(priceMatch.Groups[0].Value));
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
