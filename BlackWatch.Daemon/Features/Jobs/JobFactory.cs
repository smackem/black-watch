using System;
using BlackWatch.Core.Contracts;
using BlackWatch.Daemon.Features.Polygon;
using BlackWatch.Daemon.JobEngine;
using Microsoft.Extensions.DependencyInjection;

namespace BlackWatch.Daemon.Features.Jobs
{
    public class JobFactory : IJobFactory
    {
        public Job BuildJob(JobInfo jobInfo, IServiceProvider sp)
        {
            var dataStore = sp.GetRequiredService<IDataStore>();
            var polygon = sp.GetRequiredService<IPolygonApiClient>();

            return jobInfo switch
            {
                { AggregateCrypto: not null } => new QuoteDownloadJob(jobInfo.AggregateCrypto, dataStore, polygon),
                { DailyGroupedCrypto: not null } => new TrackerDownloadJob(jobInfo.DailyGroupedCrypto, dataStore, polygon),
                var info when info == JobInfo.Nop => NopJob.Instance,
                _ => throw new ArgumentException($"unkown kind of job: {jobInfo}"),
            };
        }
    }
}