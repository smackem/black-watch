using System.Collections.Generic;

namespace BlackWatch.Daemon.Cron
{
    public interface ICronActionSupplier
    {
        public IEnumerable<CronAction> Actions { get; }
    }
}
