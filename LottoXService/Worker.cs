using AzureOCR;
using Core;
using Core.Model;
using Database;
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
        private readonly DatabaseConfig _dbConfig;

        public Worker(IOptions<RagingBullConfig> rbOptions, IOptions<TDAmeritradeConfig> tdOptions, IOptions<OCRConfig> ocrOptions, IOptions<DatabaseConfig> dbOptions)
        {
            _ragingBullConfig = rbOptions.Value;
            _tdAmeritradeConfig = tdOptions.Value;
            _ocrConfig = ocrOptions.Value;
            _dbConfig = dbOptions.Value;

            if (_dbConfig.UsePaperTrade)
            {
                BrokerClient = new PaperTradeBrokerClient(_dbConfig.PaperTradeDatabasePath);
            } else
            {
                BrokerClient = new TDClient(_tdAmeritradeConfig);
            }
            LivePortfolioClient = new LottoXClient(_ragingBullConfig, _ocrConfig);
            LottoxPositionsDB = new PositionDB(_dbConfig.LottoxDatabasePath);
        }

        private IBrokerClient BrokerClient { get; }
        private ILivePortfolioClient LivePortfolioClient { get; }
        private PositionDB LottoxPositionsDB { get; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            LivePortfolioClient.GetPositions();

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
