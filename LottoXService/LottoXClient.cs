using AzureOCR;
using Core;
using Core.Model;
using PuppeteerSharp;
using RagingBull;
using Serilog;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
#nullable enable

namespace LottoXService
{
    public class LottoXClient : RagingBullClient
    {
        public LottoXClient(RagingBullConfig rbConfig, OCRConfig ocrConfig, PositionDatabase positionDB) : base(rbConfig, positionDB)
        {
            ImageToPositionsConverter = new ImageToPositionsConverter(ocrConfig);
            QuantityConsistencyClient = new ImageConsistencyClient();
            HeaderConsistencyClient = new ImageConsistencyClient();
        }

        private ImageToPositionsConverter ImageToPositionsConverter { get; }

        private ImageConsistencyClient QuantityConsistencyClient { get; }
        private ImageConsistencyClient HeaderConsistencyClient { get; }

        // TODO: Remove eventually
        public async Task<IList<Position>> GetPositionsFromImage(string filePath, string writeToJsonPath = null)
        {
            IList<Position> positions = await ImageToPositionsConverter.GetPositionsFromImage(filePath, writeToJsonPath);
            return positions;
        }

        public override async Task<bool> HasPortfolioChanged(bool? groundTruthChanged)
        {
            if (!await IsLoggedIn())
            {
                await Login();
                await Task.Delay(6000);
            }
            if (!await IsHeaderConsistent())
            {
                await Login();
                await Task.Delay(6000);
            }
            int current = getCurrentHeaderScreenshotNumber();
            string filePath = "C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/quantities-" + current + ".png";
            await TakeQuantityColumnScreenshot(filePath);
            return QuantityConsistencyClient.TestAndSetCurrentImage(filePath, groundTruthChanged);
        }

        private int getCurrentHeaderScreenshotNumber()
        {
            Regex reg = new Regex(@"header-(\d+).png");
            string[] files = Directory.GetFiles("C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots", "*.png");
            IEnumerable<string> matches = files.Where(path => reg.IsMatch(path));
            if (matches.Count() == 0)
            {
                return 0;
            }
            IEnumerable<int> numbers = matches
                .Select(path => int.Parse(reg.Match(path).Groups[1].Value));
            return numbers.Max();
        }

        private async Task<bool> IsHeaderConsistent()
        {
            int next = getCurrentHeaderScreenshotNumber() + 1;
            string filePath = "C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/header-" + next + ".png";
            await TakeHeaderScreenshot(filePath);
            return HeaderConsistencyClient.TestAndSetCurrentImage(filePath);
        }

        protected override async Task<IList<Position>> GetLivePositions()
        {
            // TODO: This doesn't really need to be checked since we already checked this recently in HasPortfolioChanged()
            if (!await IsLoggedIn())
            {
                await Login();
                await Task.Delay(6000);
            }
            Log.Information("Getting live positions");
            int current = getCurrentHeaderScreenshotNumber();
            string filePath = "C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/portfolio-" + current + ".png";
            await TakePortfolioScreenshot(filePath);
            IList<Position> positions = await ImageToPositionsConverter.GetPositionsFromImage(filePath);
            //IList <Position> positions = await ImageToPositionsConverter.GetPositionsFromImage("C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/new.json",
            //    "C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/new.json"
            //    );
            return positions;
        }

        private async Task TakeHeaderScreenshot(string filePath)
        {
            Page page = await GetPage();
            await page.ScreenshotAsync(filePath,
                new ScreenshotOptions { Clip = new PuppeteerSharp.Media.Clip { Width = 1000, Height = 350, X = 250 } });
        }

        private async Task TakeQuantityColumnScreenshot(string filePath)
        {
            Page page = await GetPage();
            await page.ScreenshotAsync(filePath,
                new ScreenshotOptions { Clip = new PuppeteerSharp.Media.Clip { Width = 65, Height = 350, X = 477, Y = 400 } });
        }

        private async Task TakePortfolioScreenshot(string filePath)
        {
            Page page = await GetPage();
            await page.ScreenshotAsync(filePath,
                new ScreenshotOptions { Clip = new PuppeteerSharp.Media.Clip { Width = 1000, Height = 1440 } });
        }
    }
}
