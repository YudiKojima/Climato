using Climato.Provider.Application.Contracts;
using Climato.Provider.Application.Options;
using Climato.Provider.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Climato.Provider.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddProviderApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OpenWeatherOptions>()
            .BindConfiguration(OpenWeatherOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<PexelsOptions>()
            .BindConfiguration(PexelsOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var redisConnectionString = configuration.GetConnectionString("Redis");

        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddDistributedMemoryCache();
        }
        else
        {
            services.AddStackExchangeRedisCache(options => options.Configuration = redisConnectionString);
        }

        services.AddSingleton<IWeatherCache, WeatherCache>();

        services.AddHttpClient<IWeatherAppService, WeatherAppService>((serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<OpenWeatherOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
            })
            .AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(15);
                options.Retry.MaxRetryAttempts = 2;
            });

        services.AddHttpClient<ICityImageAppService, CityImageAppService>((serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<PexelsOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", options.ApiKey);
            })
            .AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(5);
                options.Retry.MaxRetryAttempts = 1;
            });

        return services;
    }
}
