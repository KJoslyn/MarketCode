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
