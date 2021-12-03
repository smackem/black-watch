using System.Threading;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon.RequestEngine
{
    public class RequestContext
    {
        public RequestContext(ILogger logger)
        {
            Logger = logger;
        }

        public CancellationToken StoppingToken { get; init; }

        public ILogger Logger { get; }
    }
}
