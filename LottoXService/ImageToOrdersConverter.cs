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
        public async Task<(string, IList<FilledOrder>)> GetFilledOrdersFromImage(string filePath, string writeToJsonPath = null)
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

            List<string> orderStrings = GetLottoxOrderStrings(lineTexts);
            //if (orderStrings.Count == 0) return new List<FilledOrder>();
            if (orderStrings.Count == 0) return (GetFirstNormalizedDateTime(lineTexts), new List<FilledOrder>());

            //return CreateFilledOrders(orderStrings));
            return (GetFirstNormalizedDateTime(lineTexts), CreateFilledOrders(orderStrings));
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
        private List<string> GetLottoxOrderStrings(List<string> lineTexts)
        {
            List<string> orderStrings = new List<string>();
            string thisOrderStr = "";
            bool isLTX = false;
            foreach (string text in lineTexts)
            {
                string? normalizedOptionSymbol = TryNormalizeOptionSymbol(text);
                if (normalizedOptionSymbol != null)
                {
                    thisOrderStr = normalizedOptionSymbol;
                    continue;
                }

                string? normalizedDateTime = TryNormalizeDateTime(text);
                if (normalizedDateTime != null)
                {
                    if (isLTX)
                    {
                        thisOrderStr += " " + normalizedDateTime;
                        orderStrings.Add(thisOrderStr);
                        isLTX = false;
                    }
                    continue;
                }

                if (text == "LTX")
                {
                    isLTX = true;
                }

                thisOrderStr += " " + text;
            }
            return orderStrings;
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
        private List<FilledOrder> CreateFilledOrders(List<string> orderStrings)
        {
            Regex regex = new Regex(@"^([A-Z]{1,5}_\d{6}[CP]\d+(.\d)?) (\d+[., ]\d+) (Sell to Close|Buy to Open) (Market|\d+[., ]\d+) (\d+) (LTX) (\d{2}/\d{2}/\d{2} \d{2}:\d{2}:\d{2} (AM|PM))");

            List<FilledOrder> orders = new List<FilledOrder>();
            foreach (string orderStr in orderStrings)
            {
                Match match = regex.Match(orderStr);
                if (!match.Success)
                {
                    Exception ex = new FilledOrderParsingException("Could not parse lottoX order!");
                    Log.Warning(ex, "Could not parse lottoX order. Extracted text: " + orderStr);
                    throw ex;
                }
                string[] matches = match.Groups.Values.Select(group => group.Value).ToArray();

                string symbol = matches[1];
                float price = float.Parse(ReplaceSpaceOrCommaWithPeriod(matches[3]));
                string instruction = matches[4] == "Buy to Open"
                    ? InstructionType.BUY_TO_OPEN
                    : InstructionType.SELL_TO_CLOSE;
                string orderType = matches[5] == "Market"
                    ? OrderType.MARKET
                    : OrderType.LIMIT;
                float limit = orderType == OrderType.MARKET
                    ? 0
                    : float.Parse(ReplaceSpaceOrCommaWithPeriod(matches[5]));
                int quantity = int.Parse(matches[6]);
                DateTime time = DateTime.Parse(matches[8]);

                FilledOrder order = new FilledOrder(symbol, price, instruction, orderType, limit, quantity, time);
                orders.Add(order);
            }
            return orders;
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
