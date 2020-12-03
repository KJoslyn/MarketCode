using AzureOCR;
using Core.Model;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LottoXService
{
    internal class ImageToOrdersConverter : OCRClient<FilledOrderAndQuote>
    {
        public ImageToOrdersConverter(OCRConfig config, ModelBuilder<FilledOrderAndQuote> builder) : base(config, builder) { }

        protected override bool Validate(IEnumerable<Line> lines)
        {
            List<string> lineTexts = lines.Select((line, index) => line.Text).ToList();

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
    }
}
