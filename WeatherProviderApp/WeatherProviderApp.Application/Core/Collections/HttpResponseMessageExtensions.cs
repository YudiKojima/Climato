using WeatherProviderApp.Application.Core.Collections;

public static class HttpResponseMessageExtensions
{
    public static async Task<JsonDictionary> ReadContentAsDictionaryAsync(this HttpResponseMessage message)
    {
        var json = await message.Content.ReadAsStringAsync();
        return new JsonDictionary(json);
    }
}
