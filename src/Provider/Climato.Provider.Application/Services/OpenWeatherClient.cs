using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Climato.Provider.Application.Contracts;
using Climato.Provider.Application.Exceptions;
using Climato.Provider.Application.Model.OpenWeather;
using Climato.Provider.Application.Options;
using Microsoft.Extensions.Options;
using Polly;

namespace Climato.Provider.Application.Services;

public sealed class OpenWeatherClient : IOpenWeatherClient
{
    private const string ServiceName = "OpenWeather";

    private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly HttpClient client;
    private readonly string apiKey;

    public OpenWeatherClient(HttpClient httpClient, IOptions<OpenWeatherOptions> options)
    {
        client = httpClient;
        apiKey = options.Value.ApiKey;
    }

    public Task<OpenWeatherCurrentResponse?> GetCurrentAsync(string cityName, CancellationToken cancellationToken)
    {
        var url = $"data/2.5/weather?q={Uri.EscapeDataString(cityName)}&appid={apiKey}&units=metric&lang=pt_br";
        return GetAsync<OpenWeatherCurrentResponse>(url, cancellationToken);
    }

    public Task<OpenWeatherForecastResponse?> GetForecastAsync(string cityName, CancellationToken cancellationToken)
    {
        var url = $"data/2.5/forecast?q={Uri.EscapeDataString(cityName)}&appid={apiKey}&units=metric&lang=pt_br";
        return GetAsync<OpenWeatherForecastResponse>(url, cancellationToken);
    }

    public async Task<IReadOnlyList<OpenWeatherLocationResponse>> GetLocationsAsync(string query, CancellationToken cancellationToken)
    {
        var url = $"geo/1.0/direct?q={Uri.EscapeDataString(query)}&limit=5&appid={apiKey}";
        return await GetAsync<List<OpenWeatherLocationResponse>>(url, cancellationToken) ?? [];
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
                throw new ExternalServiceUnavailableException(ServiceName, $"status {(int)response.StatusCode}");
            }

            return await response.Content.ReadFromJsonAsync<T>(jsonOptions, cancellationToken)
                ?? throw new ExternalServiceUnavailableException(ServiceName, "resposta vazia");
        }
        catch (Exception exception) when (exception is HttpRequestException or ExecutionRejectedException or JsonException)
        {
            throw new ExternalServiceUnavailableException(ServiceName, "falha na chamada", exception);
        }
    }
}
