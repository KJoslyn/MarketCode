using Core;
using Core.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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
            while (!stoppingToken.IsCancellationRequested)
            {
                Client.GetPositions();
                //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
