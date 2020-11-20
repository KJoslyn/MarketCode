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
using System.Linq;
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
            //Log.Information("NOT LOGGING IN");
            await LivePortfolioClient.Login();

            TimeSpan marketOpenTime = new TimeSpan(9, 30, 0);
            TimeSpan marketCloseTime = new TimeSpan(16, 0, 0);

            int invalidCount = 0;
            int unexpectedErrorCount = 0;
            int lowConfidenceCount = 0;

            // TODO: Remove lastTopOrderDateTime
            string lastTopOrderDateTime = "";

            //string seedOrdersFilename = "C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/orders-3941.png";
            string seedOrdersFilename = "";
            while (!stoppingToken.IsCancellationRequested)
            {
                TimeSpan now = DateTime.Now.TimeOfDay;
                if (now >= marketCloseTime)
                {
                    Log.Information("Market now closed!");
                    //Log.Information("NOT BREAKING ON MARKET CLOSED");
                    break;
                }
                else if (now <= marketOpenTime)
                {
                    //Log.Warning("Market not open yet!");
                    //// Or, wait until 9:30am
                    //await Task.Delay(30*1000, stoppingToken);

                    //continue;
                }

                TimeSortedSet<PositionDelta> deltas = new TimeSortedSet<PositionDelta>();
                bool lastParseWasLowConfidence = false;

                try
                {
                    if (seedOrdersFilename.Length > 0 || lastParseWasLowConfidence)
                    {
                        Log.Information("*********Seeding live orders with file " + seedOrdersFilename);
                        (lastParseWasLowConfidence, deltas) = await LivePortfolioClient.GetLiveDeltasFromOrders(seedOrdersFilename);
                        seedOrdersFilename = "";
                    }
                    else if (await LivePortfolioClient.HaveOrdersChanged(null))
                    {
                        Log.Information("***Change in top orders detected- getting live orders");
                        (lastParseWasLowConfidence, deltas) = await LivePortfolioClient.GetLiveDeltasFromOrders();
                    }

                    if (lastParseWasLowConfidence)
                    {
                        lowConfidenceCount++;
                        if (lowConfidenceCount > 2)
                        {
                            LowConfidenceParsingException ex = new LowConfidenceParsingException("Max number of low confidence parse attempts reached");
                            throw ex;
                        }
                    }
                    else
                    {
                        lowConfidenceCount = 0;
                    }

                    //deltas = await LivePortfolioClient.GetLiveDeltasFromOrders();


                    //string topOrderDateTime;
                    //(topOrderDateTime, deltas) = await LivePortfolioClient.GetLiveDeltasFromOrders();
                    //await LivePortfolioClient.HaveOrdersChanged(topOrderDateTime != lastTopOrderDateTime);
                    //lastTopOrderDateTime = topOrderDateTime;

                    //await LivePortfolioClient.HaveOrdersChanged(true);


                    //(livePositions, deltas) = await LivePortfolioClient.GetLivePositionsAndDeltas(deltaList);

                    IEnumerable<Order> orders = OrderManager.DecideOrdersTimeSorted(deltas);

                    foreach (Order order in orders)
                    {
                        BrokerClient.PlaceOrder(order);
                    }

                    invalidCount = 0;
                    unexpectedErrorCount = 0;
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
                catch (PositionDatabaseException ex)
                {
                    //Assume the exception is already logged
                    break;
                }
                catch (LowConfidenceParsingException ex)
                {
                    Log.Fatal(ex, "Max number of low confidence parsing attempts reached");
                    break;
                }
                catch (Exception ex)
                {
                    unexpectedErrorCount++;

                    if (unexpectedErrorCount < 2)
                    {
                        Log.Error(ex, "Unexpected error: count = {ErrorCount}", unexpectedErrorCount);
                    } else if (unexpectedErrorCount > 2)
                    {
                        Log.Fatal(ex, "Unexpected error: count = {ErrorCount}", unexpectedErrorCount);
                        break;
                    }
                }

                await Task.Delay(15*1000, stoppingToken);
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
