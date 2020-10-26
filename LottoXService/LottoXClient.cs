using Core.Model;
using PuppeteerSharp;
using RagingBull;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using AzureOCR;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections;
using System;
#nullable enable

namespace LottoXService
{
    public class LottoXClient : RagingBullClient
    {
        public LottoXClient(RagingBullConfig rbConfig, OCRConfig ocrConfig) : base(rbConfig, ocrConfig) { }

        public async Task<IList<Position>> GetPositionsFromImage(string filePath, string writeToJsonPath = null)
        {
            List<Position> positions = new List<Position>();

            IList<Line> lines = await ExtractLinesFromImage(filePath, writeToJsonPath);

            // TODO: Ensure the order of the columns
            // Symbol, Quantity, Last, Averag,                Open P/L (Acct), Open P/L %, Market Value

            Regex symbolRegex = new Regex(@"^[A-Z]{1,5} \d{6}[CP]\d+");

            List<int> symbolIndices = lines
                .Select((line, index) => new { Line = line, Index = index })
                .Where(obj => symbolRegex.IsMatch(obj.Line.Text))
                .Select(obj => obj.Index).ToList();

            List<string> lineTexts = lines.Select((line, index) => line.Text).ToList();

            if (symbolIndices.Count == 0)
            {
                return positions;
            }

            int numColumns = 7;
            for (int i = 0; i < symbolIndices.Count; i++)
            {
                int numLinesInThisSymbol = symbolIndices.Count > i + 1
                    ? symbolIndices[i + 1] - symbolIndices[i]
                    : Math.Min(numColumns, lines.Count - symbolIndices[i]);

                List<string> subList = lineTexts.GetRange(symbolIndices[i], numLinesInThisSymbol);

                string joined = string.Join(" ", subList);
                string normalized = ReplaceFirst(joined, " ", "_");
                string[] parts = normalized.Split(" ");

                Instrument instrument = Instrument.CreateOptionFromSymbol(parts[0]); // TODO: try-catch?
                float marketValue = float.Parse(parts[6].Substring(1)); // Take '$' off front
                Position position = new Position(0, float.Parse(parts[3]), float.Parse(parts[1]), instrument, marketValue);
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

        public override IList<Position> GetPositions()
        {
            //TryLogin().Wait();
            //TakeScreenshot().Wait();

            return null;
            //throw new NotImplementedException();
        }

        private async Task TakeScreenshot()
        {
            int maxAttempts = 5;
            int n = 0;
            Page page = await GetPage();
            ElementHandle? el = null;
            while (el == null && n < maxAttempts)
            {
                el = await GetElement(await GetPage(), "//div[@id='jwPlayer']");
                await Task.Delay(1000);
                n++;
                Log.Information("n = " + n);
            }
            await page.ScreenshotAsync("C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/new.png");
        }
    }
}
