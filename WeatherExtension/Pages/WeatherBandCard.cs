// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BaldBeardedBuilder.WeatherExtension;
using Microsoft.CmdPal.Ext.Weather.Models;
using Microsoft.CmdPal.Ext.Weather.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System.Runtime.InteropServices;

namespace Microsoft.CmdPal.Ext.Weather.Pages;

internal sealed partial class WeatherBandCard : ContentPage, IDisposable
{
	private readonly OpenMeteoService _weatherService;
	private readonly WeatherSettingsManager _settings;
	private readonly FavoritesManager? _favoritesManager;
	private readonly FormContent _weatherForm = new();
	private readonly CancellationTokenSource _cts = new();
	private readonly CancellationToken _cancellationToken;
	private readonly SemaphoreSlim _loadGate = new(1, 1);
	private readonly GeocodingResult? _fixedLocation;
	private GeocodingResult? _lastLocation;
	private volatile bool _isDisposed;

	public WeatherBandCard(
		OpenMeteoService weatherService,
		IGeocodingService geocodingService,
		WeatherSettingsManager settings,
		FavoritesManager? favoritesManager = null,
		GeocodingResult? fixedLocation = null)
	{
		_weatherService = weatherService ?? throw new ArgumentNullException(nameof(weatherService));
		ArgumentNullException.ThrowIfNull(geocodingService);
		_settings = settings ?? throw new ArgumentNullException(nameof(settings));
		_favoritesManager = favoritesManager;
		_fixedLocation = fixedLocation;
		_cancellationToken = _cts.Token;

		Id = "com.baldbeardedbuilder.cmdpal.weather.card";
		Name = Resources.plugin_name;
		Title = Resources.plugin_name;
		Icon = Icons.WeatherIcon;

		_weatherForm.TemplateJson = WeatherCardTemplate.Json;
		_weatherForm.DataJson = WeatherCardPresentation.Serialize(
			WeatherCardPresentation.Empty(_fixedLocation?.DisplayName ?? Resources.plugin_name, Resources.loading_data));

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
		if (_isDisposed)
		{
			return;
		}

		var entered = false;
		try
		{
			await _loadGate.WaitAsync(_cancellationToken);
			entered = true;
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
					ShowUnavailable(Resources.no_favorites_hint);
					return;
				}

				location = favorites[0].ToGeocodingResult();
			}
			else
			{
				ShowUnavailable(Resources.location_not_found);
				return;
			}

			_lastLocation = location;
			var temperatureUnit = _settings.TemperatureUnit;
			var windUnit = _settings.WindSpeedUnit;
			var use24Hour = _settings.Use24HourClock;
			var weather = await _weatherService.GetCurrentWeatherAsync(
				location.Latitude, location.Longitude, temperatureUnit, windUnit, _cancellationToken);
			var forecast = await _weatherService.GetForecastAsync(
				location.Latitude, location.Longitude, temperatureUnit, _cancellationToken);
			var hourly = await _weatherService.GetHourlyForecastAsync(
				location.Latitude, location.Longitude, temperatureUnit, windUnit, _cancellationToken);

			var data = WeatherCardPresentation.Build(
				location, weather, forecast, hourly, temperatureUnit, windUnit, use24Hour, DateTimeOffset.UtcNow);
			foreach (var diagnostic in data.Diagnostics)
			{
				WeatherLogger.LogToHost(MessageState.Warning, diagnostic);
			}

			try
			{
				await WeatherCardImages.PopulateAsync(data);
			}
			catch (COMException ex)
			{
				WeatherLogger.LogToHost(MessageState.Error, $"Weather card image encoding failed: {ex.Message}");
				data.Status = Resources.card_artwork_unavailable;
			}
			if (!_isDisposed)
			{
				_weatherForm.DataJson = WeatherCardPresentation.Serialize(data);
				RaiseItemsChanged();
			}
		}
		catch (OperationCanceledException) when (_cancellationToken.IsCancellationRequested)
		{
			// Disposal cancels this load; it must not publish another card.
		}
		catch (Exception ex)
		{
			WeatherLogger.LogToHost(MessageState.Error, $"Content page weather load error: {ex.Message}");
			ShowUnavailable(Resources.unavailable);
		}
		finally
		{
			if (entered)
			{
				_loadGate.Release();
			}
		}
	}

	private void ShowUnavailable(string message)
	{
		if (_isDisposed)
		{
			return;
		}

		_weatherForm.DataJson = WeatherCardPresentation.Serialize(
			WeatherCardPresentation.Empty(_lastLocation?.DisplayName ?? _fixedLocation?.DisplayName ?? Resources.plugin_name, message));
		RaiseItemsChanged();
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
		if (_isDisposed)
		{
			return;
		}

		_isDisposed = true;
		_settings.Settings.SettingsChanged -= OnSettingsChanged;
		if (_favoritesManager != null)
		{
			_favoritesManager.FavoritesChanged -= OnFavoritesChanged;
		}

		_cts.Cancel();
		_cts.Dispose();
	}
}
