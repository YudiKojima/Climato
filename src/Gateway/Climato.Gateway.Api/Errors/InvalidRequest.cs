using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Climato.Gateway.Api.Errors;

public static class InvalidRequest
{
    private const string BodyKey = "body";

    public static ModelStateDictionary Sanitize(ModelStateDictionary modelState, IReadOnlyCollection<string> bodyParameterNames)
    {
        var result = new ModelStateDictionary();

        foreach (var (key, entry) in modelState)
        {
            if (entry.Errors.Count == 0)
            {
                continue;
            }

            if (key.Length == 0 || key.StartsWith('$') || bodyParameterNames.Contains(key))
            {
                if (!result.ContainsKey(BodyKey))
                {
                    result.AddModelError(BodyKey, "JSON inválido.");
                }

                continue;
            }

            result.AddModelError(key, "Valor inválido.");
        }

        return result;
    }
}
