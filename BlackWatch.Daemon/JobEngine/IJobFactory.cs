using System;
using BlackWatch.Core.Contracts;

namespace BlackWatch.Daemon.JobEngine
{
    public interface IJobFactory
    {
        Job BuildJob(JobInfo jobInfo, IServiceProvider sp);
    }
}