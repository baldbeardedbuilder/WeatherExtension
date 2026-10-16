// Copyright (c) Bald Bearded Builder LLC
// Licensed under the MIT license. See LICENSE in the project root.

using Microsoft.CmdPal.Ext.Weather.Models;

namespace Microsoft.CmdPal.Ext.Weather.Services;

internal static class WeatherFlyoutForecasts
{
    internal sealed record Day(DateTime Date, int? Code, double? High, double? Low, int? Rain, DateTime? Sunrise, DateTime? Sunset);

    internal sealed record Hour(DateTime Time, int? Code, double? Temperature, double? FeelsLike, int? Rain, double? Wind, int? Humidity, bool IsNight);

    public static Day[] Days(WeatherFlyoutSnapshot snapshot, DateTime now)
    {
        var daily = snapshot.Forecast?.Daily;
        var days = new List<Day>();
        for (var i = 0; i < (daily?.Time?.Count ?? 0); i++)
        {
            var date = WeatherFlyoutSnapshot.TimeAt(daily!.Time, i);
            if (date is null || date.Value.Date < now.Date)
            {
                continue;
            }

            days.Add(new Day(date.Value, WeatherFlyoutSnapshot.At(daily.WeatherCode, i),
                WeatherFlyoutSnapshot.At(daily.TemperatureMax, i), WeatherFlyoutSnapshot.At(daily.TemperatureMin, i),
                WeatherFlyoutSnapshot.At(daily.PrecipitationProbabilityMax, i),
                WeatherFlyoutSnapshot.TimeAt(daily.Sunrise, i), WeatherFlyoutSnapshot.TimeAt(daily.Sunset, i)));
        }

        return days.OrderBy(day => day.Date).Take(5).ToArray();
    }

    public static Hour[] Hours(WeatherFlyoutSnapshot snapshot, DateTime now, int count)
    {
        var hourly = snapshot.Hourly?.Hourly;
        var hours = new List<Hour>();
        var days = Days(snapshot, now);
        for (var i = 0; i < (hourly?.Time?.Count ?? 0); i++)
        {
            var time = WeatherFlyoutSnapshot.TimeAt(hourly!.Time, i);
            if (time is null || time <= now || time > now.AddHours(24))
            {
                continue;
            }

            var day = days.FirstOrDefault(day => day.Date.Date == time.Value.Date);
            var night = day?.Sunrise is DateTime rise && day.Sunset is DateTime set &&
                (time < rise || time >= set);
            hours.Add(new Hour(time.Value, WeatherFlyoutSnapshot.At(hourly.WeatherCode, i),
                WeatherFlyoutSnapshot.At(hourly.Temperature, i), WeatherFlyoutSnapshot.At(hourly.ApparentTemperature, i),
                WeatherFlyoutSnapshot.At(hourly.PrecipitationProbability, i), WeatherFlyoutSnapshot.At(hourly.WindSpeed, i),
                WeatherFlyoutSnapshot.At(hourly.RelativeHumidity, i), night));
        }

        return hours.OrderBy(hour => hour.Time).Take(count).ToArray();
    }
}
