using BankingDemo.Api.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BankingDemo.Api.Endpoints;

/// <summary>Maps domain exceptions to RFC 7807 problem responses.</summary>
public class BankingExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            AccountNotFoundException => (StatusCodes.Status404NotFound, "Account not found"),
            InsufficientFundsException => (StatusCodes.Status422UnprocessableEntity, "Insufficient funds"),
            InvalidTransferException => (StatusCodes.Status400BadRequest, "Invalid transfer"),
            _ => (0, null)
        };

        if (status == 0)
        {
            return false;
        }

        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = exception.Message }
        });
    }
}
