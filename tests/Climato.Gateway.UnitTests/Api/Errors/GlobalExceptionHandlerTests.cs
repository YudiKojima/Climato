using Climato.Gateway.Api.Errors;
using Climato.Gateway.Application.Exceptions;
using Climato.Gateway.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Climato.Gateway.UnitTests.Api.Errors;

public class GlobalExceptionHandlerTests
{
    private readonly IProblemDetailsService problemDetailsService = Substitute.For<IProblemDetailsService>();
    private readonly GlobalExceptionHandler handler;
    private readonly DefaultHttpContext httpContext = new();
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    public GlobalExceptionHandlerTests()
    {
        problemDetailsService.TryWriteAsync(Arg.Any<ProblemDetailsContext>()).Returns(true);
        handler = new GlobalExceptionHandler(problemDetailsService, NullLogger<GlobalExceptionHandler>.Instance);
    }

    [Fact]
    public async Task TryHandleAsync_DomainException_Devolve409ComAMensagem()
    {
        var handled = await handler.TryHandleAsync(httpContext, new DomainException("Regra violada."), cancellationToken);

        handled.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status409Conflict);
        await problemDetailsService.Received(1).TryWriteAsync(Arg.Is<ProblemDetailsContext>(context =>
            context.ProblemDetails.Status == StatusCodes.Status409Conflict
            && context.ProblemDetails.Title == "Operação não permitida."
            && context.ProblemDetails.Detail == "Regra violada."));
    }

    [Fact]
    public async Task TryHandleAsync_UserNotFoundException_Devolve401SessaoInvalida()
    {
        await handler.TryHandleAsync(httpContext, new UserNotFoundException(), cancellationToken);

        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
        await problemDetailsService.Received(1).TryWriteAsync(Arg.Is<ProblemDetailsContext>(context =>
            context.ProblemDetails.Title == "Sessão inválida."));
    }

    [Fact]
    public async Task TryHandleAsync_ProviderUnavailableException_Devolve503()
    {
        await handler.TryHandleAsync(httpContext, new ProviderUnavailableException("O Provider respondeu 500."), cancellationToken);

        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status503ServiceUnavailable);
        await problemDetailsService.Received(1).TryWriteAsync(Arg.Is<ProblemDetailsContext>(context =>
            context.ProblemDetails.Title == "Serviço de clima indisponível. Tente novamente em instantes."
            && context.ProblemDetails.Detail == null));
    }

    [Fact]
    public async Task TryHandleAsync_ExcecaoInesperada_Devolve500SemDetalhe()
    {
        await handler.TryHandleAsync(httpContext, new InvalidOperationException("detalhe interno"), cancellationToken);

        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        await problemDetailsService.Received(1).TryWriteAsync(Arg.Is<ProblemDetailsContext>(context =>
            context.ProblemDetails.Title == "Erro interno."
            && context.ProblemDetails.Detail == null));
    }

    [Fact]
    public async Task TryHandleAsync_CanceladoPeloCliente_Devolve499SemCorpo()
    {
        using var aborted = new CancellationTokenSource();
        aborted.Cancel();
        httpContext.RequestAborted = aborted.Token;

        var handled = await handler.TryHandleAsync(httpContext, new OperationCanceledException(), cancellationToken);

        handled.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status499ClientClosedRequest);
        await problemDetailsService.DidNotReceiveWithAnyArgs().TryWriteAsync(default!);
    }

    [Fact]
    public async Task TryHandleAsync_CancelamentoSemAbortoDoCliente_Devolve500()
    {
        await handler.TryHandleAsync(httpContext, new OperationCanceledException(), cancellationToken);

        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }
}
