// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using BaldBeardedBuilder.WeatherExtension;
using Microsoft.CmdPal.Ext.Weather.Models;

namespace Microsoft.CmdPal.Ext.Weather.Services;
/// <summary>
/// Centralizes how weather values are turned into display strings.
/// Pages, dock bands and adaptive cards used to format temperatures,
/// humidity and wind inline, which made unit changes and culture fixes
/// require touch-ups in many places. Funnel them through here instead.
/// </summary>
internal static class WeatherFormatter
{
	public const string CelsiusUnit = "\u00B0C";
	public const string FahrenheitUnit = "\u00B0F";
	public const string CelsiusKey = "celsius";
	public const string KilometersPerHourKey = "kmh";
	public const string MilesPerHourKey = "mph";

	// CA1863: cache the composite format for the localized "feels like" string.
	// The template comes from Resources, so it varies per culture; CurrentSubtitle
	// rebuilds the cache when the culture changes.
	private static System.Text.CompositeFormat? _feelsLikeFormat;
	private static string? _feelsLikeFormatCulture;
	private static readonly Lock _feelsLikeSync = new();

	private static System.Text.CompositeFormat GetFeelsLikeFormat()
	{
		var current = CultureInfo.CurrentUICulture.Name;
		lock (_feelsLikeSync)
		{
			if (_feelsLikeFormat != null && _feelsLikeFormatCulture == current)
			{
				return _feelsLikeFormat;
			}

			_feelsLikeFormat = System.Text.CompositeFormat.Parse(Resources.feels_like_template);
			_feelsLikeFormatCulture = current;
			return _feelsLikeFormat;
		}
	}

	public static string TemperatureUnit(string temperatureUnit)
		=> string.Equals(temperatureUnit, CelsiusKey, StringComparison.OrdinalIgnoreCase)
			? CelsiusUnit
			: FahrenheitUnit;

	public static string WindSpeedUnit(string windSpeedUnit)
		=> string.Equals(windSpeedUnit, MilesPerHourKey, StringComparison.OrdinalIgnoreCase)
			? "mph"
			: "km/h";

	public static string Temperature(double value, string temperatureUnit, int decimals = 0)
	{
		var format = decimals <= 0 ? "F0" : $"F{decimals}";
		return string.Create(
			CultureInfo.CurrentCulture,
			$"{value.ToString(format, CultureInfo.CurrentCulture)}{TemperatureUnit(temperatureUnit)}");
	}

	public static string FeelsLike(double value, string temperatureUnit, int decimals = 0)
		=> Temperature(value, temperatureUnit, decimals);

	public static string HighLow(double high, double low, string temperatureUnit)
		=> string.Format(
			CultureInfo.CurrentCulture,
			"{0} / {1}",
			Temperature(high, temperatureUnit),
			Temperature(low, temperatureUnit));

	public static string Humidity(int? value)
		=> value.HasValue
			? string.Format(CultureInfo.CurrentCulture, "{0}%", value.Value)
			: "--";

	public static string Wind(double speed, string windSpeedUnit, int decimals = 1)
	{
		var format = decimals <= 0 ? "F0" : $"F{decimals}";
		return string.Format(
			CultureInfo.CurrentCulture,
			"{0} {1}",
			speed.ToString(format, CultureInfo.CurrentCulture),
			WindSpeedUnit(windSpeedUnit));
	}

	public static string PrecipitationProbability(int? probability)
		=> string.Format(CultureInfo.CurrentCulture, "{0}%", probability ?? 0);

	public static string ConditionWithTemperature(int weatherCode, double temperature, string temperatureUnit)
		=> string.Format(
			CultureInfo.CurrentCulture,
			"{0} \u2014 {1}",
			WeatherDescriptions.GetLocalized(weatherCode),
			Temperature(temperature, temperatureUnit));

	public static string CurrentSubtitle(CurrentWeather current, string temperatureUnit)
		=> string.Format(
			CultureInfo.CurrentCulture,
			GetFeelsLikeFormat(),
			WeatherDescriptions.GetLocalized(current.WeatherCode),
			Temperature(current.Temperature, temperatureUnit),
			Temperature(current.ApparentTemperature, temperatureUnit));

	public static string CompassDirection(int degrees)
	{
		// 8-point compass: round to nearest 45° wedge.
		var directions = new[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
		var index = ((int)Math.Round(degrees / 45.0) % 8 + 8) % 8;
		return directions[index];
	}

	/// <summary>
	/// Formats a hour-of-day for display, honoring the user's 12/24-hour
	/// preference. The 24-hour format uses the locale's pattern so e.g.
	/// Japanese gets "13時" while English gets "13:00".
	/// </summary>
	public static string Hour(DateTime time, bool use24Hour)
	{
		return time.ToString(
			use24Hour ? "HH:mm" : "h tt",
			CultureInfo.CurrentCulture);
	}
}
