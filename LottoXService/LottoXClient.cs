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
        public LottoXClient(RagingBullConfig config) : base(config) { }

        public override IList<Position> GetPositions()
        {
            TryLogin().Wait();
            TakeScreenshot().Wait();

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
                n++;
                Log.Information("n = " + n);
                await Task.Delay(1000);
                el = await GetElement(await GetPage(), "//div[@id='jwPlayer']");
            }
            await page.ScreenshotAsync("C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/new.png");
        }
    }
}
