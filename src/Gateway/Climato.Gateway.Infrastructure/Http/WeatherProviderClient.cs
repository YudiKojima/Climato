using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Exceptions;
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

    public async Task<WeatherDto?> GetWeatherAsync(string cityName, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.GetAsync($"weathers?cityName={Uri.EscapeDataString(cityName)}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new ProviderUnavailableException($"O Provider respondeu {(int)response.StatusCode}.");
            }

            return await response.Content.ReadFromJsonAsync<WeatherDto>(cancellationToken)
                ?? throw new ProviderUnavailableException("O Provider respondeu sem corpo.");
        }
        catch (Exception exception) when (exception is HttpRequestException or ExecutionRejectedException or JsonException)
        {
            throw new ProviderUnavailableException("Falha ao chamar o Provider.", exception);
        }
    }
}
