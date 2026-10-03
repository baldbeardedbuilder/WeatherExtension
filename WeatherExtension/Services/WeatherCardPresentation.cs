// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CmdPal.Ext.Weather.Models;

namespace Microsoft.CmdPal.Ext.Weather.Services;

internal static class WeatherCardPresentation
{
	private static readonly ConcurrentDictionary<string, CompositeFormat> Formats = new(StringComparer.Ordinal);

	public static string Serialize(WeatherCardData data)
		=> JsonSerializer.Serialize(data, WeatherCardJsonContext.Default.WeatherCardData);

	public static WeatherCardData Empty(string locationName, string status) => new()
	{
		LocationName = locationName,
		Status = status,
		CurrentCondition = Resources.card_condition_unknown,
		Daylight = new() { Status = Resources.card_daylight_unavailable },
	};

	public static WeatherCardData Build(
		GeocodingResult location,
		WeatherData? weather,
		ForecastData? forecast,
		HourlyForecastData? hourly,
		string temperatureUnit,
		string windUnit,
		bool use24Hour,
		DateTimeOffset now)
	{
		var current = weather?.Current;
		var zoneId = new[] { weather?.Timezone, forecast?.Timezone, hourly?.Timezone, location.Timezone }
			.FirstOrDefault(id => !string.IsNullOrWhiteSpace(id));
		var zone = ResolveTimezone(zoneId, out var timezoneError);
		var localNow = zone == null ? (DateTime?)null : TimeZoneInfo.ConvertTime(now, zone).DateTime;
		var data = new WeatherCardData
		{
			LocationName = location.DisplayName,
			Status = current == null ? Resources.weather_service_error : string.Empty,
			CurrentTemp = current == null ? "--" : Temperature(current.Temperature, temperatureUnit),
			CurrentCondition = current == null ? Resources.card_condition_unknown : WeatherDescriptions.GetLocalized(current.WeatherCode),
			CurrentWeatherCode = current?.WeatherCode,
			IsNight = current?.IsDay == 0,
			FeelsLike = current == null ? "--" : Temperature(current.ApparentTemperature, temperatureUnit),
			Humidity = current == null ? "--" : WeatherFormatter.Humidity(current.RelativeHumidity),
			Wind = current == null || !double.IsFinite(current.WindSpeed) ? "--" : WeatherFormatter.Wind(current.WindSpeed, windUnit),
			Daylight = BuildDaylight(forecast?.Daily, zone, localNow, now, use24Hour),
		};
		if (timezoneError != null)
		{
			data.Diagnostics.Add(timezoneError);
		}

		if (localNow == null)
		{
			return data;
		}

		var daily = forecast?.Daily;
		var today = FindDate(daily?.Time, localNow.Value.Date);
		if (today >= 0)
		{
			data.TodayHighLow = HighLow(At(daily?.TemperatureMax, today), At(daily?.TemperatureMin, today), temperatureUnit);
		}

		for (var i = 0; i < (daily?.Time?.Count ?? 0) && data.Days.Count < 3; i++)
		{
			if (!TryTime(daily!.Time![i], out var date) || date.Date <= localNow.Value.Date)
			{
				continue;
			}

			var code = At(daily.WeatherCode, i);
			data.Days.Add(new()
			{
				Time = date.ToString("ddd", CultureInfo.CurrentCulture),
				WeatherCode = code,
				Condition = Condition(code),
				Temperature = HighLow(At(daily.TemperatureMax, i), At(daily.TemperatureMin, i), temperatureUnit),
				Rain = Rain(At(daily.PrecipitationProbabilityMax, i)),
			});
		}

		var hours = hourly?.Hourly;
		for (var i = 0; i < (hours?.Time?.Count ?? 0) && data.Hours.Count < 3; i++)
		{
			if (!TryTime(hours!.Time![i], out var time) || time <= localNow.Value)
			{
				continue;
			}

			var code = At(hours.WeatherCode, i);
			data.Hours.Add(new()
			{
				Time = WeatherFormatter.Hour(time, use24Hour),
				WeatherCode = code,
				Condition = Condition(code),
				Temperature = Temperature(At(hours.Temperature, i), temperatureUnit),
				Rain = Rain(At(hours.PrecipitationProbability, i)),
				IsNight = IsNight(time, daily, hourly?.Daily),
			});
		}

		if (data.Daylight.Phase == DaylightPhase.Unavailable)
		{
			data.Diagnostics.Add("Daylight times are missing or invalid for the location's current date.");
		}

		return data;
	}

	private static TimeZoneInfo? ResolveTimezone(string? id, out string? error)
	{
		error = null;
		if (string.IsNullOrWhiteSpace(id))
		{
			error = "Weather card timezone is missing; location-local forecasts cannot be selected.";
			return null;
		}

		try
		{
			return TimeZoneInfo.FindSystemTimeZoneById(id);
		}
		catch (TimeZoneNotFoundException ex)
		{
			error = $"Weather card timezone '{id}' is unavailable: {ex.Message}";
		}
		catch (InvalidTimeZoneException ex)
		{
			error = $"Weather card timezone '{id}' is invalid: {ex.Message}";
		}

		return null;
	}

	private static WeatherCardDaylight BuildDaylight(
		DailyForecast? daily, TimeZoneInfo? zone, DateTime? localNow, DateTimeOffset now, bool use24Hour)
	{
		var localTime = localNow?.ToString(use24Hour ? "HH:mm" : "h:mm tt", CultureInfo.CurrentCulture) ?? "--";
		var index = localNow == null ? -1 : FindDate(daily?.Time, localNow.Value.Date);
		if (zone == null || index < 0 ||
			!TryTime(At(daily?.Sunrise, index), out var sunrise) ||
			!TryTime(At(daily?.Sunset, index), out var sunset) ||
			sunrise.Date != localNow!.Value.Date || sunset.Date != sunrise.Date ||
			sunset <= sunrise || zone.IsInvalidTime(sunrise) || zone.IsInvalidTime(sunset) ||
			zone.IsAmbiguousTime(sunrise) || zone.IsAmbiguousTime(sunset))
		{
			return new() { LocalTime = localTime, Status = Resources.card_daylight_unavailable };
		}

		var sunriseUtc = TimeZoneInfo.ConvertTimeToUtc(sunrise, zone);
		var sunsetUtc = TimeZoneInfo.ConvertTimeToUtc(sunset, zone);
		var duration = sunsetUtc - sunriseUtc;
		var progress = Math.Clamp((now.UtcDateTime - sunriseUtc).TotalSeconds / duration.TotalSeconds, 0, 1);
		var phase = now.UtcDateTime < sunriseUtc ? DaylightPhase.BeforeSunrise :
			now.UtcDateTime >= sunsetUtc ? DaylightPhase.AfterSunset : DaylightPhase.Daytime;
		var status = phase switch
		{
			DaylightPhase.BeforeSunrise => Resources.card_before_sunrise,
			DaylightPhase.AfterSunset => Resources.card_after_sunset,
			_ => Resources.card_daytime,
		};
		var sunriseText = sunrise.ToString(use24Hour ? "HH:mm" : "h:mm tt", CultureInfo.CurrentCulture);
		var sunsetText = sunset.ToString(use24Hour ? "HH:mm" : "h:mm tt", CultureInfo.CurrentCulture);
		var durationText = Format(Resources.card_daylight_duration,
			(int)duration.TotalHours, duration.Minutes);
		return new()
		{
			Sunrise = sunriseText,
			Sunset = sunsetText,
			LocalTime = localTime,
			Duration = durationText,
			Status = status,
			Phase = phase,
			Progress = progress,
			AltText = Format(Resources.card_daylight_alt,
				sunriseText, sunsetText, durationText, status),
		};
	}

	private static bool IsNight(DateTime time, DailyForecast? daily, HourlyDailyInfo? hourlyDaily)
	{
		var index = FindDate(daily?.Time, time.Date);
		if (TryTime(At(daily?.Sunrise, index), out var sunrise) && TryTime(At(daily?.Sunset, index), out var sunset))
		{
			return time < sunrise || time >= sunset;
		}

		for (var i = 0; i < (hourlyDaily?.Sunrise?.Count ?? 0); i++)
		{
			if (TryTime(hourlyDaily!.Sunrise![i], out sunrise) && sunrise.Date == time.Date &&
				TryTime(At(hourlyDaily.Sunset, i), out sunset))
			{
				return time < sunrise || time >= sunset;
			}
		}

		return false;
	}

	private static int FindDate(List<string>? times, DateTime date)
	{
		for (var i = 0; i < (times?.Count ?? 0); i++)
		{
			if (TryTime(times![i], out var value) && value.Date == date)
			{
				return i;
			}
		}

		return -1;
	}

	private static bool TryTime(string? text, out DateTime time)
		=> DateTime.TryParseExact(text, ["yyyy-MM-dd", "yyyy-MM-dd'T'HH:mm", "yyyy-MM-dd'T'HH:mm:ss"],
			CultureInfo.InvariantCulture, DateTimeStyles.None, out time);

	private static T? At<T>(List<T>? values, int index) where T : struct
		=> values != null && index >= 0 && index < values.Count ? values[index] : null;

	private static string? At(List<string>? values, int index)
		=> values != null && index >= 0 && index < values.Count ? values[index] : null;

	private static string Temperature(double? value, string unit)
		=> value.HasValue && double.IsFinite(value.Value) ? WeatherFormatter.Temperature(value.Value, unit) : "--";

	private static string HighLow(double? high, double? low, string unit)
		=> $"{Temperature(high, unit)} / {Temperature(low, unit)}";

	private static string Condition(int? code)
		=> code.HasValue ? WeatherDescriptions.GetLocalized(code.Value) : Resources.card_condition_unknown;

	private static string Rain(int? probability)
		=> probability is >= 0 and <= 100
			? Format(Resources.card_rain_chance, probability.Value)
			: "--";

	private static string Format(string template, params object[] values)
		=> string.Format(CultureInfo.CurrentCulture, Formats.GetOrAdd(template, CompositeFormat.Parse), values);
}

[JsonSerializable(typeof(WeatherCardData))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class WeatherCardJsonContext : JsonSerializerContext
{
}
