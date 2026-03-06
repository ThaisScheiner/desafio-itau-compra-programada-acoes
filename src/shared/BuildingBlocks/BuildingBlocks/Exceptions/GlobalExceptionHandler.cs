using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;

namespace BuildingBlocks.Exceptions;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, code, title) = exception switch
        {
            DomainException de => (HttpStatusCode.BadRequest, de.Code, de.Message),
            KeyNotFoundException => (HttpStatusCode.NotFound, "NAO_ENCONTRADO", "Recurso nao encontrado."),
            _ => (HttpStatusCode.InternalServerError, "ERRO_INTERNO", "Erro inesperado.")
        };

        logger.LogError(exception, "Erro tratado: {Code}", code);

        var problem = new ProblemDetails
        {
            Status = (int)status,
            Title = title,
            Detail = exception.Message
        };

        problem.Extensions["codigo"] = code;

        httpContext.Response.StatusCode = problem.Status ?? 500;
        httpContext.Response.ContentType = "application/json; charset=utf-8";

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}