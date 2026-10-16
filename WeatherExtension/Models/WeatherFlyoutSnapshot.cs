// Copyright (c) Bald Bearded Builder LLC
// Licensed under the MIT license. See LICENSE in the project root.

using System.Globalization;
using BaldBeardedBuilder.WeatherExtension;
using Microsoft.CommandPalette.Extensions;

namespace Microsoft.CmdPal.Ext.Weather.Models;

internal sealed record WeatherFlyoutSnapshot(
    GeocodingResult Location,
    WeatherData? Weather,
    ForecastData? Forecast,
    HourlyForecastData? Hourly,
    string TemperatureUnit,
    string WindUnit,
    bool Use24HourClock,
    DateTimeOffset FetchedAt)
{
    public DateTime? LocalTime(DateTimeOffset instant)
    {
        var zone = Weather?.Timezone ?? Forecast?.Timezone ?? Hourly?.Timezone;
        if (string.IsNullOrWhiteSpace(zone))
        {
            return null;
        }

        try
        {
            return TimeZoneInfo.ConvertTime(instant, TimeZoneInfo.FindSystemTimeZoneById(zone)).DateTime;
        }
        catch (TimeZoneNotFoundException ex)
        {
            WeatherLogger.LogToHost(MessageState.Warning, $"Weather time zone is unavailable: {ex.Message}");
        }
        catch (InvalidTimeZoneException ex)
        {
            WeatherLogger.LogToHost(MessageState.Warning, $"Weather time zone is invalid: {ex.Message}");
        }

        return null;
    }

    internal static T? At<T>(List<T>? values, int index) where T : struct =>
        values is not null && index >= 0 && index < values.Count ? values[index] : null;

    internal static DateTime? TimeAt(List<string>? values, int index) =>
        values is not null && index >= 0 && index < values.Count &&
        DateTime.TryParse(values[index], CultureInfo.InvariantCulture, DateTimeStyles.None, out var value)
            ? value : null;
}
