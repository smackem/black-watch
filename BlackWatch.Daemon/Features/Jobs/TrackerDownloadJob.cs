using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Daemon.Features.Polygon;
using BlackWatch.Daemon.JobEngine;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon.Features.Jobs
{
    internal class TrackerDownloadJob : Job
    {
        private readonly Core.Contracts.TrackerDownloadJob _info;
        private readonly IPolygonApiClient _polygon;
        private readonly IDataStore _dataStore;

        public TrackerDownloadJob(Core.Contracts.TrackerDownloadJob info, IDataStore dataStore, IPolygonApiClient polygon)
            : base("download crypto trackers")
        {
            _info = info;
            _dataStore = dataStore;
            _polygon = polygon;
        }

        public override async Task<JobExecutionResult> ExecuteAsync(JobExecutionContext ctx)
        {
            GroupedDailyCurrencyPricesResponse trackerPrices;
            try
            {
                trackerPrices = await _polygon.GetGroupedDailyCryptoPricesAsync(_info.Date);
            }
            catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.TooManyRequests)
            {
                ctx.Logger.LogWarning(e, "received {StatusCode} while getting trackers => wait and retry", e.StatusCode);
                return JobExecutionResult.WaitAndRetry;
            }
            catch (Exception e)
            {
                ctx.Logger.LogError(e, "error getting grouped daily crypto prices");
                return JobExecutionResult.Fatal;
            }

            ctx.Logger.LogDebug("{Response}", trackerPrices);

            if (trackerPrices.Status != PolygonApiStatus.Ok)
            {
                ctx.Logger.LogWarning("aggregate crypto prices: got non-ok status: {Response}", trackerPrices);
            }

            if (trackerPrices.Results == null)
            {
                ctx.Logger.LogWarning("grouped daily crypto prices: got empty result set: {Response}", trackerPrices);
                return JobExecutionResult.Retry;
            }

            var trackers = trackerPrices.Results
                .Select(tp => new Tracker(tp.Symbol, null, null))
                .ToArray();

            await _dataStore.PutTrackersAsync(trackers);
            return JobExecutionResult.Ok;
        }
    }
}
