using AzureOCR;
using Core;
using Core.Model;
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
    internal class ImageToPositionsConverter : OCRClient
    {
        public ImageToPositionsConverter(OCRConfig config) : base(config) { }

        public async Task<IList<Position>> GetPositionsFromImage(string filePath, string writeToJsonPath = null)
        {
            IList<Line> lines = await ExtractLinesFromImage(filePath, writeToJsonPath);

            // We will use only the text part of the line
            List<string> lineTexts = lines.Select((line, index) => line.Text).ToList();

            bool valid = Validate(lineTexts);
            if (!valid)
            {
                Exception ex = new InvalidPortfolioStateException("Invalid portfolio state");
                Log.Warning(ex, "Invalid portfolio state. Extracted text: {@Text}", lineTexts);
                throw ex;
            }

            // Get the indices of the lines that are option symbols
            Regex symbolRegex = new Regex(@"^[A-Z]{1,5} \d{6}[CP]\d+");
            List<int> symbolIndices = lines
                .Select((line, index) => new { Line = line, Index = index })
                .Where(obj => symbolRegex.IsMatch(obj.Line.Text))
                .Select(obj => obj.Index).ToList();

            if (symbolIndices.Count == 0) return new List<Position>();

            return CreatePositions(lineTexts, symbolIndices);
        }

        private bool Validate(List<string> lineTexts)
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

        private List<Position> CreatePositions(List<string> lineTexts, List<int> symbolIndices)
        {
            List<Position> positions = new List<Position>();
            int numColumns = 7;
            for (int i = 0; i < symbolIndices.Count; i++)
            {
                int numLinesInThisSymbol = symbolIndices.Count > i + 1
                    ? symbolIndices[i + 1] - symbolIndices[i]
                    : Math.Min(numColumns, lineTexts.Count - symbolIndices[i]);

                List<string> subList = lineTexts.GetRange(symbolIndices[i], numLinesInThisSymbol);

                string joined = string.Join(" ", subList);
                string normalized = ReplaceFirst(joined, " ", "_");
                string[] parts = normalized.Split(" ");

                Position position = new Position(parts[0], float.Parse(parts[1]), float.Parse(parts[3]));
                positions.Add(position);
            }
            return positions;
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
    }
}
