// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Microsoft.CmdPal.Ext.Weather;

internal sealed class Icons
{
    internal static IconInfo WeatherIcon { get; } = new IconInfo("🌤️");

    // WMO Weather Code to Icon Mapping (Emoji)
    // Reference: https://open-meteo.com/en/docs

    // Clear sky
    internal static IconInfo ClearSky { get; } = new IconInfo("☀️");

    // Partly cloudy
    internal static IconInfo PartlyCloudy { get; } = new IconInfo("⛅");

    internal static IconInfo MainlyClear { get; } = new IconInfo("🌤️");

    internal static IconInfo Overcast { get; } = new IconInfo("☁️");

    // Fog
    internal static IconInfo Fog { get; } = new IconInfo("🌫️");

    // Drizzle
    internal static IconInfo Drizzle { get; } = new IconInfo("🌦️");

    internal static IconInfo DrizzleFreezing { get; } = new IconInfo("🌧️");

    // Rain
    internal static IconInfo Rain { get; } = new IconInfo("🌧️");

    internal static IconInfo RainFreezing { get; } = new IconInfo("🌧️");

    // Snow
    internal static IconInfo Snow { get; } = new IconInfo("❄️");

    // Rain showers
    internal static IconInfo RainShowers { get; } = new IconInfo("🌦️");

    // Snow showers
    internal static IconInfo SnowShowers { get; } = new IconInfo("🌨️");

    // Thunderstorm
    internal static IconInfo Thunderstorm { get; } = new IconInfo("⛈️");

    internal static IconInfo ThunderstormHail { get; } = new IconInfo("⛈️");

    internal static IconInfo GetIconForWeatherCode(int weatherCode)
    {
        return weatherCode switch
        {
            0 => ClearSky,
            1 or 2 => MainlyClear,
            3 => PartlyCloudy,
            45 or 48 => Fog,
            51 or 53 or 55 => Drizzle,
            56 or 57 => DrizzleFreezing,
            61 or 63 or 65 => Rain,
            66 or 67 => RainFreezing,
            71 or 73 or 75 or 77 => Snow,
            80 or 81 or 82 => RainShowers,
            85 or 86 => SnowShowers,
            95 => Thunderstorm,
            96 or 99 => ThunderstormHail,
            _ => WeatherIcon,
        };
    }

    internal static string GetWeatherDescription(int weatherCode)
        => Microsoft.CmdPal.Ext.Weather.Services.WeatherDescriptions.GetLocalized(weatherCode);
}
