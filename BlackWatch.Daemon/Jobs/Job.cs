using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon.Jobs
{
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
