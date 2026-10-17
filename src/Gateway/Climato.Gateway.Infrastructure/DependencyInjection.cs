using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Options;
using Climato.Gateway.Domain.Cities;
using Climato.Gateway.Domain.Users;
using Climato.Gateway.Infrastructure.Data;
using Climato.Gateway.Infrastructure.Data.Repositories;
using Climato.Gateway.Infrastructure.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Climato.Gateway.Infrastructure;

public static class DependencyInjection
{
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

        services.AddHttpClient<IWeatherProviderClient, WeatherProviderClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ProviderOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
        });

        return services;
    }
}
