using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Daemon.Features.MessariApi;
using BlackWatch.Daemon.RequestEngine;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon.Features.Requests.Messari
{
    public class QuoteSnapshotRequest : Request
    {
        private readonly QuoteSnapshotRequestInfo _info;
        private readonly IMessariApiClient _messari;
        private readonly IDataStore _dataStore;

        public QuoteSnapshotRequest(
            QuoteSnapshotRequestInfo info,
            IDataStore dataStore,
            IMessariApiClient messari)
            : base("download quote snapshots")
        {
            _info = info;
            _messari = messari;
            _dataStore = dataStore;
        }

        public override async Task<RequestResult> ExecuteAsync(RequestContext ctx)
        {
            AssetListResponse assets;
            try
            {
                assets = await _messari.GetAssetsAsync();
            }
            catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.TooManyRequests)
            {
                ctx.Logger.LogWarning(e, "received {StatusCode} while getting assets => wait and retry", e.StatusCode);
                return RequestResult.WaitAndRetry;
            }
            catch (Exception e)
            {
                ctx.Logger.LogError(e, "error getting hourly quote assets");
                return RequestResult.Fatal;
            }

            var quotes = assets.Data
                .Where(asset =>
                    string.IsNullOrWhiteSpace(asset.Symbol) == false && asset.Metrics?.MarketData?.LastHour != null)
                .Select(asset => new Quote(
                    Symbol: asset.Symbol,
                    Open: asset.Metrics!.MarketData!.LastHour!.Open,
                    High: asset.Metrics.MarketData.LastHour.High,
                    Low: asset.Metrics.MarketData.LastHour.Low,
                    Close: asset.Metrics.MarketData.LastHour.Close,
                    Currency: "USD",
                    Date: assets.Status.Timestamp));

            foreach (var quote in quotes)
            {
                await _dataStore.PutHourlyQuoteAsync(quote);
            }

            return RequestResult.Ok;
        }
    }
}
