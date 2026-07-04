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

internal sealed partial class HourlyForecastPage : ListPage, IDisposable
{
	private readonly GeocodingResult _location;
	private readonly IWeatherService _weatherService;
	private readonly WeatherSettingsManager _settingsManager;
	private readonly Lock _sync = new();
	private readonly CancellationTokenSource _cts = new();

	private IListItem[] _items = [];
	private bool _isLoading = true;
	private int _loadStarted;

	public HourlyForecastPage(
		GeocodingResult location,
		IWeatherService weatherService,
		WeatherSettingsManager settingsManager)
	{
		ArgumentNullException.ThrowIfNull(location);
		ArgumentNullException.ThrowIfNull(weatherService);
		ArgumentNullException.ThrowIfNull(settingsManager);

		_location = location;
		_weatherService = weatherService;
		_settingsManager = settingsManager;

		Name = Resources.page_hourly_title;
		Title = Resources.page_hourly_title;
		Icon = Icons.WeatherIcon;
		Id = $"com.baldbeardedbuilder.cmdpal.weather.hourly.{location.Id}";
		ShowDetails = true;

		// Loading is deferred until the host first renders the page (GetItems).
		// Constructing this page no longer triggers a network call, so callers
		// that create it speculatively (e.g. list rows the user never opens)
		// don't pay for an hourly-forecast fetch.
	}

	private void EnsureLoadStarted()
	{
		if (Interlocked.Exchange(ref _loadStarted, 1) == 0)
		{
			LoadHourlyData();
		}
	}

	private async void LoadHourlyData()
	{
		try
		{
			var hourlyData = await _weatherService.GetHourlyForecastAsync(
				_location.Latitude,
				_location.Longitude,
				_settingsManager.TemperatureUnit,
				_settingsManager.WindSpeedUnit,
				_cts.Token).ConfigureAwait(false);

			var items = new List<IListItem>();

			if (hourlyData?.Hourly != null)
			{
				items.AddRange(CreateHourlyItems(hourlyData));
			}

			lock (_sync)
			{
				_items = items.ToArray();
				_isLoading = false;
			}

			RaiseItemsChanged();
		}
		catch (Exception ex)
		{
			WeatherLogger.LogToHost(
				MessageState.Error,
				$"Failed to load hourly forecast: {ex.Message}");

			lock (_sync)
			{
				_isLoading = false;
			}

			RaiseItemsChanged();
		}
	}

	private List<ListItem> CreateHourlyItems(HourlyForecastData hourlyData)
	{
		var hourly = hourlyData.Hourly!;
		var items = new List<ListItem>();
		var tempUnit = _settingsManager.TemperatureUnit == "celsius" ? "°C" : "°F";
		var windUnit = _settingsManager.WindSpeedUnit == "mph" ? "mph" : "km/h";
		var now = DateTime.Now;

		// Parse sunrise/sunset times for night determination
		var sunriseTimes = ParseDailyTimes(hourlyData.Daily?.Sunrise);
		var sunsetTimes = ParseDailyTimes(hourlyData.Daily?.Sunset);

		var count = hourly.Time?.Count ?? 0;
		for (var i = 0; i < count; i++)
		{
			if (hourly.Time == null || hourly.WeatherCode == null ||
				hourly.Temperature == null || hourly.ApparentTemperature == null)
			{
				continue;
			}

			var timeStr = hourly.Time[i];
			if (!DateTime.TryParse(timeStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
			{
				continue;
			}

			if (time < now)
			{
				continue;
			}

			if (time > now.AddHours(24))
			{
				break;
			}

			var weatherCode = hourly.WeatherCode[i];
			var temperature = hourly.Temperature[i];
			var feelsLike = hourly.ApparentTemperature[i];
			var condition = Icons.GetWeatherDescription(weatherCode);
			var isNight = IsNightTime(time, sunriseTimes, sunsetTimes);

			var precipProb = hourly.PrecipitationProbability != null && i < hourly.PrecipitationProbability.Count
				? hourly.PrecipitationProbability[i]
				: 0;

			var windSpeed = hourly.WindSpeed != null && i < hourly.WindSpeed.Count
				? hourly.WindSpeed[i]
				: 0.0;

			var humidity = hourly.RelativeHumidity != null && i < hourly.RelativeHumidity.Count
				? hourly.RelativeHumidity[i]
				: 0;

			items.Add(new ListItem(new NoOpCommand())
			{
				Title = WeatherFormatter.Hour(time, _settingsManager.Use24HourClock),
				Subtitle = $"{condition} — {temperature:F0}{tempUnit}",
				Icon = Icons.GetIconForWeatherCode(weatherCode, isNight),
				Details = new Details
				{
					Title = WeatherFormatter.Hour(time, _settingsManager.Use24HourClock),
					Body = WeatherFormatter.FeelsLikeSubtitle(condition, $"{temperature:F0}{tempUnit}", $"{feelsLike:F0}{tempUnit}"),
					Metadata =
					[
						new DetailsElement { Key = Resources.temperature, Data = new DetailsLink($"{temperature:F1}{tempUnit}") },
						new DetailsElement { Key = Resources.feels_like, Data = new DetailsLink($"{feelsLike:F1}{tempUnit}") },
						new DetailsElement { Key = Resources.precipitation, Data = new DetailsLink($"{precipProb}%") },
						new DetailsElement { Key = Resources.wind_speed, Data = new DetailsLink($"{windSpeed:F1} {windUnit}") },
						new DetailsElement { Key = Resources.humidity, Data = new DetailsLink($"{humidity}%") },
					],
				},
			});
		}

		return items;
	}

	private static List<DateTime> ParseDailyTimes(List<string>? times)
	{
		var result = new List<DateTime>();
		if (times == null)
		{
			return result;
		}

		foreach (var t in times)
		{
			if (DateTime.TryParse(t, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
			{
				result.Add(parsed);
			}
		}

		return result;
	}

	private static bool IsNightTime(DateTime time, List<DateTime> sunriseTimes, List<DateTime> sunsetTimes)
	{
		// Find the sunrise/sunset for the same day
		var sunrise = sunriseTimes.FirstOrDefault(s => s.Date == time.Date);
		var sunset = sunsetTimes.FirstOrDefault(s => s.Date == time.Date);

		if (sunrise == default || sunset == default)
		{
			return false;
		}

		return time < sunrise || time >= sunset;
	}

	public override IListItem[] GetItems()
	{
		EnsureLoadStarted();

		lock (_sync)
		{
			if (_isLoading)
			{
				return
				[
					new ListItem(new NoOpCommand())
					{
						Title = Resources.loading_data,
						Icon = Icons.WeatherIcon,
					},
				];
			}

			return _items;
		}
	}

	public void Dispose()
	{
		_cts?.Cancel();
		_cts?.Dispose();
		GC.SuppressFinalize(this);
	}
}
