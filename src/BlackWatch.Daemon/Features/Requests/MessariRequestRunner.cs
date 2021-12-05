using System;
using BlackWatch.Core.Contracts;
using BlackWatch.Daemon.RequestEngine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BlackWatch.Daemon.Features.Requests
{
    public class MessariRequestRunner : RequestRunner
    {
        public MessariRequestRunner(
            ILogger<MessariRequestRunner> logger,
            IDataStore dataStore,
            IRequestFactory requestFactory,
            IOptions<MessariRequestRunnerOptions> options,
            IServiceProvider sp)
            : base(TimeSpan.FromMinutes(1), ApiTags.Messari, logger, dataStore, requestFactory, options, sp)
        {
        }
    }
}
