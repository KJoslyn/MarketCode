using AzureOCR;
using Core;
using Core.Model;
using Database;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
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
        private readonly GeneralConfig _generalConfig;

        public Worker(IOptions<RagingBullConfig> rbOptions, IOptions<TDAmeritradeConfig> tdOptions, IOptions<OCRConfig> ocrOptions, IOptions<GeneralConfig> generalOptions)
        {
            _ragingBullConfig = rbOptions.Value;
            _tdAmeritradeConfig = tdOptions.Value;
            _ocrConfig = ocrOptions.Value;
            _generalConfig = generalOptions.Value;

            PositionDatabase lottoxDatabase = new PositionDatabase(_generalConfig.LottoxDatabasePath);
            LivePortfolioClient = new LottoXClient(_ragingBullConfig, _ocrConfig, lottoxDatabase);

            if (_generalConfig.UsePaperTrade)
            {
                PositionDatabase paperDatabase = new PositionDatabase(_generalConfig.PaperTradeDatabasePath);
                BrokerClient = new PaperTradeBrokerClient(paperDatabase);
            }
            else
            {
                BrokerClient = new TDClient(_tdAmeritradeConfig);
            }
        }

        private IBrokerClient BrokerClient { get; }
        private LivePortfolioClient LivePortfolioClient { get; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            (IList<Position> livePositions, IList<PositionDelta> deltas) = await LivePortfolioClient.GetLivePositionsAndDeltas();

            // Get LottoX portfolio positions
            // Get my positions
            // Make trades
            //IList<Position> livePositions = await ((LottoXClient)LivePortfolioClient).GetPositionsFromImage("C:/Users/Admin/Pictures/Screenshots/LottoXCropped.json");

            //IList<Position> oldPositions = LottoxPositionsDB.GetPositions();

            //IList<PositionDelta> deltas = LottoxPositionsDB.ComputePositionDeltas(livePositions);
            //LottoxPositionsDB.UpdatePositionsAndDeltas(livePositions, deltas);
            //IList<Position> updatedPositions = LottoxPositionsDB.GetPositions();

            //Console.WriteLine(oldPositions);
            //Console.WriteLine(updatedPositions);

            //IList<Position> positions = BrokerClient.GetPositions();

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
