using System;
using BlackWatch.Core.Contracts;
using BlackWatch.Daemon.RequestEngine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BlackWatch.Daemon.Features.Requests
{
    public class PolygonRequestRunner : RequestRunner
    {
        public PolygonRequestRunner(
            ILogger<PolygonRequestRunner> logger,
            IDataStore dataStore,
            IRequestFactory requestFactory,
            IOptions<PolygonRequestRunnerOptions> options,
            IServiceProvider sp)
            : base(TimeSpan.FromMinutes(1), logger, dataStore, requestFactory, options, sp)
        {
        }
    }
}