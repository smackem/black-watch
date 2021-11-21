namespace BlackWatch.Daemon.JobEngine
{
    /// <summary>
    /// execution results for jobs executed by <see cref="JobExecutor"/>
    /// </summary>
    public enum JobExecutionResult
    {
        /// <summary>
        /// all is fine
        /// </summary>
        Ok,
        
        /// <summary>
        /// enqueue the job again
        /// </summary>
        Retry,
        
        /// <summary>
        /// enqueue the job again and suspend job execution for a minute
        /// </summary>
        WaitAndRetry,

        /// <summary>
        /// give up on the job
        /// </summary>
        Fatal,
    }
}
