using System.Threading;

namespace BlackWatch.Daemon.Jobs
{
    public class JobExecutionContext
    {
        public CancellationToken StoppingToken { get; init; }
    }
}
