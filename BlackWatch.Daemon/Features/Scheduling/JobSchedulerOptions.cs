using System.ComponentModel.DataAnnotations;

namespace BlackWatch.Daemon.Features.Scheduling
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
        [Required]
        public string DownloadTrackers { get; set; } = "@daily";

        [Required]
        public string DownloadQuoteHistory { get; set; } = "@hourly";
    }
}