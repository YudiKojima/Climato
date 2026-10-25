using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Exceptions;
using Climato.Gateway.Application.Model.Locations;
using Climato.Gateway.Application.Model.Weathers;
using Polly;

namespace Climato.Gateway.Infrastructure.Http;

public class WeatherProviderClient : IWeatherProviderClient
{
    private readonly HttpClient client;

    public WeatherProviderClient(HttpClient httpClient)
    {
        client = httpClient;
    }

    public Task<WeatherDto?> GetWeatherAsync(string cityName, CancellationToken cancellationToken)
    {
        return GetAsync<WeatherDto>($"weathers?cityName={Uri.EscapeDataString(cityName)}", cancellationToken);
    }

    public Task<ForecastDto?> GetForecastAsync(string cityName, CancellationToken cancellationToken)
    {
        return GetAsync<ForecastDto>($"forecasts?cityName={Uri.EscapeDataString(cityName)}", cancellationToken);
    }

    public async Task<IReadOnlyList<LocationDto>> SearchLocationsAsync(string query, CancellationToken cancellationToken)
    {
        return await GetAsync<List<LocationDto>>($"locations?query={Uri.EscapeDataString(query)}", cancellationToken) ?? [];
    }

    private async Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken) where T : class
    {
        try
        {
            using var response = await client.GetAsync(url, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new ProviderUnavailableException($"O Provider respondeu {(int)response.StatusCode}.");
            }

            return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
                ?? throw new ProviderUnavailableException("O Provider respondeu sem corpo.");
        }
        catch (Exception exception) when (exception is HttpRequestException or ExecutionRejectedException or JsonException)
        {
            throw new ProviderUnavailableException("Falha ao chamar o Provider.", exception);
        }
    }
}
