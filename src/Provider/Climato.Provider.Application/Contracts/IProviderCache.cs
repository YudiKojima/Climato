namespace Climato.Provider.Application.Contracts;

public interface IProviderCache
{
    Task<T?> GetAsync<T>(string prefix, string text, CancellationToken cancellationToken) where T : class;
    Task SetAsync<T>(string prefix, string text, T value, TimeSpan duration, CancellationToken cancellationToken) where T : class;
}
