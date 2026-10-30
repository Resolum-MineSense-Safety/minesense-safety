using Microsoft.AspNetCore.Http;

namespace MineSenseSafety.Shared.Infrastructure.Security;

/// <summary>
/// Copies the caller's <c>Authorization</c> header to a synchronous call to another microservice,
/// so the downstream service validates the same Identity &amp; Access token (ADR-007).
/// </summary>
public sealed class ForwardAuthorizationHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var authorization = accessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(authorization) && request.Headers.Authorization is null)
            request.Headers.TryAddWithoutValidation("Authorization", authorization);
        return base.SendAsync(request, cancellationToken);
    }
}
