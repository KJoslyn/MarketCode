using AzureOCR;
using Core.Model;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LottoXService
{
    internal class ImageToPositionsConverter : OCRClient<Position>
    {
        public ImageToPositionsConverter(OCRConfig config, ModelBuilder<Position> builder) : base(config, builder) { }

        protected override bool Validate(IEnumerable<Line> lines)
        {
            List<string> lineTexts = lines.Select((line, index) => line.Text).ToList();

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
    }
}
