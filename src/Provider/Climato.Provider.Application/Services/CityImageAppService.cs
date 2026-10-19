using System.Net.Http.Json;
using System.Text.Json;
using Climato.Provider.Application.Contracts;
using Climato.Provider.Application.Model.Pexels;
using Microsoft.Extensions.Logging;
using Polly;

namespace Climato.Provider.Application.Services;

public sealed class CityImageAppService : ICityImageAppService
{
    private readonly HttpClient client;
    private readonly ILogger<CityImageAppService> logger;

    public CityImageAppService(HttpClient httpClient, ILogger<CityImageAppService> logger)
    {
        client = httpClient;
        this.logger = logger;
    }

    public async Task<string?> GetImageUrlAsync(string cityName, CancellationToken cancellationToken)
    {
        var url = $"v1/search?query={Uri.EscapeDataString(cityName)}&orientation=landscape&per_page=1";

        try
        {
            using var response = await client.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Pexels respondeu {StatusCode} para {CityName}; clima segue sem imagem.", (int)response.StatusCode, cityName);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<PexelsSearchResponse>(cancellationToken);
            return result?.Photos.FirstOrDefault()?.Src.Large2x;
        }
        catch (Exception exception) when (exception is HttpRequestException or ExecutionRejectedException or JsonException)
        {
            logger.LogWarning(exception, "Falha ao buscar a imagem de {CityName} no Pexels; clima segue sem imagem.", cityName);
            return null;
        }
    }
}
