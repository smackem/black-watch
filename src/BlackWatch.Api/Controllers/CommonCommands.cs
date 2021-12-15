using BlackWatch.Core.Contracts;

namespace BlackWatch.Api.Controllers;

public record PutTallySourceCommand(string Name, string Message, string Code, EvaluationInterval Interval);
