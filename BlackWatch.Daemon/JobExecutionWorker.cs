using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Daemon.Features.Polygon;
using BlackWatch.Daemon.Jobs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon
{
    public class JobExecutionWorker : WorkerBase
    {
        private readonly ILogger<JobExecutionWorker> _logger;
        private readonly JobQueue _jobQueue;

        public JobExecutionWorker(ILogger<JobExecutionWorker> logger, JobQueue jobQueue)
        : base(logger)
        {
            _logger = logger;
            _jobQueue = jobQueue;
        }

        protected override async Task ExecuteOverrideAsync(CancellationToken stoppingToken)
        {
            try
            {
                await RunJobQueue(_jobQueue.DequeueAsync(stoppingToken), stoppingToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "error caught at top level");
                throw;
            }

            _logger.LogInformation("work done");
        }

        private async Task RunJobQueue(IAsyncEnumerable<Job> jobs, CancellationToken ct)
        {
            await foreach (var job in jobs.WithCancellation(ct))
            {
                var ctx = new JobExecutionContext
                {
                    StoppingToken = ct,
                };

                var result = await job.ExecuteAsync(ctx);

                var logLevel = result switch
                {
                    JobExecutionResult.Ok => LogLevel.Information,
                    JobExecutionResult.Retry => LogLevel.Warning,
                    JobExecutionResult.Fatal => LogLevel.Error,
                    _ => throw new ArgumentOutOfRangeException($"unknown execution result: {result}"),
                };

                _logger.Log(logLevel, "job {Job} executed with result: {JobExecutionResult}", job, result);

                if (result == JobExecutionResult.Retry)
                {
                    await _jobQueue.EnqueueAsync(job, ct);
                }
            }
        }
    }
}
