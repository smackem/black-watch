using System;
using BlackWatch.Core.Contracts;
using BlackWatch.Daemon.Features.MessariApi;
using BlackWatch.Daemon.Features.PolygonApi;
using BlackWatch.Daemon.Features.Requests.Messari;
using BlackWatch.Daemon.Features.Requests.Polygon;
using BlackWatch.Daemon.RequestEngine;
using Microsoft.Extensions.DependencyInjection;

namespace BlackWatch.Daemon.Features.Requests;

public class RequestFactory : IRequestFactory
{
    public Request BuildRequest(RequestInfo requestInfo, IServiceProvider sp)
    {
        var quoteStore = sp.GetRequiredService<IQuoteStore>();
        var requestQueue = sp.GetRequiredService<IRequestQueue>();
        var polygon = sp.GetRequiredService<IPolygonApiClient>();
        var messari = sp.GetRequiredService<IMessariApiClient>();

        return requestInfo switch
        {
            { QuoteHistoryDownload: not null } => new QuoteHistoryRequest(requestInfo.QuoteHistoryDownload, quoteStore, polygon),
            { TrackerDownload: not null } => new TrackerRequest(requestInfo.TrackerDownload, quoteStore, requestQueue, polygon),
            { QuoteSnapshotDownload: not null } => new QuoteSnapshotRequest(requestInfo.QuoteSnapshotDownload, quoteStore, messari),
            var info when info == RequestInfo.Nop => NopRequest.Instance,
            _ => throw new ArgumentException($"unknown kind of job: {requestInfo}")
        };
    }
}
