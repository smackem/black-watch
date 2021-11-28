using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Util;
using BlackWatch.Daemon.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BlackWatch.Daemon.JobEngine
{
    /// <summary>
    /// worker that performs throttled job execution, executing a configurable number of jobs per minute
    /// </summary>
    public class JobExecutor : WorkerBase
    {
        private readonly ILogger<JobExecutor> _logger;
        private readonly IDataStore _dataStore;
        private readonly JobExecutorOptions _config;
        private readonly IJobFactory _jobFactory;
        private readonly IServiceProvider _sp;

        public JobExecutor(
            ILogger<JobExecutor> logger,
            IDataStore dataStore,
            IJobFactory jobFactory,
            IOptions<JobExecutorOptions> options,
            IServiceProvider sp)
            : base(logger)
        {
            _logger = logger;
            _dataStore = dataStore;
            _jobFactory = jobFactory;
            _config = options.Value;
            _sp = sp;
        }

        protected override async Task ExecuteOverrideAsync(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromMinutes(1);

            while (stoppingToken.IsCancellationRequested == false)
            {
                var jobInfos = await _dataStore.DequeueJobsAsync(_config.MaxJobsPerMinute).Linger();
                var jobs = jobInfos.Select(info => (_jobFactory.BuildJob(info, _sp), info));
                var ctx = new JobExecutionContext(_logger)
                {
                    StoppingToken = stoppingToken,
                };

                foreach (var (job, jobInfo) in jobs)
                {
                    var result = await job.ExecuteAsync(ctx);

                    _logger.Log(GetLogLevel(result), "job {Job} executed with result: {JobExecutionResult}", job, result);

                    // ReSharper disable once ConvertIfStatementToSwitchStatement
                    if (result == JobExecutionResult.WaitAndRetry)
                    {
                        await _dataStore.EnqueueJobAsync(jobInfo).Linger();
                        await Task.Delay(interval, stoppingToken).Linger();
                    }
                    else if (result == JobExecutionResult.Retry)
                    {
                        await _dataStore.EnqueueJobAsync(jobInfo).Linger();
                    }
                }

                await Task.Delay(interval, stoppingToken).Linger();
            }

            _logger.LogInformation("job execution stopped");
        }

        private static LogLevel GetLogLevel(JobExecutionResult result)
        {
            return result switch
            {
                JobExecutionResult.Ok => LogLevel.Information,
                JobExecutionResult.Retry or JobExecutionResult.WaitAndRetry => LogLevel.Warning,
                JobExecutionResult.Fatal => LogLevel.Error,
                _ => throw new ArgumentOutOfRangeException($"unknown execution result: {result}"),
            };
        }
    }
}
