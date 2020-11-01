using AzureOCR;
using Core;
using Core.Model;
using Database;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly OrderConfig _orderConfig;

        public Worker(
            IOptions<RagingBullConfig> rbOptions, 
            IOptions<TDAmeritradeConfig> tdOptions, 
            IOptions<OCRConfig> ocrOptions, 
            IOptions<GeneralConfig> generalOptions,
            IOptions<OrderConfig> orderOptions)
        {
            _ragingBullConfig = rbOptions.Value;
            _tdAmeritradeConfig = tdOptions.Value;
            _ocrConfig = ocrOptions.Value;
            _generalConfig = generalOptions.Value;
            _orderConfig = orderOptions.Value;

            PositionDatabase lottoxDatabase = new LitePositionDatabase(_generalConfig.LottoxDatabasePath);
            LivePortfolioClient = new LottoXClient(_ragingBullConfig, _ocrConfig, lottoxDatabase);
            TDClient tdClient = new TDClient(_tdAmeritradeConfig);

            if (_generalConfig.UsePaperTrade)
            {
                PositionDatabase paperDatabase = new LitePositionDatabase(_generalConfig.PaperTradeDatabasePath);
                BrokerClient = new PaperTradeBrokerClient(paperDatabase, tdClient);
            }
            else
            {
                BrokerClient = tdClient; 
            }
            OrderManager = new OrderManager(BrokerClient, _orderConfig);
        }

        private IBrokerClient BrokerClient { get; }
        private LivePortfolioClient LivePortfolioClient { get; }
        private OrderManager OrderManager { get; }

        class DeltaList
        {
            public IList<PositionDelta> Deltas { get; set; }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //IList<Position> positions = BrokerClient.GetPositions();

            //string symbol = "SFIX_201120C35";
            //string symbol = "SFIX";
            //OptionQuote quote = BrokerClient.GetQuote(symbol);

            //Log.Information("Quote: {@Quote}", quote);

            try
            {
                //(IList<Position> livePositions, IList<PositionDelta> deltas) = await LivePortfolioClient.GetLivePositionsAndDeltas();

                IList<Position> livePositions = await ((LottoXClient)LivePortfolioClient).GetPositionsFromImage(
                    "C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/1.json",
                    "C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/1.json");

                Console.WriteLine(livePositions);

                IList<Position> offlinePositions = await ((LottoXClient)LivePortfolioClient).GetPositionsFromImage(
                    "C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/offline.json",
                    "C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/offline.json");

                Console.WriteLine(offlinePositions);

            } catch (InvalidPortfolioStateException ex)
            {
                // TODO: Email/text notification???
                Console.WriteLine("Invalid portfolio state");
            }

            //DeltaList list;
            //using (StreamReader r = new StreamReader("C:/Users/Admin/WindowsServices/MarketCode/LottoXService/deltas.json"))
            //{
            //    string json = r.ReadToEnd();
            //    list = JsonConvert.DeserializeObject<DeltaList>(json);
            //}
            //IList<PositionDelta> deltas = list.Deltas;

            //foreach (PositionDelta delta in deltas)
            //{
            //    Order? order = OrderManager.DecideOrder(delta);
            //    if (order != null)
            //    {
            //        BrokerClient.PlaceOrder(order, delta.Price);
            //    }
            //}

            //IList<Position> livePositions = await ((LottoXClient)LivePortfolioClient).GetPositionsFromImage(
            //    "C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/offline.png",
            //    "C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/offline.json");

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
