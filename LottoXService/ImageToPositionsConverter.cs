using AzureOCR;
using Core.Model;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

            // TODO: Ensure the order of the columns
            // Symbol, Quantity, Last, Averag,                Open P/L (Acct), Open P/L %, Market Value

            // Get the indices of the lines that are option symbols
            Regex symbolRegex = new Regex(@"^[A-Z]{1,5} \d{6}[CP]\d+");
            List<int> symbolIndices = lines
                .Select((line, index) => new { Line = line, Index = index })
                .Where(obj => symbolRegex.IsMatch(obj.Line.Text))
                .Select(obj => obj.Index).ToList();

            if (symbolIndices.Count == 0) return new List<Position>();

            return CreatePositions(lineTexts, symbolIndices);
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
