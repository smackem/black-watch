using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BlackWatch.Daemon
{
    public class JobExecutionConfig
    {
        [Range(1, int.MaxValue)]
        public int MaxJobsPerMinute { get; set; } = int.MaxValue;
    }
}