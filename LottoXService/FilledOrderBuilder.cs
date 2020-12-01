using Core.Model;
using Core.Model.Constants;
using LottoXService.Exceptions;
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
    public class FilledOrderBuilder : ModelBuilder<FilledOrder>
    {
        private BuildLevel _buildLevel = BuildLevel.SYMBOL;
        private int _thisLevelCounter = 0;
        private Regex _dateTimeRegexUnnormalized = new Regex(@"(\d{2}/\d{2}/\d{2}) (\d{2}[: .,]\d{2}[: .,]\d{2}) (AM|PM)");

        public FilledOrderBuilder(IEnumerable<string> currentPositionSymbols) : base()
        {
            Instruction = "";
            OrderType = "";
            CurrentPositionSymbols = currentPositionSymbols;
        }

        public enum BuildLevel
        {
            SYMBOL = 0, FILLED_PRICE, INSTRUCTION, LIMIT_OR_MARKET, QUANTITY, SPREAD, ACCT_ALIAS, TIME, DONE
        }

        public override bool Done => _buildLevel == BuildLevel.DONE;
        private double FilledPrice { get; set; }
        private string Instruction { get; set; }
        private string OrderType { get; set; }
        private double Limit { get; set; }
        private DateTime Time { get; set; }
        private IEnumerable<string> CurrentPositionSymbols { get; init; }

        public override void TakeNextWord(Word word)
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

        protected override FilledOrder Build() => new FilledOrder(Symbol, (float)FilledPrice, Instruction, OrderType, (float)Limit, Quantity, Time);

        protected override void FinishBuildLevel()
        {
            _currentStr = "";
            _buildLevel++;
            _thisLevelCounter = 0;
        }

        protected override void Reset()
        {
            Symbol = "";
            Quantity = -1;
            FilledPrice = -1;
            Instruction = "";
            OrderType = "";
            Limit = -1;
            Time = new DateTime();
            _currentStr = "";
            _buildLevel = BuildLevel.SYMBOL;
            _thisLevelCounter = 0;
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
            if (HammingDistance(fuzzyInstructionStr, "Buy to Open") <= 2)
            {
                return InstructionType.BUY_TO_OPEN;
            }
            else if (HammingDistance(fuzzyInstructionStr, "Sell to Close") <= 2)
            {
                return InstructionType.SELL_TO_CLOSE;
            }
            else
            {
                return null;
            }
        }

        private int HammingDistance(string first, string second)
        {
            if (first.Length > second.Length)
            {
                second.Concat(new string('!', first.Length - second.Length));
            }
            else if (second.Length > first.Length)
            {
                first.Concat(new string('!', second.Length - first.Length));
            }
            return first.Zip(second, (a, b) => a != b).Count(diff => diff);
        }

        private void TakeLimitOrMarket(Word word)
        {
            _currentStr += word.Text;
            if (HammingDistance(_currentStr, "Market") <= 1)
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
            if (HammingDistance(word.Text, "Vertical") <= 2 ||
                HammingDistance(word.Text, "Butterfly") <= 2)
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
            else if (word.Text == "WMM" && CurrentPositionSymbols.Contains(Symbol))
            {
                Log.Warning("WMM order encountered for existing LottoX position. Treating as LTX order. Symbol {Symbol}, Builder {@Builder}", Symbol, this);
                FinishBuildLevel();
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
