// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using BaldBeardedBuilder.WeatherExtension;

namespace Microsoft.CmdPal.Ext.Weather.Services;

/// <summary>
/// Looks up a localized human-readable description for a WMO weather code.
/// The full set of WMO codes (~28) is mapped onto a smaller set of broad
/// categories so we don't have to ship dozens of translated strings per
/// language. The icon already conveys intensity, so this stays light.
/// </summary>
internal static class WeatherDescriptions
{
	public static string GetLocalized(int weatherCode)
	{
		var key = weatherCode switch
		{
			0 => "weather_clear",
			1 => "weather_mainly_clear",
			2 => "weather_partly_cloudy",
			3 => "weather_overcast",
			45 or 48 => "weather_fog",
			51 or 53 or 55 or 56 or 57 => "weather_drizzle",
			61 or 63 or 65 or 66 or 67 => "weather_rain",
			71 or 73 or 75 or 77 => "weather_snow",
			80 or 81 or 82 => "weather_rain_showers",
			85 or 86 => "weather_snow_showers",
			95 or 96 or 99 => "weather_thunderstorm",
			_ => null,
		};

		if (key == null)
		{
			return Resources.card_condition_unknown;
		}

		// Pass CurrentUICulture explicitly so the analyzer is happy and the
		// behavior is unambiguous: we honor the user's UI language with the
		// neutral resx as fallback.
		return Resources.ResourceManager.GetString(key, CultureInfo.CurrentUICulture)
			?? Resources.card_condition_unknown;
	}
}
