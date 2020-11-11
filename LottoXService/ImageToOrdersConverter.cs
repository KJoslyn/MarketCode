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
        public ImageToOrdersConverter(OCRConfig config) : base(config) { }

        public async Task<IList<FilledOrder>> GetFilledOrdersFromImage(string filePath, string writeToJsonPath = null)
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

            List<string> orderStrings = GetLottoxOrderStrings(lines.ToList(), lineTexts);
            if (orderStrings.Count == 0) return new List<FilledOrder>();

            return CreateFilledOrders(orderStrings);
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

        private List<string> GetLottoxOrderStrings(List<Line> lines, List<string> lineTexts)
        {
            Regex optionSymbolRegex = new Regex(@"^[A-Z]{1,5} \d{6}[CP]\d+(.\d+)?");

            //Sometimes the colons in the date are interpreted as periods.
            Regex dateTimeRegex = new Regex(@"(\d{2}/\d{2}/\d{2}) (\d{2}[ :.]\d{2}[ :.]\d{2}) (AM|PM)");

            List<string> orderStrings = new List<string>();
            string thisOrderStr = "";
            bool isLTX = false;
            foreach (string text in lineTexts)
            {
                if (optionSymbolRegex.IsMatch(text))
                {
                    thisOrderStr = text.Replace(" ", "_");
                    continue;
                }

                Match dateTimeMatch = dateTimeRegex.Match(text);
                if (dateTimeMatch.Success && isLTX)
                {
                    string[] groups = dateTimeMatch.Groups.Values.Select(group => group.Value).ToArray();
                    string dateTime = string.Format("{0} {1} {2}",
                        groups[1],
                        groups[2].Replace(" ", ":").Replace(".", ":"),
                        groups[3]);
                    thisOrderStr += " " + dateTime;

                    orderStrings.Add(thisOrderStr);
                    isLTX = false;
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

        private List<FilledOrder> CreateFilledOrders(List<string> orderStrings)
        {
            Regex regex = new Regex(@"^([A-Z]{1,5}_\d{6}[CP]\d+(.\d+)?) (\d+[.]\d+) (Sell to Close|Buy to Open) (Market|\d+[.]\d+) (\d+) ([a-zA-Z]+) (\d{2}/\d{2}/\d{2} \d{2}:\d{2}:\d{2} (AM|PM))");

            List<FilledOrder> orders = new List<FilledOrder>();
            foreach (string orderStr in orderStrings)
            {
                Match match = regex.Match(orderStr);
                if (!match.Success)
                {
                    Exception ex = new InvalidPortfolioStateException("Could not parse lottoX order!");
                    Log.Warning(ex, "Could not parse lottoX order. Extracted text: " + orderStr);
                    throw ex;
                }
                string[] matches = match.Groups.Values.Select(group => group.Value).ToArray();

                string symbol = matches[1];
                float price = float.Parse(matches[3]);
                string instruction = matches[4] == "Buy to Open"
                    ? InstructionType.BUY_TO_OPEN
                    : InstructionType.SELL_TO_CLOSE;
                string orderType = matches[5] == "Market"
                    ? OrderType.MARKET
                    : OrderType.LIMIT;
                float limit = orderType == OrderType.MARKET
                    ? 0
                    : float.Parse(matches[5]);
                int quantity = int.Parse(matches[6]);
                DateTime time = DateTime.Parse(matches[8].Replace(".", ":"));

                FilledOrder order = new FilledOrder(symbol, price, instruction, orderType, limit, quantity, time);
                orders.Add(order);
            }
            return orders;
        }
    }
}
