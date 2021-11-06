using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlackWatch.Daemon
{
    public class Job
    {
        private readonly Func<CancellationToken, Task> _action;
        private readonly string _moniker;

        public Job(string moniker, Func<CancellationToken, Task> action)
        {
            _moniker = moniker;
            _action = action;
        }

        public Task ExecuteAsync(CancellationToken ct)
        {
            return _action(ct);
        }

        public override string ToString()
        {
            return $"Job: {_moniker}";
        }
    }
}
