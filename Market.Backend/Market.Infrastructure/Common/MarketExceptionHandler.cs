using FluentValidation;
using Market.Application.Common.Exceptions;
using Market.Shared.Dtos;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Security.Claims;

namespace Market.Infrastructure.Common;

/// <summary>
/// Global exception handler for unhandled exceptions.
/// Converts every exception into the same <see cref="ErrorDto"/> JSON shape:
/// <list type="bullet">
/// <item>ValidationException → 400 "validation.error" (+ field errors)</item>
/// <item>MarketUnauthorizedException → 401 "auth.unauthorized"</item>
/// <item>MarketForbiddenException → 403 "auth.forbidden"</item>
/// <item>MarketNotFoundException → 404 "entity.error"</item>
/// <item>MarketConflictException / MarketBusinessRuleException → 409 "entity.error"</item>
/// <item>DbUpdateException → 409 "database.error"</item>
/// <item>anything else → 500 "internal.error"</item>
/// </list>
/// </summary>
public sealed class MarketExceptionHandler(
    ILogger<MarketExceptionHandler> logger,
    IHostEnvironment env
) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        // If the response has already started, let it bubble up
        if (ctx.Response.HasStarted)
        {
            logger.LogWarning(ex, "Response already started, letting the exception bubble.");
            return false;
        }

        var traceId = Activity.Current?.Id ?? ctx.TraceIdentifier;
        var statusCode = GetStatusCode(ex);
        var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";

        if (statusCode >= StatusCodes.Status500InternalServerError || ex is DbUpdateException)
        {
            logger.LogError(ex,
                "Unhandled exception ({StatusCode}). Path: {Path}, Method: {Method}, TraceId: {TraceId}, UserId: {UserId}",
                statusCode, ctx.Request.Path, ctx.Request.Method, traceId, userId);
        }
        else
        {
            // Expected errors (validation, 401/403/404/409) are not bugs: log them without the stack trace.
            logger.LogWarning(
                "Request failed with {StatusCode} ({ExceptionType}: {ExceptionMessage}). Path: {Path}, Method: {Method}, TraceId: {TraceId}, UserId: {UserId}",
                statusCode, ex.GetType().Name, ex.Message, ctx.Request.Path, ctx.Request.Method, traceId, userId);
        }

        ctx.Response.ContentType = "application/json";
        ctx.Response.StatusCode = statusCode;

        var error = BuildErrorDto(ex, env.IsDevelopment(), traceId);

        await ctx.Response.WriteAsJsonAsync(error, cancellationToken: ct);
        return true; // prevents rethrowing the exception
    }

    private static int GetStatusCode(Exception ex) => ex switch
    {
        ValidationException => StatusCodes.Status400BadRequest,
        MarketUnauthorizedException => StatusCodes.Status401Unauthorized,
        MarketForbiddenException => StatusCodes.Status403Forbidden,
        MarketNotFoundException => StatusCodes.Status404NotFound,
        MarketConflictException or MarketBusinessRuleException => StatusCodes.Status409Conflict,
        DbUpdateException => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };

    private static ErrorDto BuildErrorDto(Exception ex, bool isDev, string traceId)
    {
        string code = "internal.error";
        string message = "Došlo je do neočekivane greške. Pokušajte ponovo.";
        List<FieldErrorDto>? errors = null;

        switch (ex)
        {
            case ValidationException vex:
                code = "validation.error";
                if (vex.Errors.Any())
                {
                    errors = vex.Errors
                        .Select(e => new FieldErrorDto
                        {
                            Field = FieldErrorDto.ToCamelCasePath(e.PropertyName),
                            Message = e.ErrorMessage,
                            ErrorCode = e.ErrorCode
                        })
                        .ToList();
                    message = string.Join(" ", vex.Errors.Select(e => e.ErrorMessage).Distinct());
                }
                else
                {
                    message = vex.Message;
                }
                break;

            case MarketUnauthorizedException:
                code = "auth.unauthorized";
                message = ex.Message;
                break;

            case MarketForbiddenException:
                code = "auth.forbidden";
                message = ex.Message;
                break;

            case MarketNotFoundException:
            case MarketConflictException:
            case MarketBusinessRuleException:
                code = "entity.error";
                message = ex.Message;
                break;

            case DbUpdateException:
                // Never send SQL details to the client (they are logged, and shown in Details only in Development).
                code = "database.error";
                message = "Podaci nisu spremljeni jer su u konfliktu sa postojećim podacima (npr. duplikat ili povezani zapis).";
                break;
        }

        return new ErrorDto
        {
            Code = code,
            Message = message,
            TraceId = traceId,
            Details = isDev ? ex.ToString() : null, // stack trace only in Development environment
            Errors = errors
        };
    }
}
