using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Services;
using BlackWatch.Core.Util;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class TallySourceController : Controller
{

    private const string UserId = "0";
    private const string ResponseMimeType = "application/json";
    private readonly IUserDataStore _userDataStore;
    private readonly IIdGenerator _idGenerator;
    private readonly ILogger<TallySourceController> _logger;
    private readonly TallyService _tallyService;

    public TallySourceController(
        IUserDataStore userDataStore,
        IIdGenerator idGenerator,
        ILogger<TallySourceController> logger,
        TallyService tallyService)
    {
        _userDataStore = userDataStore;
        _idGenerator = idGenerator;
        _logger = logger;
        _tallyService = tallyService;
    }

    [HttpGet("{id}")]
    [Produces(ResponseMimeType)]
    public async Task<ActionResult<TallySource>> GetById(string id)
    {
        var tallySource = await _userDataStore.GetTallySourceAsync(UserId, id);
        // ReSharper disable once InvertIf
        if (tallySource == null)
        {
            _logger.LogWarning("tally source not found: {TallySourceId}", id);
            return NotFound();
        }

        return Ok(tallySource);
    }

    [HttpGet("{id}/eval")]
    [ProducesResponseType(typeof(Tally), (int)HttpStatusCode.OK)]
    [Produces(ResponseMimeType)]
    public async Task<ActionResult<Tally>> Evaluate(string id)
    {
        var tally = await InternalEvaluateAsync(id);

        return tally != null
            ? Ok(tally)
            : NotFound();
    }

    [HttpPost("{id}/eval")]
    [ProducesResponseType(typeof(Tally), (int)HttpStatusCode.Created)]
    [Produces(ResponseMimeType)]
    public async Task<ActionResult<Tally>> EvaluateAndStoreTally(string id)
    {
        var tally = await InternalEvaluateAsync(id);
        if (tally == null)
        {
            return NotFound();
        }

        await _userDataStore.PutTallyAsync(tally);
        _logger.LogInformation("tally stored: {Tally}", tally);
        return CreatedAtAction(nameof(GetTally), new { id, count = 1 }, tally);
    }

    [HttpGet("{id}/tally")]
    [ProducesResponseType(typeof(Tally), (int)HttpStatusCode.OK)]
    [Produces(ResponseMimeType)]
    public async Task<ActionResult<Tally[]>> GetTally(string id, [FromQuery] int count = 1)
    {
        var tallySource = await _userDataStore.GetTallySourceAsync(UserId, id);
        // ReSharper disable once InvertIf
        if (tallySource == null)
        {
            _logger.LogWarning("tally source not found: {TallySourceId}", id);
            return NotFound();
        }

        var tallies = await _userDataStore.GetTalliesAsync(id, count);
        return Ok(tallies);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<TallySource>), (int)HttpStatusCode.OK)]
    [Produces(ResponseMimeType)]
    public async Task<IReadOnlyCollection<TallySource>> Index()
    {
        return await _userDataStore.GetTallySourcesAsync().ToListAsync().Linger();
    }

    [HttpPost]
    [ProducesResponseType(typeof(TallySource), (int)HttpStatusCode.Created)]
    [Produces(ResponseMimeType)]
    public async Task<IActionResult> Create([FromBody] PutTallySourceCommand command)
    {
        var id = await _idGenerator.GenerateIdAsync();
        var tallySource = new TallySource(id, command.Name, command.Message, command.Code, command.Interval, 1, DateTimeOffset.UtcNow);
        await _userDataStore.PutTallySourceAsync(UserId, tallySource);
        _logger.LogInformation("tally source created: user-{UserId}:{TallySourceId}", UserId, id);
        return CreatedAtAction(nameof(GetById), new { id = tallySource.Id }, tallySource);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TallySource), (int)HttpStatusCode.OK)]
    [Produces(ResponseMimeType)]
    public async Task<IActionResult> Update(string id, [FromBody] PutTallySourceCommand command)
    {
        var tallySource = await _userDataStore.GetTallySourceAsync(UserId, id);
        if (tallySource == null)
        {
            _logger.LogWarning("tally source not found: user-{UserId}:{TallySourceId}", UserId, id);
            return NotFound();
        }

        var modifiedTallySource = tallySource.Update(command.Name, command.Message, command.Code, command.Interval);
        await _userDataStore.PutTallySourceAsync(UserId, modifiedTallySource);
        _logger.LogInformation("tally source updated: user-{UserId}:{TallySourceId}", UserId, id);
        return Ok(modifiedTallySource);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        // ReSharper disable once InvertIf
        if (await _userDataStore.DeleteTallySourceAsync(UserId, id) == false)
        {
            _logger.LogWarning("tally source not found: user-{UserId}:{TallySourceId}", UserId, id);
            return NotFound();
        }

        _logger.LogInformation("tally source removed: user-{UserId}:{TallySourceId}", UserId, id);
        return NoContent();
    }

    private async Task<Tally?> InternalEvaluateAsync(string id)
    {
        var tallySource = await _userDataStore.GetTallySourceAsync(UserId, id);
        // ReSharper disable once InvertIf
        if (tallySource == null)
        {
            _logger.LogWarning("tally source not found: {TallySourceId}", id);
            return null;
        }

        return await _tallyService.EvaluateAsync(tallySource);
    }
}
