using System.Threading;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon.Jobs
{
    public class JobExecutionContext
    {
        public JobExecutionContext(ILogger logger)
        {
            Logger = logger;
        }

        public CancellationToken StoppingToken { get; init; }

        public ILogger Logger { get; }
    }
}
