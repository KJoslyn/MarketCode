using Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
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
        private IBrokerClient BrokerClient { get; }
        private IPortfolioClient PortfolioClient { get; }

        public Worker(IOptions<RagingBullConfig> rbOptions, IOptions<TDAmeritradeConfig> tdOptions)
        {
            _ragingBullConfig = rbOptions.Value;
            _tdAmeritradeConfig = tdOptions.Value;

            BrokerClient = new TDClient(_tdAmeritradeConfig);
            PortfolioClient = new LottoXClient(_ragingBullConfig);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Get LottoX portfolio positions
            // Get my positions
            // Make trades

            try
            {
                PortfolioClient.GetPositions();
                //IList<Position> positions = BrokerClient.GetPositions();
            }
            catch (Exception ex)
            {

            }
            finally
            {
                await PortfolioClient.Logout();
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
