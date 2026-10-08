using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MineSenseSafety.Shared.Domain.Model;

namespace MineSenseSafety.Shared.Interfaces.ASP.Configuration;

/// <summary>
/// Maps broken business rules to HTTP 422 so controllers stay free of try/catch.
/// </summary>
public class DomainExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not DomainException exception) return;
        context.Result = new UnprocessableEntityObjectResult(new { message = exception.Message });
        context.ExceptionHandled = true;
    }
}
