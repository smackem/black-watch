using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Daemon.Features.Polygon;
using BlackWatch.Daemon.RequestEngine;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon.Features.Jobs
{
    internal class TrackerRequest : Request
    {
        private readonly TrackerRequestInfo _info;
        private readonly IPolygonApiClient _polygon;
        private readonly IDataStore _dataStore;

        public TrackerRequest(TrackerRequestInfo info, IDataStore dataStore, IPolygonApiClient polygon)
            : base("download crypto trackers")
        {
            _info = info;
            _dataStore = dataStore;
            _polygon = polygon;
        }

        public override async Task<RequestResult> ExecuteAsync(RequestContext ctx)
        {
            GroupedDailyCurrencyPricesResponse trackerPrices;
            try
            {
                trackerPrices = await _polygon.GetGroupedDailyCryptoPricesAsync(_info.Date);
            }
            catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.TooManyRequests)
            {
                ctx.Logger.LogWarning(e, "received {StatusCode} while getting trackers => wait and retry", e.StatusCode);
                return RequestResult.WaitAndRetry;
            }
            catch (Exception e)
            {
                ctx.Logger.LogError(e, "error getting grouped daily crypto prices");
                return RequestResult.Fatal;
            }

            ctx.Logger.LogDebug("{Response}", trackerPrices);

            if (trackerPrices.Status != PolygonApiStatus.Ok)
            {
                ctx.Logger.LogWarning("aggregate crypto prices: got non-ok status: {Response}", trackerPrices);
            }

            if (trackerPrices.Results == null)
            {
                ctx.Logger.LogWarning("grouped daily crypto prices: got empty result set: {Response}", trackerPrices);
                return RequestResult.Retry;
            }

            var trackers = trackerPrices.Results
                .Select(tp => new Tracker(tp.Symbol, null, null))
                .ToArray();

            await _dataStore.PutTrackersAsync(trackers);
            return RequestResult.Ok;
        }
    }
}
