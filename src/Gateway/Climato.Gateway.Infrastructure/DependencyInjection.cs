using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Options;
using Climato.Gateway.Domain.Cities;
using Climato.Gateway.Domain.Users;
using Climato.Gateway.Infrastructure.Auth;
using Climato.Gateway.Infrastructure.Data;
using Climato.Gateway.Infrastructure.Data.Repositories;
using Climato.Gateway.Infrastructure.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Climato.Gateway.Infrastructure;

public static class DependencyInjection
{
    private static readonly TimeSpan healthCheckTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan providerHealthCheckTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan providerConnectTimeout = TimeSpan.FromSeconds(2);

    public static IServiceCollection AddGatewayInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Climato");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:Climato não configurada. Ver docs/runbook.md.");
        }

        services.AddDbContext<ClimatoDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ICityRepository, CityRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        services.AddOptions<ProviderOptions>()
            .BindConfiguration(ProviderOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<DemoOptions>()
            .BindConfiguration(DemoOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<ITokenService, JwtTokenService>();

        services.AddHttpClient<IWeatherProviderClient, WeatherProviderClient>(ConfigureProviderClient)
            .ConfigurePrimaryHttpMessageHandler(CreateProviderHandler)
            .AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(25);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
                options.Retry.MaxRetryAttempts = 1;
                options.Retry.Delay = TimeSpan.FromMilliseconds(500);
                options.Retry.ShouldHandle = args => ValueTask.FromResult(IsTransportFailure(args.Outcome.Exception, args.Context.CancellationToken));
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
            });

        services.AddHttpClient(ProviderHealthCheck.HttpClientName, (serviceProvider, client) =>
        {
            ConfigureProviderClient(serviceProvider, client);
            client.Timeout = TimeSpan.FromSeconds(4);
        })
            .ConfigurePrimaryHttpMessageHandler(CreateProviderHandler);

        services.AddHealthChecks()
            .AddDbContextCheck<ClimatoDbContext>("postgres", tags: ["ready"])
            .AddCheck<ProviderHealthCheck>("provider", tags: ["ready"]);

        services.PostConfigure<HealthCheckServiceOptions>(options =>
        {
            foreach (var registration in options.Registrations)
            {
                registration.Timeout = registration.Name == "provider" ? providerHealthCheckTimeout : healthCheckTimeout;
            }
        });

        return services;
    }

    private static SocketsHttpHandler CreateProviderHandler()
    {
        return new SocketsHttpHandler { ConnectTimeout = providerConnectTimeout };
    }

    private static bool IsTransportFailure(Exception? exception, CancellationToken cancellationToken)
    {
        return exception is HttpRequestException
            || (exception is TaskCanceledException { InnerException: TimeoutException } && !cancellationToken.IsCancellationRequested);
    }

    private static void ConfigureProviderClient(IServiceProvider serviceProvider, HttpClient client)
    {
        var options = serviceProvider.GetRequiredService<IOptions<ProviderOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl);
    }
}
