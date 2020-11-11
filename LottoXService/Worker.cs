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
using System.Diagnostics.CodeAnalysis;
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
        private IHostApplicationLifetime _hostApplicationLifetime;

        public Worker(
            IHostApplicationLifetime hostApplicationLifetime,
            IOptions<RagingBullConfig> rbOptions, 
            IOptions<TDAmeritradeConfig> tdOptions, 
            IOptions<OCRConfig> ocrOptions, 
            IOptions<GeneralConfig> generalOptions,
            IOptions<OrderConfig> orderOptions)
        {
            _hostApplicationLifetime = hostApplicationLifetime;
            _ragingBullConfig = rbOptions.Value;
            _tdAmeritradeConfig = tdOptions.Value;
            _ocrConfig = ocrOptions.Value;
            _generalConfig = generalOptions.Value;
            _orderConfig = orderOptions.Value;

            PositionDatabase lottoxDatabase = new LitePositionDatabase(_generalConfig.LottoxDatabasePath);
            LivePortfolioClient = new LottoXClient(_ragingBullConfig, _ocrConfig, lottoxDatabase);
            TDClient tdClient = new TDClient(_tdAmeritradeConfig);
            MarketDataClient = tdClient;

            if (_generalConfig.UsePaperTrade)
            {
                PositionDatabase paperDatabase = new LitePositionDatabase(_generalConfig.PaperTradeDatabasePath);
                BrokerClient = new PaperTradeBrokerClient(paperDatabase, MarketDataClient);
            }
            else
            {
                BrokerClient = tdClient; 
            }
            OrderManager = new OrderManager(BrokerClient, MarketDataClient, _orderConfig);
        }

        private IBrokerClient BrokerClient { get; }
        private LivePortfolioClient LivePortfolioClient { get; }
        private OrderManager OrderManager { get; }
        private IMarketDataClient MarketDataClient { get; }

        class DeltaList
        {
            public IList<PositionDelta> Deltas { get; set; }
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            LivePortfolioClient.Logout();
            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!MarketDataClient.IsMarketOpenToday())
            {
                Log.Information("Market closed today");
                return;
            }
            await LivePortfolioClient.Login();

            TimeSpan marketOpenTime = new TimeSpan(9, 30, 0);
            TimeSpan marketCloseTime = new TimeSpan(16, 0, 0);

            int invalidCount = 0;

            while (!stoppingToken.IsCancellationRequested)
            {
                TimeSpan now = DateTime.Now.TimeOfDay;
                if (now >= marketCloseTime)
                {
                    Log.Information("Market now closed!");
                    //break;
                }
                else if (now <= marketOpenTime)
                {
                    //Log.Warning("Market not open yet!");
                    //// Or, wait until 9:30am
                    //await Task.Delay(30*1000, stoppingToken);

                    //continue;
                }

                IList<PositionDelta> deltas = new List<PositionDelta>();

                try
                {
                    // TODO
                    //if (await LivePortfolioClient.HasPortfolioChanged())
                    //{
                    //    (livePositions, deltas) = await LivePortfolioClient.GetLivePositionsAndDeltas();
                    //}
                    deltas = await LivePortfolioClient.GetLiveDeltasFromOrders();

                    break;

                    await LivePortfolioClient.HaveOrdersChanged(deltas.Count > 0);
                    //(livePositions, deltas) = await LivePortfolioClient.GetLivePositionsAndDeltas(deltaList);

                    invalidCount = 0;
                }
                catch (InvalidPortfolioStateException ex)
                {
                    invalidCount++;

                    if (invalidCount == 2)
                    {
                        Log.Error("Portfolio found invalid {InvalidCount} times", invalidCount);
                    } else if (invalidCount > 2)
                    {
                        Log.Fatal("Portfolio found invalid {InvalidCount} times", invalidCount);
                        break;
                    }
                    await LivePortfolioClient.Login();

                    continue;
                }

                foreach (PositionDelta delta in deltas)
                {
                    Order? order = OrderManager.DecideOrder(delta);
                    if (order != null)
                    {
                        BrokerClient.PlaceOrder(order);
                    }
                }
                await Task.Delay(30*1000, stoppingToken);
            }

            _hostApplicationLifetime.StopApplication();
        }
    }
}
            //ImageConsistencyClient con = new ImageConsistencyClient();
            //con.TestAndSetCurrentImage("C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/quantitiesTest1.png");
            //con.TestAndSetCurrentImage("C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/quantitiesTest2.png");
            //return;

            //con.TestAndSetCurrentImage("C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/portfolio.png");
            //con.TestAndSetCurrentImage("C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/1.png");


            //MarketDataClient.GetQuote("AAPL_201120C115");
            //IList<Position> positions = BrokerClient.GetPositions();

            //string symbol = "SFIX_201120C35";
            //string symbol = "SFIX";
            //OptionQuote quote = MarketDataClient.GetQuote(symbol);

            //Log.Information("Quote: {@Quote}", quote);

            //try
            //{
            //    //(IList<Position> livePositions, IList<PositionDelta> deltas) = await LivePortfolioClient.GetLivePositionsAndDeltas();

            //    IList<Position> livePositions = await ((LottoXClient)LivePortfolioClient).GetPositionsFromImage(
            //        "C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/1.json",
            //        "C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/1.json");

            //    Console.WriteLine(livePositions);

            //    IList<Position> offlinePositions = await ((LottoXClient)LivePortfolioClient).GetPositionsFromImage(
            //        "C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/offline.json",
            //        "C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/offline.json");

            //    Console.WriteLine(offlinePositions);

            //} catch (InvalidPortfolioStateException ex)
            //{
            //    // TODO: Email/text notification???
            //    Console.WriteLine("Invalid portfolio state");
            //}

            //DeltaList list;
            //using (StreamReader r = new StreamReader("C:/Users/Admin/WindowsServices/MarketCode/LottoXService/deltas.json"))
            //{
            //    string json = r.ReadToEnd();
            //    list = JsonConvert.DeserializeObject<DeltaList>(json);
            //}
            //IList<PositionDelta> deltaList = list.Deltas;

            //await LivePortfolioClient.GetLivePositionsAndDeltas(deltaList);

            //return;

            //foreach (PositionDelta delta in deltaList)
            //{
            //    Order? order = OrderManager.DecideOrder(delta);
            //    if (order != null)
            //    {
            //        BrokerClient.PlaceOrder(order);
            //    }
            //}
