using AzureOCR;
using Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.IO;
using TDAmeritrade;

namespace LottoXService
{
    public class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, true)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            try
            {
                Log.Information("LottoX Application Starting Up");
                CreateHostBuilder(args, configuration).Build().Run(); // TODO: RunAsync?
            }
            catch (Exception ex)
            {
                // TODO: This will actually catch any error thrown in the worker. Need to read about services and how they work.
                Log.Fatal(ex, "The application failed to start correctly."); // This message is misleading.
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args, IConfigurationRoot configuration)
        {
            return Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .UseSerilog()
                .ConfigureServices((hostcontext, services) =>
                {
                    services.AddHostedService<Worker>()
                        .AddOptions()
                        .Configure<RagingBullConfig>(configuration.GetSection("RagingBull"))
                        .Configure<TDAmeritradeConfig>(configuration.GetSection("TDAmeritrade"))
                        .Configure<OCRConfig>(configuration.GetSection("AzureOCR"))
                        .Configure<GeneralConfig>(configuration.GetSection("General"))
                        .Configure<OrderConfig>(configuration.GetSection("OrderSettings"));
                });
        }
    }
}
