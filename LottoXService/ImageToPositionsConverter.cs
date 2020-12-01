using AzureOCR;
using Core;
using Core.Model;
using Core.Model.Constants;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using PuppeteerSharp;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
#nullable enable

namespace LottoXService
{
    internal class ImageToPositionsConverter : OCRClient
    {
        public ImageToPositionsConverter(OCRConfig config) : base(config) { }

        public async Task<IList<Position>> GetPositionsFromImage(string filePath, string writeToJsonPath = null)
        {
            IList<Line> lines = await ExtractLinesFromImage(filePath, writeToJsonPath);

            // We will use only the text part of the line
            List<string> lineTexts = lines.Select((line, index) => line.Text).ToList();

            bool valid = ValidatePositionsColumnHeaders(lineTexts);
            if (!valid)
            {
                Exception ex = new InvalidPortfolioStateException("Invalid portfolio state");
                Log.Warning(ex, "Invalid portfolio state. Extracted text: {@Text}", lineTexts);
                throw ex;
            }

            return CreatePositions(lines);
        }

        private bool ValidatePositionsColumnHeaders(List<string> lineTexts)
        {
            int indexOfSymbol = lineTexts
                .Select((text, index) => new { Text = text, Index = index })
                .Where(obj => obj.Text == "Symbol")
                .Select(obj => obj.Index)
                .FirstOrDefault(); // Default is 0

            // We are looking for the 4 column headers in this order: "Symbol", "Quantity", "Last", and "Average"
            // However, "Quantity" may be interpreted as "A Quantity" due to the arrow to the left of the text "Quantity".
            // "Average" may be cut off.
            List<string> subList = lineTexts.GetRange(indexOfSymbol, 4);
            string joined = string.Join(" ", subList);
            Regex headersRegex = new Regex("^Symbol (. )?Quantity Last Aver");

            return headersRegex.IsMatch(joined);
        }

        private List<Position> CreatePositions(IList<Line> lines)
        {
            List<Position> positions = new List<Position>();
            PositionBuilder builder = new PositionBuilder();
            foreach (Line line in lines)
            {
                foreach (Word word in line.Words)
                {
                    builder.TakeNextWord(word);
                    if (builder.Done)
                    {
                        Position pos = builder.BuildAndReset();
                        positions.Add(pos);
                    }
                }
            }
            return positions;
        }
    }
}
