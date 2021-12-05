using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Util;
using BlackWatch.Daemon.Features;
using BlackWatch.Daemon.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BlackWatch.Daemon.RequestEngine
{
    /// <summary>
    /// worker that performs throttled job execution, executing a configurable number of jobs per minute
    /// </summary>
    public abstract class RequestRunner : WorkerBase
    {
        private readonly ILogger<RequestRunner> _logger;
        private readonly IDataStore _dataStore;
        private readonly RequestRunnerOptions _config;
        private readonly IRequestFactory _requestFactory;
        private readonly IServiceProvider _sp;
        private readonly TimeSpan _interval;
        private readonly string _apiTag;

        protected RequestRunner(
            TimeSpan interval,
            string apiTag,
            ILogger<RequestRunner> logger,
            IDataStore dataStore,
            IRequestFactory requestFactory,
            IOptions<RequestRunnerOptions> options,
            IServiceProvider sp)
            : base(logger)
        {
            _interval = interval;
            _apiTag = apiTag;
            _logger = logger;
            _dataStore = dataStore;
            _requestFactory = requestFactory;
            _config = options.Value;
            _sp = sp;
        }

        protected sealed override async Task ExecuteOverrideAsync(CancellationToken stoppingToken)
        {
            while (stoppingToken.IsCancellationRequested == false)
            {
                var jobInfos = await _dataStore.DequeueRequestsAsync(_config.MaxRequestsPerMinute, _apiTag).Linger();
                var jobs = jobInfos.Select(info => (_requestFactory.BuildRequest(info, _sp), info));
                var ctx = new RequestContext(_logger)
                {
                    StoppingToken = stoppingToken,
                };

                foreach (var (job, jobInfo) in jobs)
                {
                    var result = await job.ExecuteAsync(ctx);

                    _logger.Log(GetLogLevel(result), "job {Job} executed with result: {JobExecutionResult}", job, result);

                    // ReSharper disable once ConvertIfStatementToSwitchStatement
                    if (result == RequestResult.WaitAndRetry)
                    {
                        await _dataStore.EnqueueRequestAsync(jobInfo).Linger();
                        await Task.Delay(_interval, stoppingToken).Linger();
                    }
                    else if (result == RequestResult.Retry)
                    {
                        await _dataStore.EnqueueRequestAsync(jobInfo).Linger();
                    }
                }

                await Task.Delay(_interval, stoppingToken).Linger();
            }

            _logger.LogInformation("job execution stopped");
        }

        private static LogLevel GetLogLevel(RequestResult result)
        {
            return result switch
            {
                RequestResult.Ok => LogLevel.Information,
                RequestResult.Retry or RequestResult.WaitAndRetry => LogLevel.Warning,
                RequestResult.Fatal => LogLevel.Error,
                _ => throw new ArgumentOutOfRangeException($"unknown execution result: {result}"),
            };
        }
    }
}
