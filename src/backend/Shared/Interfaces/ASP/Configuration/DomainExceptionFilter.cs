using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MineSenseSafety.Shared.Domain.Model;

namespace MineSenseSafety.Shared.Interfaces.ASP.Configuration;

/// <summary>
/// Maps broken business rules to HTTP 422, state conflicts to 409, missing references to 404
/// and missing or malformed data to 400, so controllers stay free of try/catch.
/// </summary>
public class DomainExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not DomainException exception) return;
        var body = new { message = exception.Message };
        context.Result = exception switch
        {
            DomainConflictException => new ConflictObjectResult(body),
            DomainNotFoundException => new NotFoundObjectResult(body),
            DomainValidationException => new BadRequestObjectResult(body),
            _ => new UnprocessableEntityObjectResult(body)
        };
        context.ExceptionHandled = true;
    }
}
