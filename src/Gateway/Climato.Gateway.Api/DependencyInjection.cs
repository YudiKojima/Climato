using System.Text;
using Climato.Gateway.Api.Errors;
using Climato.Gateway.Api.Filters;
using Climato.Gateway.Api.Observability;
using Climato.Gateway.Api.OpenApi;
using Climato.Gateway.Api.RateLimiting;
using Climato.Gateway.Application.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Climato.Gateway.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddGatewayApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers(options => options.Filters.Add<ValidationFilter>())
            .AddJsonOptions(options => options.AllowInputFormatterExceptionMessages = false)
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var bodyParameters = context.ActionDescriptor.Parameters
                        .Where(parameter => parameter.BindingInfo?.BindingSource == BindingSource.Body)
                        .Select(parameter => parameter.Name)
                        .ToList();
                    var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
                    var problem = factory.CreateValidationProblemDetails(
                        context.HttpContext,
                        InvalidRequest.Sanitize(context.ModelState, bodyParameters),
                        StatusCodes.Status400BadRequest,
                        "Dados inválidos.");

                    return new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
                };
            });
        services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
        services.AddProblemDetails(options => options.CustomizeProblemDetails = ProblemDetailsTraceId.Apply);
        services.AddExceptionHandler<GlobalExceptionHandler>();

        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy("Web", policy => policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .WithExposedHeaders(CorrelationIdMiddleware.HeaderName));
        });

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;

                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = JwtRegisteredClaimNames.Name
                };
            });

        services.AddAuthorization();
        services.AddGatewayRateLimiting();

        return services;
    }
}
