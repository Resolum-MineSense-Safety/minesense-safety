using FeedbackReliabilityService.Application.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FeedbackReliabilityService.Interfaces.REST;

public class FeedbackExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var status = context.Exception switch
        {
            FeedbackNotFoundException => StatusCodes.Status404NotFound,
            FeedbackForbiddenException => StatusCodes.Status403Forbidden,
            AlertSourceUnavailableException => StatusCodes.Status503ServiceUnavailable,
            _ => 0
        };
        if (status == 0) return;
        context.Result = new ObjectResult(new ProblemDetails { Status = status, Title = context.Exception.Message }) { StatusCode = status };
        context.ExceptionHandled = true;
    }
}
