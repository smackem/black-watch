using System.ComponentModel.DataAnnotations;

namespace BlackWatch.Core.Services
{
    public class RedisOptions
    {
        [Required]
        public string ConnectionString { get; set; } = string.Empty;
    }
}
