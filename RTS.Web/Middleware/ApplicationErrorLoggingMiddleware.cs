using RTS.Application.Auditing;
using RTS.Web.Identity;

namespace RTS.Web.Middleware;

public sealed class ApplicationErrorLoggingMiddleware
{
    public const string CorrelationItemKey =
        "ApplicationCorrelationId";

    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ApplicationErrorLoggingMiddleware>
        _logger;

    public ApplicationErrorLoggingMiddleware(
        RequestDelegate next,
        IServiceScopeFactory scopeFactory,
        ILogger<ApplicationErrorLoggingMiddleware> logger)
    {
        _next = next;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId =
            context.Items.TryGetValue(
                CorrelationItemKey,
                out var existingValue) &&
            existingValue is Guid existingCorrelationId
                ? existingCorrelationId
                : Guid.NewGuid();

        context.Items[CorrelationItemKey] =
            correlationId;

        context.Response.Headers[
            "X-Correlation-ID"] =
            correlationId.ToString("D");

        try
        {
            await _next(context);
        }
        catch (Exception exception)
            when (!context.RequestAborted
                .IsCancellationRequested)
        {
            await TryRecordAsync(
                context,
                exception,
                correlationId);

            throw;
        }
    }

    private async Task TryRecordAsync(
        HttpContext context,
        Exception exception,
        Guid correlationId)
    {
        try
        {
            Guid? userExternalId = null;

            if (context.User.TryGetExternalUserId(
                out var externalUserId))
            {
                userExternalId = externalUserId;
            }

            await using var scope =
                _scopeFactory.CreateAsyncScope();

            var errorService =
                scope.ServiceProvider
                    .GetRequiredService<
                        IApplicationErrorService>();

            await errorService.RecordAsync(
                new RecordApplicationErrorRequest(
                    userExternalId,
                    correlationId,
                    exception.GetType().FullName
                        ?? exception.GetType().Name,
                    null,
                    "An unexpected error occurred.",
                    exception.ToString(),
                    exception.Source,
                    context.Request.Path.Value,
                    context.Request.Method,
                    StatusCodes
                        .Status500InternalServerError),
                CancellationToken.None);
        }
        catch (Exception loggingException)
        {
            _logger.LogError(
                loggingException,
                "Unable to persist application error " +
                "for correlation ID {CorrelationId}.",
                correlationId);
        }
    }
}