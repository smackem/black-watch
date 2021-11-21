using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon.JobEngine
{
    /// <summary>
    /// a job executed by the <see cref="JobExecutor"/>
    /// </summary>
    public abstract class Job
    {
        private readonly string _moniker;

        protected Job(string moniker)
        {
            _moniker = moniker;
        }

        public abstract Task<JobExecutionResult> ExecuteAsync(JobExecutionContext ctx);

        public override string ToString()
        {
            return $"Job[{_moniker}]";
        }
    }

    /// <summary>
    /// a job that does nothing, only logs a warning. can be used to signal some misunderstanding...
    /// </summary>
    public class NopJob : Job
    {
        private NopJob() : base("nop") { }

        public static Job Instance { get; } = new NopJob();

        public override Task<JobExecutionResult> ExecuteAsync(JobExecutionContext ctx)
        {
            ctx.Logger.LogWarning("executing nop job");
            return Task.FromResult(JobExecutionResult.Ok);
        }
    }
}
