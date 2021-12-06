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
        public string DownloadQuoteHistory { get; set; } = "@daily";

        [Required]
        public string DownloadQuoteSnapshot { get; set; } = "@hourly";

        [Required]
        public string EvaluationEveryHour { get; set; } = "10 * * * *";

        [Required]
        public string EvaluationEverySixHours { get; set; } = "0 */6 * * *";

        [Required]
        public string EvaluationEveryDay { get; set; } = "0 1 * * *";
    }
}