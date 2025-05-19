namespace WeatherProviderApp.Application.Contracts
{
    public interface ICityImageAppService
    {
        Task<string?> GetImageUrlAsync(string cityName);
    }
}
