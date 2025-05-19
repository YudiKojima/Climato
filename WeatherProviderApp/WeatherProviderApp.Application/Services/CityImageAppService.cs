using Microsoft.Extensions.Configuration;
using WeatherProviderApp.Application.Contracts;

namespace WeatherProviderApp.Application.Services
{
    public class CityImageAppService : ICityImageAppService
    {
        private readonly HttpClient client;
        private readonly string apiKey;

        public CityImageAppService(HttpClient httpClient, IConfiguration config)
        {
            client = httpClient;
            apiKey = config["Pexels:ApiKey"]!;
        }

        public async Task<string?> GetImageUrlAsync(string? cityName)
        {
            if (string.IsNullOrWhiteSpace(cityName))
            {
                return null;
            }

            var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.pexels.com/v1/search?query={cityName}&orientation=landscape&per_page=1");
            request.Headers.Add("Authorization", apiKey);

            var response = await client.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var result = await response.ReadContentAsDictionaryAsync();
            string? cityImage = null;

            if (result.TryGetValue("photos", out var photosObj) && photosObj is List<object> photos && photos.Count > 0)
            {
                if (photos[0] is Dictionary<string, object> photo &&
                    photo.TryGetValue("src", out var srcObj) &&
                    srcObj is Dictionary<string, object> src &&
                    src.TryGetValue("large2x", out var imageUrlObj))
                {
                    cityImage = imageUrlObj?.ToString();
                }
            }

            return cityImage;
        }
    }
}
