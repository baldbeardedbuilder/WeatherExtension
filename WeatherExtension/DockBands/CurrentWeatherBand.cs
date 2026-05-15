// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BaldBeardedBuilder.WeatherExtension;
using Microsoft.CmdPal.Ext.Weather.Pages;
using Microsoft.CmdPal.Ext.Weather.Models;
using Microsoft.CmdPal.Ext.Weather.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Timer = System.Timers.Timer;

namespace Microsoft.CmdPal.Ext.Weather.DockBands;

internal sealed partial class CurrentWeatherBand : ListItem, IDisposable
{
	private readonly OpenMeteoService _weatherService;
	private readonly GeocodingService _geocodingService;
	private readonly WeatherSettingsManager _settings;
	private readonly FavoritesManager _favoritesManager;
	private readonly WeatherBandCard _contentPage;
	private readonly Timer _updateTimer;
	private bool _isDisposed;
	private int _isUpdating;

	internal ICommandItem? DockItem { get; set; }

	public CurrentWeatherBand(
		OpenMeteoService weatherService,
		GeocodingService geocodingService,
		WeatherSettingsManager settings,
		WeatherBandCard contentPage,
		FavoritesManager favoritesManager)
	{
		_weatherService = weatherService ?? throw new ArgumentNullException(nameof(weatherService));
		_geocodingService = geocodingService ?? throw new ArgumentNullException(nameof(geocodingService));
		_settings = settings ?? throw new ArgumentNullException(nameof(settings));
		_contentPage = contentPage ?? throw new ArgumentNullException(nameof(contentPage));
		_favoritesManager = favoritesManager ?? throw new ArgumentNullException(nameof(favoritesManager));

		Command = _contentPage;
		Icon = Icons.WeatherIcon;
		Title = Resources.loading;
		Subtitle = Resources.dockband_title;

		// Initialize timer with update interval from settings
		var intervalMs = _settings.UpdateIntervalMinutes * 60 * 1000;
		_updateTimer = new Timer(intervalMs);
		_updateTimer.Elapsed += async (s, e) => await UpdateWeatherAsync();
		_updateTimer.Start();

		_settings.Settings.SettingsChanged += OnSettingsChanged;
		_favoritesManager.FavoritesChanged += OnFavoritesChanged;

		// Fetch weather immediately on startup
		_ = UpdateWeatherAsync();
	}

	private async Task UpdateWeatherAsync()
	{
		if (_isDisposed || Interlocked.CompareExchange(ref _isUpdating, 1, 0) != 0)
		{
			return;
		}

		try
		{
			// Get first favorite location
			var favorites = _favoritesManager.GetFavorites();
			if (favorites.Count == 0)
			{
				Title = "--";
				Subtitle = Resources.no_favorites_hint;
			}
			else
			{
				var location = favorites[0].ToGeocodingResult();
				var weather = await _weatherService.GetCurrentWeatherAsync(
					location.Latitude,
					location.Longitude,
					_settings.TemperatureUnit,
					_settings.WindSpeedUnit);

				if (weather?.Current != null)
				{
					var unit = _settings.TemperatureUnit == "celsius" ? "°C" : "°F";
					var condition = Icons.GetWeatherDescription(weather.Current.WeatherCode);
					Title = $"{weather.Current.Temperature:F0}{unit} {condition}";
					Icon = Icons.GetIconForWeatherCode(weather.Current.WeatherCode);

					if (DockItem is CommandItem dockCommandItem)
					{
						dockCommandItem.Icon = Icon;
					}

					// Fetch today's forecast for high/low
					var forecast = await _weatherService.GetForecastAsync(
						location.Latitude,
						location.Longitude,
						_settings.TemperatureUnit);

					if (forecast?.Daily?.TemperatureMax?.Count > 0 &&
						forecast.Daily.TemperatureMin?.Count > 0)
					{
						var high = forecast.Daily.TemperatureMax[0];
						var low = forecast.Daily.TemperatureMin[0];
						Subtitle = $"H: {high:F0}{unit}  L: {low:F0}{unit}";
					}
					else
					{
						Subtitle = location.DisplayName;
					}
				}
				else
				{
					Title = "--";
					Subtitle = Resources.weather_service_error;
				}
			}

			// Refresh the expanded content page to stay in sync with the band
			await _contentPage.LoadWeatherDataAsync();
		}
		catch (OperationCanceledException)
		{
			// Timer or settings change cancelled — don't show error
		}
		catch (HttpRequestException ex)
		{
			WeatherLogger.LogToHost(
				MessageState.Error,
				$"Band weather network error: {ex.Message}");

			if (Title == Resources.loading)
			{
				Title = "--";
				Subtitle = Resources.network_error;
			}
		}
		catch (Exception ex)
		{
			WeatherLogger.LogToHost(
				MessageState.Error,
				$"Band weather update error: {ex.Message}");

			if (Title == Resources.loading)
			{
				Title = "--";
				Subtitle = Resources.unavailable;
			}
		}
		finally
		{
			Interlocked.Exchange(ref _isUpdating, 0);
		}
	}

	private async void OnFavoritesChanged(object? sender, EventArgs e)
	{
		await UpdateWeatherAsync();
	}

	private async void OnSettingsChanged(object sender, Settings args)
	{
		await UpdateWeatherAsync();
	}

	public void Dispose()
	{
		if (!_isDisposed)
		{
			_isDisposed = true;
			_settings.Settings.SettingsChanged -= OnSettingsChanged;
			_favoritesManager.FavoritesChanged -= OnFavoritesChanged;
			_updateTimer?.Stop();
			_updateTimer?.Dispose();
		}
	}
}
