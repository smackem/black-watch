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
                { QuoteHistoryDownload: not null } => new QuoteDownloadJob(jobInfo.QuoteHistoryDownload, dataStore, polygon),
                { TrackerDownload: not null } => new TrackerDownloadJob(jobInfo.TrackerDownload, dataStore, polygon),
                var info when info == JobInfo.Nop => NopJob.Instance,
                _ => throw new ArgumentException($"unkown kind of job: {jobInfo}"),
            };
        }
    }
}