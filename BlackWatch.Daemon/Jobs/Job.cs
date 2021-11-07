using System;
using System.Threading.Tasks;

namespace BlackWatch.Daemon.Jobs
{
    public class Job
    {
        private readonly Func<JobExecutionContext, Task> _action;
        private readonly string _moniker;

        public Job(string moniker, Func<JobExecutionContext, Task> action)
        {
            _moniker = moniker;
            _action = action;
        }

        public Task ExecuteAsync(JobExecutionContext ctx)
        {
            return _action(ctx);
        }

        public override string ToString()
        {
            return $"Job[{_moniker}]";
        }
    }
}
