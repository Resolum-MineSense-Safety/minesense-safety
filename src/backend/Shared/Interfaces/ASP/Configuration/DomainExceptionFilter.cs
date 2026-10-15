using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MineSenseSafety.Shared.Domain.Model;

namespace MineSenseSafety.Shared.Interfaces.ASP.Configuration;

/// <summary>
/// Maps broken business rules to HTTP 422 (or 409 for state conflicts)
/// so controllers stay free of try/catch.
/// </summary>
public class DomainExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not DomainException exception) return;
        var body = new { message = exception.Message };
        context.Result = exception is DomainConflictException
            ? new ConflictObjectResult(body)
            : new UnprocessableEntityObjectResult(body);
        context.ExceptionHandled = true;
    }
}
