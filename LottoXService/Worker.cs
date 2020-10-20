using Core;
using Core.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TDAmeritrade;

namespace LottoXService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private IBrokerClient Client { get; }

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            Client = new TDClient(Config.RothAcctNumber);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            IList<Position> positions = Client.GetPositions();

            while (!stoppingToken.IsCancellationRequested)
            {
                //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
