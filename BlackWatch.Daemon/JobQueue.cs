using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using BlackWatch.Core.Util;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon
{
    public class JobQueue
    {
        private readonly ThrottlingQueue<Job> _queue;
        private readonly ILogger<JobQueue> _logger;

        public JobQueue(int maxJobsPerMinute, ILogger<JobQueue> logger)
        {
            _queue = new ThrottlingQueue<Job>(TimeSpan.FromMinutes(1), maxJobsPerMinute);
            _logger = logger;
        }

        public async IAsyncEnumerable<Job> DequeueAsync([EnumeratorCancellation] CancellationToken ct)
        {
            await foreach (var job in _queue.DequeueAllAsync(ct))
            {
                _logger.LogDebug("dequeued job: {Job}", job);
                yield return job;
            }
        }

        public async Task EnqueueAsync(Job job, CancellationToken ct)
        {
            _logger.LogDebug("enqueuing job: {Job}", job);
            if (await _queue.EnqueueAsync(job, ct) == false)
            {
                _logger.LogWarning("failed to enqueue job: {Job}", job);
            }
        }
    }
}
