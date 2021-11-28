using System;
using BlackWatch.Core.Contracts;

namespace BlackWatch.Daemon.JobEngine
{
    /// <summary>
    /// creates <see cref="Job"/> instances from <see cref="JobInfo"/>s retrieved from other services
    /// </summary>
    public interface IJobFactory
    {
        Job BuildJob(JobInfo jobInfo, IServiceProvider sp);
    }
}