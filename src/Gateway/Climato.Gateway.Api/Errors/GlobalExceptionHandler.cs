using Climato.Gateway.Application.Exceptions;
using Climato.Gateway.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Gateway.Api.Errors;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    {
        this.problemDetailsService = problemDetailsService;
        this.logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("Requisição cancelada pelo cliente em {Method} {Path}.", httpContext.Request.Method, httpContext.Request.Path);
            httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            return true;
        }

        ProblemDetails problem;

        if (exception is DomainException)
        {
            problem = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Operação não permitida.",
                Detail = exception.Message
            };
        }
        else if (exception is UserNotFoundException)
        {
            logger.LogInformation("Token de usuário inexistente em {Method} {Path}.", httpContext.Request.Method, httpContext.Request.Path);

            problem = new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Sessão inválida."
            };
        }
        else if (exception is ProviderUnavailableException)
        {
            logger.LogWarning(exception, "Provider indisponível em {Method} {Path}.", httpContext.Request.Method, httpContext.Request.Path);

            problem = new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Serviço de clima indisponível. Tente novamente em instantes."
            };
        }
        else
        {
            logger.LogError(exception, "Erro não tratado em {Method} {Path}.", httpContext.Request.Method, httpContext.Request.Path);

            problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Erro interno."
            };
        }

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem
        });
    }
}
