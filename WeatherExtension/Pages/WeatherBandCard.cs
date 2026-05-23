// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BaldBeardedBuilder.WeatherExtension;
using Microsoft.CmdPal.Ext.Weather.Models;
using Microsoft.CmdPal.Ext.Weather.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System.Globalization;

namespace Microsoft.CmdPal.Ext.Weather.Pages;

internal sealed partial class WeatherBandCard : ContentPage, IDisposable
{
	private readonly OpenMeteoService _weatherService;
	private readonly IGeocodingService _geocodingService;
	private readonly WeatherSettingsManager _settings;
	private readonly FavoritesManager? _favoritesManager;
	private readonly FormContent _weatherForm = new();
	private readonly CancellationTokenSource _cts = new();
	private readonly GeocodingResult? _fixedLocation;
	private readonly WeatherSettingsPage? _settingsPage;

	public WeatherBandCard(
		OpenMeteoService weatherService,
		IGeocodingService geocodingService,
		WeatherSettingsManager settings,
		FavoritesManager? favoritesManager = null,
		GeocodingResult? fixedLocation = null,
		WeatherSettingsPage? settingsPage = null)
	{
		_weatherService = weatherService ?? throw new ArgumentNullException(nameof(weatherService));
		_geocodingService = geocodingService ?? throw new ArgumentNullException(nameof(geocodingService));
		_settings = settings ?? throw new ArgumentNullException(nameof(settings));
		_favoritesManager = favoritesManager;
		_fixedLocation = fixedLocation;

		Id = "com.baldbeardedbuilder.cmdpal.weather.card";
		Name = Resources.plugin_name;
		Title = Resources.plugin_name;
		Icon = Icons.WeatherIcon;

		_weatherForm.TemplateJson = GetCardTemplate();
		_weatherForm.DataJson = GetLoadingData();

		_settingsPage = settingsPage;

		_settings.Settings.SettingsChanged += OnSettingsChanged;
		if (_favoritesManager != null)
		{
			_favoritesManager.FavoritesChanged += OnFavoritesChanged;
		}

		_ = LoadWeatherDataAsync();
	}

	public override IContent[] GetContent() => [_weatherForm];

	internal async Task LoadWeatherDataAsync()
	{
		try
		{
			GeocodingResult location;

			if (_fixedLocation != null)
			{
				location = _fixedLocation;
			}
			else if (_favoritesManager != null)
			{
				var favorites = _favoritesManager.GetFavorites();
				if (favorites.Count == 0)
				{
					_weatherForm.DataJson = GetErrorData(
						Resources.no_favorites_hint);
					RaiseItemsChanged();
					return;
				}

				location = favorites[0].ToGeocodingResult();
			}
			else
			{
				_weatherForm.DataJson = GetErrorData(
					Resources.location_not_found);
				RaiseItemsChanged();
				return;
			}
			var weather = await _weatherService.GetCurrentWeatherAsync(
				location.Latitude,
				location.Longitude,
				_settings.TemperatureUnit,
				_settings.WindSpeedUnit,
				_cts.Token);

			var forecast = await _weatherService.GetForecastAsync(
				location.Latitude,
				location.Longitude,
				_settings.TemperatureUnit,
				_cts.Token);

			var hourly = await _weatherService.GetHourlyForecastAsync(
				location.Latitude,
				location.Longitude,
				_settings.TemperatureUnit,
				_settings.WindSpeedUnit,
				_cts.Token);

			_weatherForm.DataJson = BuildWeatherData(location, weather, forecast, hourly);
			
			RaiseItemsChanged();
		}
		catch (Exception ex)
		{
			WeatherLogger.LogToHost(
				MessageState.Error,
				$"Content page weather load error: {ex.Message}");

			_weatherForm.DataJson = GetErrorData(
				Resources.unavailable);
				
			RaiseItemsChanged();
		}
	}

	private string BuildWeatherData(
		GeocodingResult location,
		WeatherData? weather,
		ForecastData? forecast,
		HourlyForecastData? hourly)
	{
		var tempUnit = _settings.TemperatureUnit == "celsius" ? "\u00B0C" : "\u00B0F";
		var windUnit = _settings.WindSpeedUnit == "mph" ? "mph" : "km/h";

		var currentTemp = "--";
		var currentCondition = Resources.card_condition_unknown;
		var currentIcon = "\uD83C\uDF24\uFE0F";
		var feelsLike = "--";
		var humidity = "--";
		var wind = "--";
		var todayHighLow = "--";

		if (weather?.Current != null)
		{
			var c = weather.Current;
			currentTemp = $"{c.Temperature:F0}{tempUnit}";
			currentCondition = Icons.GetWeatherDescription(c.WeatherCode);
			currentIcon = GetEmojiForWeatherCode(c.WeatherCode);
			feelsLike = $"{c.ApparentTemperature:F0}{tempUnit}";
			humidity = $"{c.RelativeHumidity}%";
			wind = $"{c.WindSpeed:F1} {windUnit}";
		}

		if (forecast?.Daily?.TemperatureMax?.Count > 0 &&
			forecast.Daily.TemperatureMin?.Count > 0)
		{
			var high = forecast.Daily.TemperatureMax[0];
			var low = forecast.Daily.TemperatureMin[0];
			todayHighLow = $"{high:F0}{tempUnit} / {low:F0}{tempUnit}";
		}

		var hour1Time = "--";
		var hour1Icon = currentIcon;
		var hour1Temp = "--";
		var hour1Precip = "--";
		var hour2Time = "--";
		var hour2Icon = currentIcon;
		var hour2Temp = "--";
		var hour2Precip = "--";
		var hour3Time = "--";
		var hour3Icon = currentIcon;
		var hour3Temp = "--";
		var hour3Precip = "--";

		if (hourly?.Hourly != null)
		{
			var now = DateTime.Now;
			var hoursAdded = 0;

			for (var i = 0; i < (hourly.Hourly.Time?.Count ?? 0) && hoursAdded < 3; i++)
			{
				var timeStr = hourly.Hourly.Time?[i];
				if (timeStr == null || !DateTime.TryParse(timeStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var hourTime))
				{
					continue;
				}

				if (hourTime <= now)
				{
					continue;
				}

				var time = WeatherFormatter.Hour(hourTime, _settings.Use24HourClock);
				var weatherCode = hourly.Hourly.WeatherCode?[i] ?? 0;
				var icon = GetEmojiForWeatherCode(weatherCode);
				var temp = $"{hourly.Hourly.Temperature?[i]:F0}{tempUnit}";
				var precip = $"{hourly.Hourly.PrecipitationProbability?[i] ?? 0}%";

				if (hoursAdded == 0)
				{
					hour1Time = time;
					hour1Icon = icon;
					hour1Temp = temp;
					hour1Precip = precip;
				}
				else if (hoursAdded == 1)
				{
					hour2Time = time;
					hour2Icon = icon;
					hour2Temp = temp;
					hour2Precip = precip;
				}
				else if (hoursAdded == 2)
				{
					hour3Time = time;
					hour3Icon = icon;
					hour3Temp = temp;
					hour3Precip = precip;
				}

				hoursAdded++;
			}
		}

		var day1Name = "--";
		var day1Icon = currentIcon;
		var day1Condition = "--";
		var day1HighLow = "--";
		var day2Name = "--";
		var day2Icon = currentIcon;
		var day2Condition = "--";
		var day2HighLow = "--";
		var day3Name = "--";
		var day3Icon = currentIcon;
		var day3Condition = "--";
		var day3HighLow = "--";

		if (forecast?.Daily != null && forecast.Daily.Time?.Count >= 4)
		{
			for (var i = 1; i <= 3; i++)
			{
				if (forecast.Daily.Time == null || i >= forecast.Daily.Time.Count)
				{
					break;
				}

				var dateStr = forecast.Daily.Time[i];
				if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
				{
					var dayName = date.ToString("ddd", CultureInfo.CurrentCulture);
					var weatherCode = forecast.Daily.WeatherCode?[i] ?? 0;
					var icon = GetEmojiForWeatherCode(weatherCode);
					var condition = Icons.GetWeatherDescription(weatherCode);
					var high = forecast.Daily.TemperatureMax?[i] ?? 0;
					var low = forecast.Daily.TemperatureMin?[i] ?? 0;
					var highLow = $"{high:F0}{tempUnit} / {low:F0}{tempUnit}";

					if (i == 1)
					{
						day1Name = dayName;
						day1Icon = icon;
						day1Condition = condition;
						day1HighLow = highLow;
					}
					else if (i == 2)
					{
						day2Name = dayName;
						day2Icon = icon;
						day2Condition = condition;
						day2HighLow = highLow;
					}
					else if (i == 3)
					{
						day3Name = dayName;
						day3Icon = icon;
						day3Condition = condition;
						day3HighLow = highLow;
					}
				}
			}
		}

		var locationName = location.DisplayName;

		return $$"""
        {
            "locationName": "{{locationName}}",
            "currentIcon": "{{currentIcon}}",
            "currentTemp": "{{currentTemp}}",
            "currentCondition": "{{currentCondition}}",
            "feelsLike": "{{feelsLike}}",
            "todayHighLow": "{{todayHighLow}}",
            "humidity": "{{humidity}}",
            "wind": "{{wind}}",
            "hour1Time": "{{hour1Time}}",
            "hour1Icon": "{{hour1Icon}}",
            "hour1Temp": "{{hour1Temp}}",
            "hour1Precip": "{{hour1Precip}}",
            "hour2Time": "{{hour2Time}}",
            "hour2Icon": "{{hour2Icon}}",
            "hour2Temp": "{{hour2Temp}}",
            "hour2Precip": "{{hour2Precip}}",
            "hour3Time": "{{hour3Time}}",
            "hour3Icon": "{{hour3Icon}}",
            "hour3Temp": "{{hour3Temp}}",
            "hour3Precip": "{{hour3Precip}}",
            "day1Name": "{{day1Name}}",
            "day1Icon": "{{day1Icon}}",
            "day1Condition": "{{day1Condition}}",
            "day1HighLow": "{{day1HighLow}}",
            "day2Name": "{{day2Name}}",
            "day2Icon": "{{day2Icon}}",
            "day2Condition": "{{day2Condition}}",
            "day2HighLow": "{{day2HighLow}}",
            "day3Name": "{{day3Name}}",
            "day3Icon": "{{day3Icon}}",
            "day3Condition": "{{day3Condition}}",
            "day3HighLow": "{{day3HighLow}}"
        }
        """;
	}

	private static string GetEmojiForWeatherCode(int weatherCode)
	{
		return weatherCode switch
		{
			0 => "\u2600\uFE0F",
			1 or 2 => "\uD83C\uDF24\uFE0F",
			3 => "\u26C5",
			45 or 48 => "\uD83C\uDF2B\uFE0F",
			51 or 53 or 55 => "\uD83C\uDF26\uFE0F",
			56 or 57 => "\uD83C\uDF27\uFE0F",
			61 or 63 or 65 => "\uD83C\uDF27\uFE0F",
			66 or 67 => "\uD83C\uDF27\uFE0F",
			71 or 73 or 75 or 77 => "\u2744\uFE0F",
			80 or 81 or 82 => "\uD83C\uDF26\uFE0F",
			85 or 86 => "\uD83C\uDF28\uFE0F",
			95 => "\u26C8\uFE0F",
			96 or 99 => "\u26C8\uFE0F",
			_ => "\uD83C\uDF24\uFE0F",
		};
	}

	private static string GetLoadingData()
	{
		return $$"""
        {
            "locationName": "{{Resources.loading}}",
            "currentIcon": "🌤️",
            "currentTemp": "--",
            "currentCondition": "{{Resources.loading_data}}",
            "feelsLike": "--",
            "todayHighLow": "--",
            "humidity": "--",
            "wind": "--",
            "hour1Time": "--",
            "hour1Icon": "🌤️",
            "hour1Temp": "--",
            "hour1Precip": "--",
            "hour2Time": "--",
            "hour2Icon": "🌤️",
            "hour2Temp": "--",
            "hour2Precip": "--",
            "hour3Time": "--",
            "hour3Icon": "🌤️",
            "hour3Temp": "--",
            "hour3Precip": "--",
            "day1Name": "--",
            "day1Icon": "🌤️",
            "day1Condition": "--",
            "day1HighLow": "--",
            "day2Name": "--",
            "day2Icon": "🌤️",
            "day2Condition": "--",
            "day2HighLow": "--",
            "day3Name": "--",
            "day3Icon": "🌤️",
            "day3Condition": "--",
            "day3HighLow": "--"
        }
        """;
	}

	private static string GetErrorData(string errorMessage)
	{
		return $$"""
        {
            "locationName": "{{errorMessage}}",
            "currentIcon": "⚠️",
            "currentTemp": "--",
            "currentCondition": "{{errorMessage}}",
            "feelsLike": "--",
            "todayHighLow": "--",
            "humidity": "--",
            "wind": "--",
            "hour1Time": "--",
            "hour1Icon": "🌤️",
            "hour1Temp": "--",
            "hour1Precip": "--",
            "hour2Time": "--",
            "hour2Icon": "🌤️",
            "hour2Temp": "--",
            "hour2Precip": "--",
            "hour3Time": "--",
            "hour3Icon": "🌤️",
            "hour3Temp": "--",
            "hour3Precip": "--",
            "day1Name": "--",
            "day1Icon": "🌤️",
            "day1Condition": "--",
            "day1HighLow": "--",
            "day2Name": "--",
            "day2Icon": "🌤️",
            "day2Condition": "--",
            "day2HighLow": "--",
            "day3Name": "--",
            "day3Icon": "🌤️",
            "day3Condition": "--",
            "day3HighLow": "--"
        }
        """;
	}

	private static string GetCardTemplate()
	{
		return $$"""
        {
            "type": "AdaptiveCard",
            "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
            "version": "1.5",
            "body": [
                {
                    "type": "TextBlock",
                    "text": "${locationName}",
                    "size": "large",
                    "weight": "bolder"
                },
                {
                    "type": "ColumnSet",
                    "spacing": "large",
                    "columns": [
                        {
                            "type": "Column",
                            "width": "3",
                            "items": [
                                {
                                    "type": "TextBlock",
                                    "text": "{{Resources.card_section_current}}",
                                    "weight": "bolder",
                                    "size": "medium"
                                }
                            ]
                        },
                        {
                            "type": "Column",
                            "width": "3",
                            "spacing": "medium",
                            "items": [
                                {
                                    "type": "TextBlock",
                                    "text": "{{Resources.card_section_next_three_hours}}",
                                    "weight": "bolder",
                                    "size": "medium"
                                }
                            ]
                        }
                    ]
                },
                {
                    "type": "ColumnSet",
                    "columns": [
                        {
                            "type": "Column",
                            "width": "3",
                            "style": "emphasis",
                            "items": [
                                {
                                    "type": "ColumnSet",
                                    "columns": [
                                        {
                                            "type": "Column",
                                            "width": "auto",
                                            "verticalContentAlignment": "center",
                                            "horizontalAlignment": "center",
                                            "items": [
                                                {
                                                    "type": "TextBlock",
                                                    "text": "${currentIcon}",
                                                    "size": "extraLarge",
                                                    "horizontalAlignment": "center"
                                                },
                                                {
                                                    "type": "TextBlock",
                                                    "text": "${currentTemp}",
                                                    "size": "extraLarge",
                                                    "weight": "bolder",
                                                    "horizontalAlignment": "center",
                                                    "spacing": "none"
                                                },
                                                {
                                                    "type": "TextBlock",
                                                    "text": "${currentCondition}",
                                                    "horizontalAlignment": "center",
                                                    "wrap": true,
                                                    "spacing": "none",
                                                    "isSubtle": true
                                                }
                                            ]
                                        },
                                        {
                                            "type": "Column",
                                            "width": "stretch",
                                            "spacing": "large",
                                            "verticalContentAlignment": "center",
                                            "items": [
                                                {
                                                    "type": "FactSet",
                                                    "facts": [
                                                        { "title": "{{Resources.feels_like}}", "value": "${feelsLike}" },
                                                        { "title": "{{Resources.card_label_high_low}}", "value": "${todayHighLow}" },
                                                        { "title": "{{Resources.humidity}}", "value": "${humidity}" },
                                                        { "title": "{{Resources.wind_speed}}", "value": "${wind}" }
                                                    ]
                                                }
                                            ]
                                        }
                                    ]
                                }
                            ]
                        },
                        {
                            "type": "Column",
                            "width": "1",
                            "spacing": "medium",
                            "style": "emphasis",
                            "verticalContentAlignment": "center",
                            "items": [
                                { "type": "TextBlock", "text": "${hour1Time}", "weight": "bolder", "horizontalAlignment": "center" },
                                { "type": "TextBlock", "text": "${hour1Icon}", "horizontalAlignment": "center", "size": "large", "spacing": "small" },
                                { "type": "TextBlock", "text": "${hour1Temp} / ${hour1Precip}", "horizontalAlignment": "center", "spacing": "small" }
                            ]
                        },
                        {
                            "type": "Column",
                            "width": "1",
                            "spacing": "small",
                            "style": "emphasis",
                            "verticalContentAlignment": "center",
                            "items": [
                                { "type": "TextBlock", "text": "${hour2Time}", "weight": "bolder", "horizontalAlignment": "center" },
                                { "type": "TextBlock", "text": "${hour2Icon}", "horizontalAlignment": "center", "size": "large", "spacing": "small" },
                                { "type": "TextBlock", "text": "${hour2Temp} / ${hour2Precip}", "horizontalAlignment": "center", "spacing": "small" }
                            ]
                        },
                        {
                            "type": "Column",
                            "width": "1",
                            "spacing": "small",
                            "style": "emphasis",
                            "verticalContentAlignment": "center",
                            "items": [
                                { "type": "TextBlock", "text": "${hour3Time}", "weight": "bolder", "horizontalAlignment": "center" },
                                { "type": "TextBlock", "text": "${hour3Icon}", "horizontalAlignment": "center", "size": "large", "spacing": "small" },
                                { "type": "TextBlock", "text": "${hour3Temp} / ${hour3Precip}", "horizontalAlignment": "center", "spacing": "small" }
                            ]
                        }
                    ]
                },
                {
                    "type": "TextBlock",
                    "text": "{{Resources.card_section_three_day_forecast}}",
                    "size": "medium",
                    "weight": "bolder",
                    "separator": true,
                    "spacing": "large"
                },
                {
                    "type": "ColumnSet",
                    "columns": [
                        {
                            "type": "Column",
                            "width": "stretch",
                            "style": "emphasis",
                            "verticalContentAlignment": "top",
                            "items": [
                                { "type": "TextBlock", "text": "${day1Name}", "weight": "bolder", "horizontalAlignment": "center" },
                                { "type": "TextBlock", "text": "${day1Icon}", "horizontalAlignment": "center", "size": "large", "spacing": "small" },
                                { "type": "TextBlock", "text": "${day1Condition}", "horizontalAlignment": "center", "isSubtle": true, "wrap": true, "size": "small", "spacing": "small" },
                                { "type": "TextBlock", "text": "${day1HighLow}", "horizontalAlignment": "center", "spacing": "small" }
                            ]
                        },
                        {
                            "type": "Column",
                            "width": "stretch",
                            "style": "emphasis",
                            "spacing": "medium",
                            "verticalContentAlignment": "top",
                            "items": [
                                { "type": "TextBlock", "text": "${day2Name}", "weight": "bolder", "horizontalAlignment": "center" },
                                { "type": "TextBlock", "text": "${day2Icon}", "horizontalAlignment": "center", "size": "large", "spacing": "small" },
                                { "type": "TextBlock", "text": "${day2Condition}", "horizontalAlignment": "center", "isSubtle": true, "wrap": true, "size": "small", "spacing": "small" },
                                { "type": "TextBlock", "text": "${day2HighLow}", "horizontalAlignment": "center", "spacing": "small" }
                            ]
                        },
                        {
                            "type": "Column",
                            "width": "stretch",
                            "style": "emphasis",
                            "spacing": "medium",
                            "verticalContentAlignment": "top",
                            "items": [
                                { "type": "TextBlock", "text": "${day3Name}", "weight": "bolder", "horizontalAlignment": "center" },
                                { "type": "TextBlock", "text": "${day3Icon}", "horizontalAlignment": "center", "size": "large", "spacing": "small" },
                                { "type": "TextBlock", "text": "${day3Condition}", "horizontalAlignment": "center", "isSubtle": true, "wrap": true, "size": "small", "spacing": "small" },
                                { "type": "TextBlock", "text": "${day3HighLow}", "horizontalAlignment": "center", "spacing": "small" }
                            ]
                        }
                    ]
                }
            ]
        }
        """;
	}

	private async void OnFavoritesChanged(object? sender, EventArgs e)
	{
		if (_fixedLocation == null)
		{
			await LoadWeatherDataAsync();
		}
	}

	private async void OnSettingsChanged(object sender, Settings args)
	{
		await LoadWeatherDataAsync();
	}

	public void Dispose()
	{
		_settings.Settings.SettingsChanged -= OnSettingsChanged;
		if (_favoritesManager != null)
		{
			_favoritesManager.FavoritesChanged -= OnFavoritesChanged;
		}
		_cts?.Cancel();
		_cts?.Dispose();
	}
}
