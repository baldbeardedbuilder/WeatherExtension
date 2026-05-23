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

internal sealed partial class WeatherDetailPage : ListPage, IDisposable
{
	private readonly GeocodingResult _location;
	private readonly IWeatherService _weatherService;
	private readonly WeatherSettingsManager _settingsManager;
	private readonly WeatherSettingsPage _settingsPage;
	private readonly Lock _sync = new();
	private readonly CancellationTokenSource _cts = new();

	private IListItem[] _items = [];
	private bool _isLoading = true;

	public WeatherDetailPage(
		GeocodingResult location,
		IWeatherService weatherService,
		WeatherSettingsManager settingsManager,
		WeatherSettingsPage settingsPage)
	{
		ArgumentNullException.ThrowIfNull(location);
		ArgumentNullException.ThrowIfNull(weatherService);
		ArgumentNullException.ThrowIfNull(settingsManager);

		_location = location;
		_weatherService = weatherService;
		_settingsManager = settingsManager;
		_settingsPage = settingsPage;

		Name = Resources.page_forecast_title;
		Title = Resources.page_forecast_title;
		Icon = Icons.WeatherIcon;
		Id = $"com.baldbeardedbuilder.cmdpal.weather.detail.{location.Id}";
		ShowDetails = true;

		LoadWeatherData();
	}

	private async void LoadWeatherData()
	{
		try
		{
			var weatherData = await _weatherService.GetCurrentWeatherAsync(
				_location.Latitude,
				_location.Longitude,
				_settingsManager.TemperatureUnit,
				_settingsManager.WindSpeedUnit,
				_cts.Token).ConfigureAwait(false);

			var items = new List<IListItem>();

			if (weatherData?.Current != null)
			{
				items.Add(CreateCurrentWeatherItem(weatherData));
			}

			if (_settingsManager.ShowForecast)
			{
				var forecastData = await _weatherService.GetForecastAsync(
					_location.Latitude,
					_location.Longitude,
					_settingsManager.TemperatureUnit,
					_cts.Token).ConfigureAwait(false);

				if (forecastData?.Daily != null)
				{
					items.AddRange(CreateForecastItems(forecastData));
				}
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
				$"Failed to load weather detail: {ex.Message}");

			lock (_sync)
			{
				_isLoading = false;
			}

			RaiseItemsChanged();
		}
	}

	private ListItem CreateCurrentWeatherItem(WeatherData weatherData)
	{
		var current = weatherData.Current!;
		var tempUnit = _settingsManager.TemperatureUnit == "celsius" ? "°C" : "°F";
		var windUnit = _settingsManager.WindSpeedUnit == "mph" ? "mph" : "km/h";
		var condition = Icons.GetWeatherDescription(current.WeatherCode);

		var hourlyPage = new HourlyForecastPage(_location, _weatherService, _settingsManager);

		return new ListItem(hourlyPage)
		{
			Title = Resources.current_weather,
			Subtitle = $"{condition} — {current.Temperature:F0}{tempUnit}",
			Icon = Icons.GetIconForWeatherCode(current.WeatherCode),
			Details = new Details
			{
				Title = Resources.current_weather,
				Body = WeatherFormatter.FeelsLikeSubtitle(condition, $"{current.Temperature:F0}{tempUnit}", $"{current.ApparentTemperature:F0}{tempUnit}"),
				Metadata =
				[
					new DetailsElement { Key = Resources.temperature, Data = new DetailsLink($"{current.Temperature:F1}{tempUnit}") },
					new DetailsElement { Key = Resources.feels_like, Data = new DetailsLink($"{current.ApparentTemperature:F1}{tempUnit}") },
					new DetailsElement { Key = Resources.humidity, Data = new DetailsLink($"{current.RelativeHumidity}%") },
					new DetailsElement { Key = Resources.wind_speed, Data = new DetailsLink($"{current.WindSpeed:F1} {windUnit}") },
					new DetailsElement { Key = Resources.wind_direction, Data = new DetailsLink(WeatherFormatter.CompassDirection(current.WindDirection)) },
				],
			},
		};
	}

	private List<ListItem> CreateForecastItems(ForecastData forecastData)
	{
		var daily = forecastData.Daily!;
		var items = new List<ListItem>();
		var tempUnit = _settingsManager.TemperatureUnit == "celsius" ? "°C" : "°F";

		var count = Math.Min(7, daily.Time?.Count ?? 0);
		for (var i = 0; i < count; i++)
		{
			if (daily.Time == null || daily.WeatherCode == null ||
				daily.TemperatureMax == null || daily.TemperatureMin == null)
			{
				continue;
			}

			var dateStr = daily.Time[i];
			if (!DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
			{
				continue;
			}

			var weatherCode = daily.WeatherCode[i];
			var tempMax = daily.TemperatureMax[i];
			var tempMin = daily.TemperatureMin[i];
			var condition = Icons.GetWeatherDescription(weatherCode);

			var dayName = date.Date == DateTime.Today.Date
				? Resources.today
				: date.Date == DateTime.Today.AddDays(1).Date
					? Resources.tomorrow
					: date.ToString("dddd", CultureInfo.CurrentCulture);

			var precipProb = daily.PrecipitationProbabilityMax != null && i < daily.PrecipitationProbabilityMax.Count
				? daily.PrecipitationProbabilityMax[i]
				: 0;

			items.Add(new ListItem(new NoOpCommand())
			{
				Title = dayName,
				Subtitle = $"{condition} — H: {tempMax:F0}{tempUnit} L: {tempMin:F0}{tempUnit}",
				Icon = Icons.GetIconForWeatherCode(weatherCode),
				Details = new Details
				{
					Title = date.ToString("D", CultureInfo.CurrentCulture),
					Body = $"{condition}",
					Metadata =
					[
						new DetailsElement { Key = Resources.high, Data = new DetailsLink($"{tempMax:F0}{tempUnit}") },
						new DetailsElement { Key = Resources.low, Data = new DetailsLink($"{tempMin:F0}{tempUnit}") },
						new DetailsElement { Key = Resources.precipitation, Data = new DetailsLink($"{precipProb}%") },
					],
				},
			});
		}

		return items;
	}



	public override IListItem[] GetItems()
	{
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
