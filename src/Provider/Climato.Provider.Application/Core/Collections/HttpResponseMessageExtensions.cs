namespace Climato.Provider.Application.Core.Collections;

public static class HttpResponseMessageExtensions
{
    public static async Task<JsonDictionary> ReadContentAsDictionaryAsync(this HttpResponseMessage message, CancellationToken cancellationToken)
    {
        var json = await message.Content.ReadAsStringAsync(cancellationToken);
        return new JsonDictionary(json);
    }
}
