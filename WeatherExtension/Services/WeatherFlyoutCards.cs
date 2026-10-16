// Copyright (c) Bald Bearded Builder LLC
// Licensed under the MIT license. See LICENSE in the project root.

using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using BaldBeardedBuilder.WeatherExtension;
using Microsoft.CmdPal.Ext.Weather.Models;
using Microsoft.CommandPalette.Extensions;

namespace Microsoft.CmdPal.Ext.Weather.Services;

internal static class WeatherFlyoutCards
{
    public static string Build(WeatherFlyoutView view, WeatherFlyoutSnapshot snapshot, DateTimeOffset instant)
    {
        var body = new JsonArray(FlyoutCard.Text(snapshot.Location.DisplayName, "large"));
        if (snapshot.Weather?.Current is not { } current)
        {
            body.Add(FlyoutCard.Text(Resources.weather_service_error));
            return FlyoutCard.Document(body);
        }

        body.Add(FlyoutCard.Columns(
            FlyoutCard.Column(FlyoutCard.Text(Emoji(current.WeatherCode, current.IsDay == 0), "extraLarge")),
            FlyoutCard.Column(FlyoutCard.Text(Temperature(current.Temperature, snapshot), "extraLarge")),
            FlyoutCard.Column(FlyoutCard.Text(Icons.GetWeatherDescription(current.WeatherCode)),
                FlyoutCard.Text($"{Resources.feels_like}: {Temperature(current.ApparentTemperature, snapshot)}", subtle: true))));

        var now = snapshot.LocalTime(instant);
        var days = now.HasValue ? WeatherFlyoutForecasts.Days(snapshot, now.Value) : [];
        var today = days.FirstOrDefault(day => day.Date.Date == now?.Date);

        if (view is WeatherFlyoutView.Glance or WeatherFlyoutView.DailyOutlook)
        {
            body.Add(FlyoutCard.Columns(
                FlyoutCard.Column(FlyoutCard.Text(Resources.high, "small", true), FlyoutCard.Text(Temperature(today?.High, snapshot))),
                FlyoutCard.Column(FlyoutCard.Text(Resources.low, "small", true), FlyoutCard.Text(Temperature(today?.Low, snapshot))),
                FlyoutCard.Column(FlyoutCard.Text(Resources.wind_speed, "small", true), FlyoutCard.Text(WeatherFormatter.Wind(current.WindSpeed, snapshot.WindUnit))),
                FlyoutCard.Column(FlyoutCard.Text(Resources.humidity, "small", true), FlyoutCard.Text(WeatherFormatter.Humidity(current.RelativeHumidity)))));
        }

        if (view != WeatherFlyoutView.Glance && now is null)
        {
            body.Add(FlyoutCard.Text(FlyoutText.Get("TimeZoneUnavailable")));
        }
        else if (view == WeatherFlyoutView.NextHours)
        {
            body.Add(FlyoutCard.Text(FlyoutText.FormatTemplate(Resources.card_section_next_hours, 6)));
            var hours = WeatherFlyoutForecasts.Hours(snapshot, now!.Value, 6);
            if (hours.Length == 0)
            {
                body.Add(FlyoutCard.Text(Resources.unavailable));
            }
            else
            {
                body.Add(FlyoutCard.Columns(hours.Select(hour => FlyoutCard.Column(
                    FlyoutCard.Text(WeatherFormatter.Hour(hour.Time, snapshot.Use24HourClock), "small", true),
                    FlyoutCard.Text(Emoji(hour.Code, hour.IsNight), "large"),
                    FlyoutCard.Text(Temperature(hour.Temperature, snapshot)),
                    FlyoutCard.Text(Percent(hour.Rain), "small", true))).ToArray()));
            }

            body.Add(FlyoutCard.Text($"{FlyoutText.Get("Sunset")}: {Clock(today?.Sunset, snapshot)}", "small", true));
        }
        else if (view == WeatherFlyoutView.DailyOutlook)
        {
            body.Add(FlyoutCard.Text(FlyoutText.FormatTemplate(Resources.card_section_day_forecast, 5)));
            if (days.Length == 0)
            {
                body.Add(FlyoutCard.Text(Resources.unavailable));
            }

            foreach (var day in days)
            {
                body.Add(FlyoutCard.Columns(
                    FlyoutCard.Column(FlyoutCard.Text(day.Date.Date == now!.Value.Date ? Resources.today : day.Date.ToString("ddd", CultureInfo.CurrentCulture))),
                    FlyoutCard.Column(FlyoutCard.Text(Emoji(day.Code))),
                    FlyoutCard.Column(FlyoutCard.Text(Percent(day.Rain))),
                    FlyoutCard.Column(FlyoutCard.Text($"{Temperature(day.Low, snapshot)} / {Temperature(day.High, snapshot)}"))));
            }
        }
        else if (view == WeatherFlyoutView.DaylightArc)
        {
            body.Add(FlyoutCard.Text(FlyoutText.Get("Daylight")));
            if (today?.Sunrise is DateTime rise && today.Sunset is DateTime set &&
                rise.Date == now!.Value.Date && set.Date == rise.Date && set > rise)
            {
                var remaining = now < rise ? FlyoutText.Format("UntilSunrise", Duration(rise - now.Value))
                    : now < set ? FlyoutText.Format("UntilSunset", Duration(set - now.Value)) : FlyoutText.Get("AfterSunset");
                body.Add(FlyoutCard.Text(remaining, "large"));
                try
                {
                    body.Add(FlyoutCard.Image(DaylightArcImage.Create(now.Value, rise, set), FlyoutText.Get("ArcDescription")));
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ExternalException)
                {
                    WeatherLogger.LogToHost(MessageState.Error, $"Daylight arc image failed: {ex.Message}");
                    body.Add(FlyoutCard.Text(Resources.unavailable));
                }
                body.Add(FlyoutCard.Columns(
                    FlyoutCard.Column(FlyoutCard.Text(FlyoutText.Get("Sunrise"), "small", true), FlyoutCard.Text(Clock(rise, snapshot))),
                    FlyoutCard.Column(FlyoutCard.Text(FlyoutText.Get("Sunset"), "small", true), FlyoutCard.Text(Clock(set, snapshot)))));
                body.Add(FlyoutCard.Text(FlyoutText.Format("TotalDaylight", Duration(set - rise)), "small", true));
            }
            else
            {
                body.Add(FlyoutCard.Text(FlyoutText.Get("DaylightUnavailable")));
            }
        }

        if (DateTime.TryParse(current.Time, CultureInfo.InvariantCulture, DateTimeStyles.None, out var updated))
        {
            var timestamp = updated.Date == now?.Date ? Clock(updated, snapshot) : updated.ToString("g", CultureInfo.CurrentCulture);
            body.Add(FlyoutCard.Text(FlyoutText.Format("Updated", timestamp), "small", true));
        }

        return FlyoutCard.Document(body);
    }

    internal static string Emoji(int? code, bool night = false) =>
        code.HasValue ? Icons.GetIconForWeatherCode(code.Value, night).Dark.Icon ?? "--" : "--";

    internal static string Temperature(double? value, WeatherFlyoutSnapshot snapshot) =>
        value.HasValue && double.IsFinite(value.Value) ? WeatherFormatter.Temperature(value.Value, snapshot.TemperatureUnit) : "--";

    internal static string Percent(int? value) => value.HasValue ? WeatherFormatter.Humidity(value) : "--";

    internal static string Clock(DateTime? value, WeatherFlyoutSnapshot snapshot) =>
        value?.ToString(snapshot.Use24HourClock ? "HH:mm" : "h:mm tt", CultureInfo.CurrentCulture) ?? "--";

    internal static string Duration(TimeSpan duration)
    {
        var minutes = Math.Max(0, (int)Math.Ceiling(duration.TotalMinutes));
        return FlyoutText.Format("Duration", minutes / 60, minutes % 60);
    }
}
