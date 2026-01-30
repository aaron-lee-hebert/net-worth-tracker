using NetWorthTracker.Core.ViewModels;

namespace NetWorthTracker.Application.Interfaces;

/// <summary>
/// Service for generating financial forecasts and managing forecast assumptions.
/// </summary>
public interface IForecastService
{
    /// <summary>
    /// Generates forecast data including historical data and projections.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="forecastMonths">Number of months to forecast (default 60 months / 5 years)</param>
    Task<ForecastViewModel> GetForecastDataAsync(Guid userId, int forecastMonths = 60);

    /// <summary>
    /// Gets the user's current forecast assumptions.
    /// </summary>
    Task<ForecastAssumptionsViewModel> GetAssumptionsAsync(Guid userId);

    /// <summary>
    /// Saves custom forecast assumptions for a user.
    /// </summary>
    Task SaveAssumptionsAsync(Guid userId, ForecastAssumptionsViewModel assumptions);

    /// <summary>
    /// Resets the user's forecast assumptions to system defaults.
    /// </summary>
    Task ResetAssumptionsAsync(Guid userId);
}
