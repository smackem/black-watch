using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using BlackWatch.Core.Util;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon.JobEngine
{
    public class InMemoryJobQueue
    {
        private readonly ThrottlingQueue<Job> _queue;
        private readonly ILogger<InMemoryJobQueue> _logger;
        private int _queueSize;

        public InMemoryJobQueue(int maxJobsPerMinute, ILogger<InMemoryJobQueue> logger)
        {
            _queue = new ThrottlingQueue<Job>(TimeSpan.FromSeconds(65), maxJobsPerMinute);
            _logger = logger;
        }

        public async IAsyncEnumerable<Job> DequeueAsync([EnumeratorCancellation] CancellationToken ct)
        {
            await foreach (var job in _queue.DequeueAllAsync(ct))
            {
                Interlocked.Decrement(ref _queueSize);
                _logger.LogDebug("dequeued job: {Job} => queue size is {QueueSize}", job, _queueSize);
                yield return job;
            }
        }

        public async Task EnqueueAsync(Job job, CancellationToken ct)
        {
            if (await _queue.EnqueueAsync(job, ct))
            {
                Interlocked.Increment(ref _queueSize);
                _logger.LogDebug("enqueued job: {Job} => queue size is {QueueSize}", job, _queueSize);
            }
            else
            {
                _logger.LogWarning("failed to enqueue job: {Job}", job);
            }
        }
    }
}
