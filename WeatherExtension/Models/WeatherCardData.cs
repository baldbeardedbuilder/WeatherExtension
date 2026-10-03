// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Microsoft.CmdPal.Ext.Weather.Models;

internal sealed class WeatherCardData
{
	public string LocationName { get; init; } = string.Empty;
	public string Status { get; set; } = string.Empty;
	public string CurrentTemp { get; init; } = "--";
	public string CurrentCondition { get; init; } = string.Empty;
	public string CurrentImage { get; set; } = string.Empty;
	public string FeelsLike { get; init; } = "--";
	public string TodayHighLow { get; set; } = "--";
	public string Humidity { get; init; } = "--";
	public string Wind { get; init; } = "--";
	public WeatherCardDaylight Daylight { get; init; } = new();
	public List<WeatherCardForecast> Hours { get; } = [];
	public List<WeatherCardForecast> Days { get; } = [];
	public WeatherCardLabels Labels { get; } = new();

	[JsonIgnore]
	public int? CurrentWeatherCode { get; init; }

	[JsonIgnore]
	public bool IsNight { get; init; }

	[JsonIgnore]
	public List<string> Diagnostics { get; } = [];
}

internal sealed class WeatherCardDaylight
{
	public string Sunrise { get; init; } = "--";
	public string Sunset { get; init; } = "--";
	public string LocalTime { get; init; } = "--";
	public string Duration { get; init; } = "--";
	public string Status { get; init; } = string.Empty;
	public string Image { get; set; } = string.Empty;
	public string AltText { get; init; } = string.Empty;

	[JsonIgnore]
	public DaylightPhase Phase { get; init; }

	[JsonIgnore]
	public double Progress { get; init; }
}

internal enum DaylightPhase
{
	Unavailable,
	BeforeSunrise,
	Daytime,
	AfterSunset,
}

internal sealed class WeatherCardForecast
{
	public string Time { get; init; } = string.Empty;
	public string Condition { get; init; } = string.Empty;
	public string Temperature { get; init; } = "--";
	public string Rain { get; init; } = "--";
	public string Image { get; set; } = string.Empty;

	[JsonIgnore]
	public int? WeatherCode { get; init; }

	[JsonIgnore]
	public bool IsNight { get; init; }
}

internal sealed class WeatherCardLabels
{
	public string Daylight { get; } = Resources.card_daylight;
	public string Sunrise { get; } = Resources.card_sunrise;
	public string Sunset { get; } = Resources.card_sunset;
	public string LocalTime { get; } = Resources.card_local_time;
	public string FeelsLike { get; } = Resources.feels_like;
	public string HighLow { get; } = Resources.card_label_high_low;
	public string Humidity { get; } = Resources.humidity;
	public string Wind { get; } = Resources.wind_speed;
	public string Hours { get; } = Resources.card_section_next_three_hours;
	public string Days { get; } = Resources.card_section_three_day_forecast;
	public string ForecastUnavailable { get; } = Resources.card_forecast_unavailable;
}
