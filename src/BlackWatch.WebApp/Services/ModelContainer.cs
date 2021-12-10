using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BlackWatch.WebApp.Features.Api;

namespace BlackWatch.WebApp.Services
{
    public class ModelContainer
    {
        private readonly Dictionary<string, TallySource> _tallySources = new();
        private readonly List<Tally> _tallies = new();

        public IReadOnlyCollection<TallySource> TallySources =>
            _tallySources.Values;

        public IReadOnlyCollection<Tally> Tallies =>
            new ReadOnlyCollection<Tally>(_tallies);

        public void PutTallySource(TallySource tallySource) =>
            _tallySources[tallySource.Id ?? throw new ArgumentNullException(nameof(tallySource))] = tallySource;

        public TallySource? GetTallySource(string id) =>
            _tallySources.TryGetValue(id, out var ts) ? ts : null;

        public void PutTally(Tally tally) =>
            _tallies.Add(tally);

        public IReadOnlyList<Tally> GetTallies(string tallySourceId) => _tallies
            .Where(t => t.TallySourceId == tallySourceId)
            .OrderByDescending(t => t.DateCreated)
            .ToArray();
    }
}
