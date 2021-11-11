using System;
using System.ComponentModel.DataAnnotations;

namespace BlackWatch.Daemon.Scheduling
{
    public class JobSchedulerOptions
    {
        [Range(1, 1000)]
        public int QuoteHistoryDays { get; set; } = 100;

        [Required]
        public JobSchedulerCronOptions Cron { get; set; } = new();
    }

    public class JobSchedulerCronOptions
    {
        public string DownloadTrackers { get; set; } = string.Empty;
        public string DownloadAggregates { get; set; } = string.Empty;
    }
}