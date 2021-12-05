using System;
using BlackWatch.Core.Contracts;
using BlackWatch.Daemon.Features.Polygon;
using BlackWatch.Daemon.RequestEngine;
using Microsoft.Extensions.DependencyInjection;

namespace BlackWatch.Daemon.Features.Requests
{
    public class RequestFactory : IRequestFactory
    {
        public Request BuildRequest(RequestInfo requestInfo, IServiceProvider sp)
        {
            var dataStore = sp.GetRequiredService<IDataStore>();
            var polygon = sp.GetRequiredService<IPolygonApiClient>();

            return requestInfo switch
            {
                { QuoteHistoryDownload: not null } => new QuoteHistoryRequest(requestInfo.QuoteHistoryDownload, dataStore, polygon),
                { TrackerDownload: not null } => new TrackerRequest(requestInfo.TrackerDownload, dataStore, polygon),
                var info when info == RequestInfo.Nop => NopRequest.Instance,
                _ => throw new ArgumentException($"unknown kind of job: {requestInfo}"),
            };
        }
    }
}