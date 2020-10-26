using AzureOCR;
using Core;
using Core.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TDAmeritrade;
#nullable enable

namespace LottoXService
{
    public class Worker : BackgroundService
    {
        private readonly RagingBullConfig _ragingBullConfig;
        private readonly TDAmeritradeConfig _tdAmeritradeConfig;
        private readonly OCRConfig _ocrConfig;
        private IBrokerClient BrokerClient { get; }
        private IPortfolioClient PortfolioClient { get; }

        public Worker(IOptions<RagingBullConfig> rbOptions, IOptions<TDAmeritradeConfig> tdOptions, IOptions<OCRConfig> ocrOptions)
        {
            _ragingBullConfig = rbOptions.Value;
            _tdAmeritradeConfig = tdOptions.Value;
            _ocrConfig = ocrOptions.Value;

            BrokerClient = new TDClient(_tdAmeritradeConfig);
            PortfolioClient = new LottoXClient(_ragingBullConfig, _ocrConfig);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Get LottoX portfolio positions
            // Get my positions
            // Make trades

            await ((LottoXClient)PortfolioClient).GetPositionsFromImage("C:/Users/Admin/Pictures/Screenshots/LottoXCropped.json");

            //IList<Position> positions = BrokerClient.GetPositions();
            //Console.WriteLine(positions);
            try
            {
                //PortfolioClient.GetPositions();
            }
            catch (Exception ex)
            {

            }
            finally
            {
                //await PortfolioClient.Logout();
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
