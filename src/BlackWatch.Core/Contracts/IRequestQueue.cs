using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlackWatch.Core.Contracts;

public interface IRequestQueue
{
    /// <summary>
    /// inserts the specified <see cref="RequestInfo" />s in the request queues responsible for the request's
    /// api tags
    /// </summary>
    public Task EnqueueRequestsAsync(IEnumerable<RequestInfo> jobs);

    /// <summary>
    /// inserts the specified <see cref="RequestInfo" />s at the end of the job queue responsible for the
    /// request's api tag
    /// </summary>
    public Task EnqueueRequestAsync(RequestInfo request);

    /// <summary>
    /// gets and removes up to <paramref name="count"/> jobs from the head of the request queue responsible
    /// for the given <paramref name="apiTag"/>
    /// </summary>
    public Task<RequestInfo[]> DequeueRequestsAsync(int count, string apiTag);

    /// <summary>
    /// gets the current request queue length for the given <paramref name="apiTag"/>
    /// </summary>
    public Task<long> GetRequestQueueLengthAsync(string apiTag);
}
