using System.Threading.Tasks;

namespace BlackWatch.Core.Contracts;

public interface IIdGenerator
{
    /// <summary>
    /// generates a new id, unique to the scope of this application
    /// </summary>
    public Task<string> GenerateIdAsync();
}
