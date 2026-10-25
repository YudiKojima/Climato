using Climato.Provider.Domain.Weathers;

namespace Climato.Provider.Application.Contracts;

public interface IForecastAppService
{
    Task<Forecast?> GetByCityAsync(string cityName, CancellationToken cancellationToken);
}
