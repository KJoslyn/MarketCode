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
using PuppeteerSharp.Input;
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

        public override async Task<bool> Logout()
        {
            try
            {
                await DoubleClickPortfolio();
            } catch (Exception ex)
            {
                Log.Fatal(ex, "Could not find live portfolio element when logging out");
                return false;
            }
            return await base.Logout();
        }

        protected override async Task<bool> TryLogin()
        {
            if (await IsLoggedIn()) return true;

            bool baseResult = await base.TryLogin();
            if (!baseResult) return false;

            try
            {
                await Task.Delay(3000);
                await DoubleClickPortfolio();
            } catch (Exception ex)
            {
                Log.Fatal(ex, "Could not find live portfolio element when logging in");
                return false;
            }
            return true;
        }

        public override IList<Position> GetPositions()
        {
            TryLogin().Wait();
            Task.Delay(3000).Wait();
            TakeScreenshot("1.png").Wait();
            Logout().Wait();

            return null;
            //throw new NotImplementedException();
        }

        private async Task DoubleClickPortfolio()
        {
            int maxAttempts = 5;
            int n = 0;
            ElementHandle? el;
            do
            {
                el = await GetElement(await GetPage(), "//div[@id='jwPlayer']");
                if (el == null)
                {
                    await Task.Delay(1000);
                    n++;
                    Log.Information("n = " + n);
                }
            }
            while (el == null && n < maxAttempts);

            if (el == null)
            {
                throw new RagingBullException("Could not find live portfolio on page");
            }
            await Task.Delay(3000);
            await el.ClickAsync(new ClickOptions { ClickCount = 1, Delay = 10 });
            await el.ClickAsync(new ClickOptions { ClickCount = 2, Delay = 10 });

            await Task.Delay(1000);
            //await el.TapAsync();
            //await el.TapAsync();
        }

        private async Task TakeScreenshot(string filename)
        {
            Page page = await GetPage();
            await page.ScreenshotAsync("C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/" + filename);
        }
    }
}
