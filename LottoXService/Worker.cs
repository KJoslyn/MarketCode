using AzureOCR;
using Core;
using Core.Exceptions;
using Core.Model;
using Core.Model.Constants;
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

            TDClient tdClient = new TDClient(_tdAmeritradeConfig);
            MarketDataClient = tdClient;
            PortfolioDatabase lottoxDatabase = new LitePortfolioDatabase(_generalConfig.LottoxDatabasePath, _generalConfig.SymbolsDatabasePath);
            LivePortfolioClient = new LottoXClient(_ragingBullConfig, _ocrConfig, lottoxDatabase, MarketDataClient);

            if (_generalConfig.UsePaperTrade)
            {
                Log.Information("PAPER TRADING");
                PortfolioDatabase paperDatabase = new LitePortfolioDatabase(_generalConfig.PaperTradeDatabasePath, _generalConfig.PaperSymbolsDatabasePath);
                BrokerClient = new PaperTradeBrokerClient(paperDatabase, MarketDataClient);
            }
            else
            {
                Log.Information("*****Trading with real account");
                BrokerClient = tdClient; 
            }
            OrderManager = new OrderManager(BrokerClient, MarketDataClient, _orderConfig);
        }

        private IBrokerClient BrokerClient { get; }
        private LivePortfolioClient LivePortfolioClient { get; }
        private OrderManager OrderManager { get; }
        private MarketDataClient MarketDataClient { get; }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            LivePortfolioClient.Logout();
            LivePortfolioClient.Database.Dispose();
            if (BrokerClient is PaperTradeBrokerClient)
            {
                ((PaperTradeBrokerClient)BrokerClient).Database.Dispose();
            }
            
            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //TimeSortedCollection<FilledOrder> liveOrders = new TimeSortedCollection<FilledOrder>();
            //liveOrders.Add(new FilledOrder("NIO_201204C55", (float)0.5, "BUY_TO_OPEN", "LIMIT", (float)0.5, 50, new DateTime(2020, 12, 1, 12, 25, 33)));
            //liveOrders.Add(new FilledOrder("NIO_201204C60", (float)0.5, "BUY_TO_OPEN", "LIMIT", (float)0.5, 50, new DateTime(2020, 12, 1, 12, 22, 30)));
            //liveOrders.Add(new FilledOrder("NIO_201204C62", (float)0.5, "BUY_TO_OPEN", "LIMIT", (float)0.5, 50, new DateTime(2020, 12, 1, 12, 22, 30)));
            //liveOrders.Add(new FilledOrder("NIO_201204C65", (float)0.5, "BUY_TO_OPEN", "LIMIT", (float)0.5, 50, new DateTime(2020, 12, 1, 12, 30, 00)));
            //LivePortfolioClient.IdentifyNewAndUpdatedOrders(liveOrders, 260);

            //IEnumerable<Position> positions = BrokerClient.GetPositions();
            //OptionQuote quote;
            //try
            //{
            //    quote = MarketDataClient.GetOptionQuote("AAPL_201218C140");
            //    //OptionQuote quote = MarketDataClient.GetQuote("AAPL_121819C140");
            //}
            //catch (Exception ex)
            //{
            //    Log.Information(ex, "hello");
            //}
            //Order o1 = new Order("AAPL_201218C150", 1, InstructionType.SELL_TO_CLOSE, OrderType.MARKET, (float)0.08);
            //BrokerClient.PlaceOrder(o1);
            //Log.Information("Placing Order: {@Order}", o1);

            //IList<Position> positions = await ((LottoXClient)LivePortfolioClient).GetPositionsFromImage("C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/portfolio-4380.png");

            //Order o1 = new Order("AAPL_201218C150", 1, InstructionType.BUY_TO_OPEN, OrderType.LIMIT, (float).03);
            //BrokerClient.PlaceOrder(o1);

            BrokerClient.GetPositions();

            Log.Information("RETURNING EARLY");
            return;

            if (!MarketDataClient.IsMarketOpenToday())
            {
                Log.Information("Market closed today");
                return;
            }
            //Log.Information("NOT LOGGING IN");
            await LivePortfolioClient.Login();

            TimeSpan marketOpenTime = new TimeSpan(9, 30, 0);
            TimeSpan marketCloseTime = new TimeSpan(16, 0, 0);

            int invalidPortfolioStateCount = 0;
            int errorCount = 0;

            //string seedOrdersFilename = "C:/Users/Admin/WindowsServices/MarketCode/LottoXService/screenshots/orders-4173.png";
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
                    Log.Information("Market not open yet!");
                    // Or, wait until 9:30am
                    await Task.Delay(30 * 1000, stoppingToken);

                    continue;
                }

                try
                {
                    TimeSortedCollection<PositionDelta> deltas;

                    if (seedOrdersFilename.Length > 0)
                    {
                        Log.Information("*********Seeding live orders with file " + seedOrdersFilename);
                        deltas = await LivePortfolioClient.GetLiveDeltasFromPositions(seedOrdersFilename);
                        seedOrdersFilename = "";
                    }
                    else if (errorCount > 0
                        || invalidPortfolioStateCount > 0)
                    {
                        Log.Information("***Getting live deltas after error");
                        deltas = await LivePortfolioClient.GetLiveDeltasFromPositions();
                    }
                    else if (await LivePortfolioClient.HaveOrdersChanged(null))
                    {
                        Log.Information("***Change in top orders detected- getting live orders");
                        deltas = await LivePortfolioClient.GetLiveDeltasFromPositions();
                    }
                    else
                    {
                        deltas = new TimeSortedCollection<PositionDelta>();
                    }

                    foreach(PositionDelta delta in deltas)
                    {
                        Order? order = OrderManager.DecideOrder(delta);
                        if (order != null)
                        {
                            BrokerClient.PlaceOrder(order);
                        }
                    }

                    invalidPortfolioStateCount = 0;
                    errorCount = 0;
                }
                catch (InvalidPortfolioStateException)
                {
                    invalidPortfolioStateCount++;

                    if (invalidPortfolioStateCount == 2)
                    {
                        Log.Error("Portfolio found invalid {InvalidCount} times", invalidPortfolioStateCount);
                    } else if (invalidPortfolioStateCount > 2)
                    {
                        Log.Fatal("Portfolio found invalid {InvalidCount} times", invalidPortfolioStateCount);
                        break;
                    }
                    await LivePortfolioClient.Login();

                    continue;
                }
                catch (PortfolioDatabaseException)
                {
                    //Assume the exception is already logged
                    break;
                }
                catch (OptionParsingException ex)
                {
                    Log.Fatal(ex, "Error parsing option symbol. Symbol {Symbol}. Terminating program.", ex.Symbol);
                    break;
                }
                catch (ArgumentException ex)
                {
                    Log.Fatal(ex, "Arument exception encountered- terminating program.");
                    break;
                }
                catch (ModelBuilderException ex)
                {
                    Log.Error(ex, "ModelBuilderException encountered");
                    errorCount++;

                    if (errorCount > 2)
                    {
                        Log.Fatal(ex, "Too many consecutive errors encountered. Terminating program. Error count = {ErrorCount}", errorCount);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;

                    if (errorCount <= 2)
                    {
                        Log.Error(ex, "Unexpected error: count = {ErrorCount}", errorCount);
                    } 
                    else if (errorCount > 2)
                    {
                        Log.Fatal(ex, "Too many consecutive errors encountered. Terminating program. Error count = {ErrorCount}", errorCount);
                        break;
                    }
                }

                await Task.Delay(15*1000, stoppingToken);
            }

            _hostApplicationLifetime.StopApplication();
        }
    }
}
