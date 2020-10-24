using Core;
using Core.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PuppeteerSharp;
using Serilog;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TDAmeritrade;

namespace LottoXService
{
    public class Worker : BackgroundService
    {
        private readonly RagingBullConfig _ragingBullConfig;
        private readonly TDAmeritradeConfig _tdAmeritradeConfig;
        private IBrokerClient Client { get; }

        public Worker(IOptions<RagingBullConfig> rbOptions, IOptions<TDAmeritradeConfig> tdOptions)
        {
            _ragingBullConfig = rbOptions.Value;
            _tdAmeritradeConfig = tdOptions.Value;

            Client = new TDClient(_tdAmeritradeConfig);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await LoadLottoXPortfolio();
            IList<Position> positions = Client.GetPositions();

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task<Response> LoadLottoXPortfolio()
        {
            //// This only downloads the browser version if it is has not been downloaded already
            //await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
            //var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            //{
            //    Headless = true
            //});
            //var page = await browser.NewPageAsync();
            //await page.GoToAsync("https://ragingbull.com/");
            //await page.ScreenshotAsync("C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/new.png", new ScreenshotOptions
            //{
            //    Clip = new Clip
            //    {
            //        X = 50,
            //        Y = 50,
            //        Width = 75,
            //        Height = 75
            //    }
            //});
            Log.Information(_ragingBullConfig.Username);
            return null;
        }
    }
}
