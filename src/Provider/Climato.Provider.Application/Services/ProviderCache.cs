using System.Globalization;
using System.Text;
using System.Text.Json;
using Climato.Provider.Application.Contracts;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Climato.Provider.Application.Services;

public sealed class ProviderCache : IProviderCache
{
    private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache cache;
    private readonly ILogger<ProviderCache> logger;

    public ProviderCache(IDistributedCache cache, ILogger<ProviderCache> logger)
    {
        this.cache = cache;
        this.logger = logger;
    }

    public async Task<T?> GetAsync<T>(string prefix, string text, CancellationToken cancellationToken) where T : class
    {
        try
        {
            var bytes = await cache.GetAsync(CreateKey(prefix, text), cancellationToken);
            return bytes == null ? null : JsonSerializer.Deserialize<T>(bytes, jsonOptions);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Falha ao ler o cache {Prefix} de {Text}; seguindo sem cache.", prefix, text);
            return null;
        }
    }

    public async Task SetAsync<T>(string prefix, string text, T value, TimeSpan duration, CancellationToken cancellationToken) where T : class
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, jsonOptions);
            var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = duration };
            await cache.SetAsync(CreateKey(prefix, text), bytes, options, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Falha ao gravar o cache {Prefix} de {Text}; seguindo sem cache.", prefix, text);
        }
    }

    private static string CreateKey(string prefix, string text)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var decomposed = string.Join(' ', words).ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return $"{prefix}:{builder.ToString().Normalize(NormalizationForm.FormC)}";
    }
}
