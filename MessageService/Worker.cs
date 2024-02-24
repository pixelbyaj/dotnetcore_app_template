using StartupServices.Interface;
using StartupServices.Processors;
using System.Data;

namespace StartupServices
{
    public class Worker : BackgroundService
    {
        #region private instance variable
        private readonly ILogger<Worker> _logger;
        #endregion

        #region ctor
        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }
        #endregion

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Service started");
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(Int32.MaxValue, stoppingToken);
            }
        }
    }
}