using System;
using System.Threading.Tasks;

namespace BlackWatch.Daemon.Jobs
{
    public class Job
    {
        private readonly Func<JobExecutionContext, Task<JobExecutionResult>> _action;
        private readonly string _moniker;

        public Job(string moniker, Func<JobExecutionContext, Task<JobExecutionResult>> action)
        {
            _moniker = moniker;
            _action = action;
        }

        public Task<JobExecutionResult> ExecuteAsync(JobExecutionContext ctx)
        {
            return _action(ctx);
        }

        public override string ToString()
        {
            return $"Job[{_moniker}]";
        }
    }
}
