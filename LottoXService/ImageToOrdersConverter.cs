using AzureOCR;
using Core;
using Core.Model;
using Core.Model.Constants;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
#nullable enable

namespace LottoXService
{
    internal class ImageToOrdersConverter : OCRClient
    {
        private Regex _spaceOrComma = new Regex("[ ,]");
        private Regex _optionSymbolRegexUnnormalized = new Regex(@"^[A-Z]{1,5} \d{6}[CP]\d+([., ]\d)?");
        private Regex _dateTimeRegexUnnormalized = new Regex(@"(\d{2}/\d{2}/\d{2}) (\d{2}[: .,]\d{2}[: .,]\d{2}) (AM|PM)");

        public ImageToOrdersConverter(OCRConfig config) : base(config) { }

        // TODO: Remove first part of tuple
        public async Task<LiveOrdersResult> GetFilledOrdersFromImage(
            string filePath,
            IList<string> currentPositionSymbols,
            string writeToJsonPath = null)
        {
            IList<Line> lines = await ExtractLinesFromImage(filePath, writeToJsonPath);

            // We will use only the text part of the line
            List<string> lineTexts = lines.Select((line, index) => line.Text).ToList();

            bool valid = ValidateOrderColumnHeaders(lineTexts);
            if (!valid)
            {
                Exception ex = new InvalidPortfolioStateException("Invalid portfolio state");
                Log.Warning(ex, "Invalid portfolio state. Extracted text: {@Text}", lineTexts);
                throw ex;
            }

            IEnumerable<string> orderStrings;
            // TODO: Don't hardcode .93
            bool skippedOrderDueToLowConfidence = GetLottoxOrderStrings(lines, currentPositionSymbols, 0.93, out orderStrings);

            return new LiveOrdersResult(CreateFilledOrders(orderStrings), skippedOrderDueToLowConfidence);
        }

        private string GetFirstNormalizedDateTime(List<string> lineTexts)
        {
            foreach (string text in lineTexts)
            {
                string? normalizedDateTime = TryNormalizeDateTime(text);
                if (normalizedDateTime != null)
                {
                    return normalizedDateTime;
                }
            }
            return "";
        }

        private bool ValidateOrderColumnHeaders(List<string> lineTexts)
        {
            int symbolColumnIdx = lineTexts.FindIndex(text => text == "Symbol");
            int filledCanceledColumnIndex = lineTexts.FindIndex(text => text == "Filled/Canceled");
            if (symbolColumnIdx == -1 || filledCanceledColumnIndex == -1)
            {
                return false;
            }
            List<string> subList = lineTexts.GetRange(symbolColumnIdx, filledCanceledColumnIndex - symbolColumnIdx + 1);
            string joined = string.Join(" ", subList);
            Regex regex = new Regex(@"^Symbol(\||\s)+Fill?ed.*?Type?(\||\s)+Limit?(\||\s)+Quantity(\||\s)+Spread(\||\s)+Acct[.]? Alias(\||\s)+Fill?ed/Canceled");
            return regex.IsMatch(joined);
        }

        /// <summary>
        /// Returns a list of string representations of LottoX orders. Common mistakes in image recognition for option symbols and DateTimes
        /// are corrected. Mistakes in prices are NOT corrected. This is commonly a period that has been interpreted as a space or comma.
        /// Due to the potential irregularity in where spaces occur, we simply concatenate all parts of a particular order here and leave it
        /// to another function to split out the parts of the order and determine whether the order's format is valid.
        /// </summary>
        private bool GetLottoxOrderStrings(IEnumerable<Line> lines, IEnumerable<string> currentPositionSymbols, double intConfidenceThreshold, out IEnumerable<string> orderStrings)
        {
            bool skippedOrderDueToLowConfidence = false; 
            orderStrings = new List<string>();

            string orderStr = "";
            string symbol = "";
            bool lowConfidenceSkip = false;
            bool isLTXOrApprovedWMM = false;
            bool isComplexOrder = false;
            bool wasLineOverriden = false;

            foreach (Line line in lines)
            {
                string? overrideLineText = null;

                foreach(Word word in line.Words)
                {
                    bool isInt = int.TryParse(word.Text, out int parsedInt);
                    if (isInt && word.Confidence < intConfidenceThreshold)
                    {
                        // 1's can be mistaken as 11's, since there is a faint column to the left of the 1.
                        // Quantities should only be in increments of 5 anyway (unless it is a quantity of 1.)
                        if (parsedInt == 11)
                        {
                            overrideLineText = line.Text.Replace("11", "1");
                            Log.Information("Handled Low confidence integer found. OrderString {OrderString}, Integer {Integer}- overriden to 1.", orderStr, parsedInt);
                            wasLineOverriden = true;
                        }
                        else
                        {
                            Log.Warning("Unhandled Low confidence integer found- skipping order. OrderString {OrderString}, Integer {Integer}", orderStr, parsedInt);
                            lowConfidenceSkip = true;
                        }
                    }
                }
                string text = overrideLineText ?? line.Text;

                if (text == "Butterfly" || text == "Vertical")
                {
                    isComplexOrder = true;
                    continue;
                }

                string? normalizedOptionSymbol = TryNormalizeOptionSymbol(text);
                if (normalizedOptionSymbol != null)
                {
                    orderStr = normalizedOptionSymbol;
                    symbol = normalizedOptionSymbol;
                    continue;
                }

                // Date string comes at the end of an order, so we finalize and reset here.
                string? normalizedDateTimeStr = TryNormalizeDateTime(text);
                if (normalizedDateTimeStr != null)
                {
                    if (isLTXOrApprovedWMM &&
                        symbol.Length > 0 &&
                        !isComplexOrder)
                    {
                        if (lowConfidenceSkip)
                        {
                            skippedOrderDueToLowConfidence = true;
                        }
                        else
                        {
                            orderStr += " " + normalizedDateTimeStr;
                            ((List<string>) orderStrings).Add(orderStr);
                            if (wasLineOverriden)
                            {
                                Log.Warning("LTX or Approved WMM order has overriden low-confidence text.");
                            }
                        }
                    }
                    // Reset flags
                    symbol = "";
                    lowConfidenceSkip = false;
                    isLTXOrApprovedWMM = false;
                    isComplexOrder = false;
                    wasLineOverriden = false;
                    continue;
                }

                // Orders may be mistakenly labeled as WMM orders. If there is a current position
                // corresponding to a "WMM"- tagged order, we should assume it is mistagged/relevant.
                if (text == "LTX" || 
                    (text == "WMM" && currentPositionSymbols.Contains(symbol)))
                {
                    isLTXOrApprovedWMM = true;
                }

                orderStr += " " + text;
            }
            return skippedOrderDueToLowConfidence;
        }

        private string? TryNormalizeOptionSymbol(string orig)
        {
            if (! _optionSymbolRegexUnnormalized.IsMatch(orig)) return null;

            string withUnderscore = ReplaceFirst(orig, " ", "_");
            return ReplaceSpaceOrCommaWithPeriod(withUnderscore);
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

        /// <summary>
        /// Returns a list of FilledOrders for LottoX orders. We expect the input to be a valid string representation of a
        /// LottoX order, where the option symbol and DateTime are correct.
        /// We allow filled and limit prices to mistakenly include a space or comma in place of a period, and correct that here.
        /// </summary>
        private TimeSortedSet<FilledOrder> CreateFilledOrders(IEnumerable<string> orderStrings)
        {
            if (orderStrings.Count() == 0) return new TimeSortedSet<FilledOrder>();

            Regex regex = new Regex(@"^([A-Z]{1,5}_\d{6}[CP]\d+(.\d)?) (\d+[., ]\d+ )?([A-Za-z ]+?) (Market|\d+[., ]\d+) (.*? )?(LTX|WMM) (\d{2}/\d{2}/\d{2} \d{2}:\d{2}:\d{2} (AM|PM))");

            TimeSortedSet<FilledOrder> orders = new TimeSortedSet<FilledOrder>();
            foreach (string orderStr in orderStrings)
            {
                Match match = regex.Match(orderStr.Replace("|", ""));
                if (!match.Success)
                {
                    Exception ex = new FilledOrderParsingException("Could not parse order!");
                    Log.Warning(ex, "Could not parse order. Extracted text: " + orderStr);
                    throw ex;
                }

                string[] matches = match.Groups.Values.Select(group => group.Value).ToArray();

                // If there was no filled price, the order has not been filled and we need to skip it.
                if (matches[3] == "") continue;

                string symbol = matches[1];
                float price = float.Parse(ReplaceSpaceOrCommaWithPeriod(matches[3].Trim()));
                string? instruction = GetInstruction(matches[4]);
                if (instruction == null)
                {
                    Exception ex = new FilledOrderParsingException("Could not parse instruction from order!");
                    Log.Warning(ex, "Could not parse instruction from order. Extracted text: " + orderStr);
                    throw ex;
                }
                string orderType = matches[5] == "Market"
                    ? OrderType.MARKET
                    : OrderType.LIMIT;
                float limit = orderType == OrderType.MARKET
                    ? 0
                    : float.Parse(ReplaceSpaceOrCommaWithPeriod(matches[5]));
                bool hasQuantity = int.TryParse(matches[6].Trim(), out int quantity);
                if (!hasQuantity)
                {
                    Log.Information("!!!!! Could not parse quantity from order. Assuming 1. Extracted text: " + orderStr);
                    quantity = 1;
                }
                DateTime time = DateTime.Parse(matches[8]);

                FilledOrder order = new FilledOrder(symbol, price, instruction, orderType, limit, quantity, time);
                orders.Add(order);

                if (matches[7] == "WMM")
                {
                    Log.Warning("WMM order encountered for existing LottoX position. Treating as LTX order. Symbol {Symbol}, Order {@FilledOrder}", symbol, order);
                }
            }
            return orders;
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
