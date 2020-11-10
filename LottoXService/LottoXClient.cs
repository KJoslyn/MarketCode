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

        public override async Task<bool> Login()
        {
            bool loginResult = await base.Login();
            await Task.Delay(6000);

            string filepath = GetNextHeaderFilepath();
            await TakeHeaderScreenshot(filepath);
            HeaderConsistencyClient.Init(filepath);

            return loginResult;
        }

        public override async Task<bool> HavePositionsChanged(bool? groundTruthChanged)
        {
            if (await HasHeaderChanged())
            {
                InvalidPortfolioStateException ex = new InvalidPortfolioStateException("Header has changed");
                Log.Information(ex, "Portfolio header has changed!");
                throw ex;
            }
            string filepath = GetNextQuantityFilepath();
            await TakeQuantityColumnScreenshot(filepath);
            return QuantityConsistencyClient.TestAndSetCurrentImage(filepath, groundTruthChanged);
        }

        public override Task<bool> HaveOrdersChanged(bool? groundTruthChanged)
        {
            throw new System.NotImplementedException();
        }

        protected override async Task<IList<Position>> RecognizeLivePositions()
        {
            Log.Information("Getting live positions");
            string filepath = GetNextPortfolioFilepath();
            await TakePortfolioScreenshot(filepath);
            IList<Position> positions = await ImageToPositionsConverter.GetPositionsFromImage(filepath);
            //IList <Position> positions = await ImageToPositionsConverter.GetPositionsFromImage("C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/new.json",
            //    "C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/new.json"
            //    );
            return positions;
        }

        protected override Task<IList<FilledOrder>> RecognizeLiveOrders()
        {
            throw new System.NotImplementedException();
        }

        private async Task<bool> HasHeaderChanged()
        {
            string filepath = GetNextHeaderFilepath();
            await TakeHeaderScreenshot(filepath);
            return HeaderConsistencyClient.TestAndSetCurrentImage(filepath);
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

        private string GetNextHeaderFilepath()
        {
            int next = getCurrentHeaderScreenshotNumber() + 1;
            return "C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/header-" + next + ".png";
        }

        private string GetNextQuantityFilepath()
        {
            int current = getCurrentHeaderScreenshotNumber();
            return "C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/quantities-" + current + ".png";
        }

        private string GetNextPortfolioFilepath()
        {
            int current = getCurrentHeaderScreenshotNumber();
            return "C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/portfolio-" + current + ".png";
        }


        private async Task TakeHeaderScreenshot(string filepath)
        {
            Page page = await GetPage();
            await page.ScreenshotAsync(filepath,
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
