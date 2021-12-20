using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Util;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Core.Services;

/// <summary>
/// implements a <see cref="IRequestQueue"/> backed by the redis persistent cache 
/// </summary>
public class RedisRequestQueue : RedisStore, IRequestQueue
{
    private readonly ILogger<RedisRequestQueue> _logger;
    public RedisRequestQueue(RedisConnection connection, ILogger<RedisRequestQueue> logger)
        : base(connection)
    {
        _logger = logger;
    }
    
    public async Task EnqueueRequestsAsync(IEnumerable<RequestInfo> requests)
    {
        var db = await GetDatabaseAsync().Linger();
        var groups = requests.GroupBy(request => request.ApiTag);

        foreach (var group in groups)
        {
            if (group.Key == null)
            {
                throw new ArgumentException("requests without api tag found", nameof(requests));
            }

            var values = group
                .Select(Serialize)
                .ToArray();

            var count = await db.ListRightPushAsync(RedisNames.Requests(group.Key), values).Linger();
            _logger.LogDebug(
                "enqueued {EnqueuedJobs} requests for {ApiTag} => total queue length = {JobQueueLength}",
                values.Length, group.Key, count);
        }
    }

    public Task EnqueueRequestAsync(RequestInfo request)
    {
        return EnqueueRequestsAsync(new[] { request });
    }

    public async Task<RequestInfo[]> DequeueRequestsAsync(int count, string apiTag)
    {
        var db = await GetDatabaseAsync().Linger();
        var values = await db.ListLeftPopAsync(RedisNames.Requests(apiTag), count).Linger();
        var result = values
            .Where(v => v.HasValue)
            .Select(Deserialize<RequestInfo>)
            .ToArray();
        _logger.LogDebug("dequeued {DequeuedJobs}/{EnquiredJobs} jobs", result.Length, count);
        return result;
    }

    public async Task<long> GetRequestQueueLengthAsync(string apiTag)
    {
        var db = await GetDatabaseAsync().Linger();
        return await db.ListLengthAsync(RedisNames.Requests(apiTag)).Linger();
    }
}
