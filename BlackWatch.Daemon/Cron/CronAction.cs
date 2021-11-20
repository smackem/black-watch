using System.Threading.Tasks;
using Cronos;

namespace BlackWatch.Daemon.Cron
{
    public abstract class CronAction
    {
        protected CronAction(CronExpression cronExpr, string moniker)
        {
            CronExpr = cronExpr;
            Moniker = moniker;
        }

        public CronExpression CronExpr { get; }
        public string Moniker { get; }

        public abstract Task<bool> ExecuteAsync();

        public override string ToString()
        {
            return $"CronAction[{Moniker}]";
        }
    }
}
