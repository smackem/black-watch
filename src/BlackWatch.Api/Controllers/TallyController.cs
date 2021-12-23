using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class TallyController
{
    private const string UserId = "0";
    private const string ResponseMimeType = "application/json";
    private readonly IUserDataStore _userDataStore;
    private readonly ILogger<TallyController> _logger;
    private readonly TallyService _tallyService;

    public TallyController(IUserDataStore userDataStore, ILogger<TallyController> logger, TallyService tallyService)
    {
        _userDataStore = userDataStore;
        _logger = logger;
        _tallyService = tallyService;
    }

    [HttpGet]
    [Produces(ResponseMimeType)]
    [ProducesResponseType(typeof(IReadOnlyCollection<Tally>), (int) HttpStatusCode.OK)]
    public async Task<IReadOnlyCollection<Tally>> Index([FromQuery] int count = 1)
    {
        var tallies = new List<Tally>();
        await foreach (var tallySource in _userDataStore.GetTallySourcesAsync(UserId))
        {
            var localTallies = await _userDataStore.GetTalliesAsync(tallySource.Id, count);
            tallies.AddRange(localTallies);
        }
        return tallies;
    }

    [HttpPost("eval")]
    [Produces(ResponseMimeType)]
    [ProducesResponseType(typeof(Tally), (int)HttpStatusCode.OK)]
    public async Task<Tally> Eval([FromBody] PutTallySourceCommand command)
    {
        var tallySource = new TallySource(
            Id: "temp-id",
            Name: command.Name,
            Message: command.Message,
            Code: command.Code,
            Interval: command.Interval,
            Version: 1,
            DateModified: DateTimeOffset.Now);

        return await _tallyService.EvaluateAsync(tallySource);
    }
}
