using AzureOCR;
using Core;
using Core.Model;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LottoXService
{
    internal class ImageToOrdersConverter : ImageToModelsClient<FilledOrder>
    {
        public ImageToOrdersConverter(OCRConfig config, ModelBuilder<FilledOrder> builder) : base(config, builder) { }

        protected override void ValidateOrThrow(IEnumerable<Line> lines)
        {
            List<string> lineTexts = lines.Select((line, index) => line.Text).ToList();
            bool valid = Validate(lineTexts);

            if (!valid)
            {
                InvalidPortfolioStateException ex = new InvalidPortfolioStateException("Invalid portfolio state attempting to parse orders");
                Log.Warning(ex, "Invalid portfolio state attempting to parse orders. Extracted text: {@Text}", lineTexts);
                throw ex;
            }
        }

        private bool Validate(List<string> lineTexts)
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
    }
}
