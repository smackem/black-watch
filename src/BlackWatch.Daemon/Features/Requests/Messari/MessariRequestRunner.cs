using System;
using BlackWatch.Core.Contracts;
using BlackWatch.Daemon.RequestEngine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BlackWatch.Daemon.Features.Requests.Messari;

public class MessariRequestRunner : RequestRunner
{
    public MessariRequestRunner(
        ILogger<MessariRequestRunner> logger,
        IRequestQueue requestQueue,
        IRequestFactory requestFactory,
        IOptions<MessariRequestRunnerOptions> options,
        IServiceProvider sp)
        : base(TimeSpan.FromMinutes(1), ApiTags.Messari, logger, requestQueue, requestFactory, options, sp)
    {}
}
