using AzureOCR;
using Core.Model;
using PuppeteerSharp;
using RagingBull;
using System.Collections.Generic;
using System.Threading.Tasks;
#nullable enable

namespace LottoXService
{
    public class LottoXClient : RagingBullClient
    {
        public LottoXClient(RagingBullConfig rbConfig, OCRConfig ocrConfig) : base(rbConfig)
        {
            ImageToPositionsConverter = new ImageToPositionsConverter(ocrConfig);
        }

        private ImageToPositionsConverter ImageToPositionsConverter { get; }

        // TODO: Remove eventually
        public async Task<IList<Position>> GetPositionsFromImage(string filePath, string writeToJsonPath = null)
        {
            IList<Position> positions = await ImageToPositionsConverter.GetPositionsFromImage(filePath, writeToJsonPath);
            return positions;
        }

        public override async Task<(IList<Position>, IList<PositionDelta>)> GetLivePositionsAndDeltas()
        {
            await TryLogin();
            await Task.Delay(6000);
            await TakeScreenshot("1.png");
            //IList<Position> positions = await ImageToPositionsConverter.GetPositionsFromImage("C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/1.png");

            // TODO: Remove
            await Logout();

            //return positions;
            return (null, null);
            //throw new NotImplementedException();
        }

        private async Task TakeScreenshot(string filename)
        {
            Page page = await GetPage();
            await page.ScreenshotAsync("C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/" + filename,
                new ScreenshotOptions { Clip = new PuppeteerSharp.Media.Clip { Width = 1000, Height = 1440 } });
        }
    }
}
