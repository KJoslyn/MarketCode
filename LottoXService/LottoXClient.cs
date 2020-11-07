using AzureOCR;
using Core;
using Core.Model;
using PuppeteerSharp;
using RagingBull;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;
#nullable enable

namespace LottoXService
{
    public class LottoXClient : RagingBullClient
    {
        public LottoXClient(RagingBullConfig rbConfig, OCRConfig ocrConfig, PositionDatabase positionDB) : base(rbConfig, positionDB)
        {
            ImageToPositionsConverter = new ImageToPositionsConverter(ocrConfig);
            ImageConsistencyClient = new ImageConsistencyClient();
        }

        private ImageToPositionsConverter ImageToPositionsConverter { get; }

        private ImageConsistencyClient ImageConsistencyClient { get; }

        // TODO: Remove eventually
        public async Task<IList<Position>> GetPositionsFromImage(string filePath, string writeToJsonPath = null)
        {
            IList<Position> positions = await ImageToPositionsConverter.GetPositionsFromImage(filePath, writeToJsonPath);
            return positions;
        }

        public override async Task<bool> HasPortfolioChanged()
        {
            string filePath = "C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/quantities.jpg";
            await TakeQuantityColumnScreenshot(filePath);
            return ImageConsistencyClient.TestAndSetCurrentImage(filePath);
        }

        protected override async Task<IList<Position>> GetLivePositions()
        {
            await TryLogin();
            await Task.Delay(6000);
            Log.Information("Getting live positions");
            string filePath = "C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/portfolio.png";
            await TakePortfolioScreenshot(filePath);
            IList<Position> positions = await ImageToPositionsConverter.GetPositionsFromImage(filePath);
            //IList <Position> positions = await ImageToPositionsConverter.GetPositionsFromImage("C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/new.json",
            //    "C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/new.json"
            //    );
            return positions;
        }

        private async Task TakeQuantityColumnScreenshot(string filePath)
        {
            Page page = await GetPage();
            await page.ScreenshotAsync(
                filePath,
                new ScreenshotOptions { 
                    Clip = new PuppeteerSharp.Media.Clip { Width = 1000, Height = 1440 }, 
                    Type = ScreenshotType.Jpeg 
                });
        }

        private async Task TakePortfolioScreenshot(string filePath)
        {
            Page page = await GetPage();
            await page.ScreenshotAsync(filePath,
                new ScreenshotOptions { Clip = new PuppeteerSharp.Media.Clip { Width = 1000, Height = 1440 } });
        }
    }
}
