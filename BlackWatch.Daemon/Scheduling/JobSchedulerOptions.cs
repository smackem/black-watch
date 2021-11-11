using System.ComponentModel.DataAnnotations;

namespace BlackWatch.Daemon
{
    public class JobSchedulerOptions
    {
        [Range(1, 1000)]
        public int QuoteHistoryDays { get; set; } = 100;
    }
}