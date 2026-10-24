using Microsoft.AspNetCore.Diagnostics;
using TicTacToe.Api.Domain;

namespace TicTacToe.Api.Endpoints;

/// <summary>Translates domain exceptions into RFC 7807 problem responses with a machine-readable "code".</summary>
public sealed class GameExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        (int Status, string Title, string Code)? mapped = exception switch
        {
            GameNotFoundException => (StatusCodes.Status404NotFound, "Game not found", "GameNotFound"),
            GameRuleException { Code: GameErrorCode.InvalidRequest or GameErrorCode.OutOfBounds } e
                => (StatusCodes.Status400BadRequest, "Invalid request", e.Code.ToString()),
            GameRuleException e => (StatusCodes.Status409Conflict, "Action not allowed", e.Code.ToString()),
            _ => null,
        };

        if (mapped is not { } m)
        {
            return false;
        }

        httpContext.Response.StatusCode = m.Status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = m.Status,
                Title = m.Title,
                Detail = exception.Message,
                Extensions = { ["code"] = m.Code },
            },
        });
    }
}
