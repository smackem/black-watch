using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlackWatch.WebApp.Features.Api;

public interface IApiClient
{
    Task<IReadOnlyCollection<TallySource>> GetTallySourcesAsync();
    Task<TallySource> GetTallySourceAsync(string id);
    Task<TallySource> CreateTallySourceAsync(PutTallySourceCommand command);
    Task<TallySource> UpdateTallySourceAsync(string id, PutTallySourceCommand command);
    Task DeleteTallySourceAsync(string id);
    Task<Tally> EvaluateTallySourceAsync(string id);
    Task<Tally> EvaluateTallySourceAndStoreTallyAsync(string id);
    Task<IReadOnlyCollection<Tally>> GetTallyAsync(string tallySourceId, int count);
    Task<IReadOnlyCollection<Tally>> GetTalliesAsync(int count);
}
