using System.ComponentModel.DataAnnotations;

namespace BlackWatch.Daemon.JobEngine
{
    public class JobExecutorOptions
    {
        [Range(1, int.MaxValue)]
        public int MaxJobsPerMinute { get; set; } = int.MaxValue;
    }
}
