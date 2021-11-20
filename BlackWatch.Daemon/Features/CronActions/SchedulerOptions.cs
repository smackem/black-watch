using System.ComponentModel.DataAnnotations;

namespace BlackWatch.Daemon.Features.CronActions
{
    public class SchedulerOptions
    {
        [Range(1, 1000)]
        public int QuoteHistoryDays { get; set; } = 100;

        [Required]
        public SchedulerCronOptions Cron { get; set; } = new();
    }

    public class SchedulerCronOptions
    {
        [Required]
        public string DownloadTrackers { get; set; } = "@daily";

        [Required]
        public string DownloadQuoteHistory { get; set; } = "@hourly";
    }
}