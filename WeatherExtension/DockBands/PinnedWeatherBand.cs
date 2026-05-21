// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BaldBeardedBuilder.WeatherExtension;
using Microsoft.CmdPal.Ext.Weather.Models;
using Microsoft.CmdPal.Ext.Weather.Pages;
using Microsoft.CmdPal.Ext.Weather.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Timer = System.Timers.Timer;

namespace Microsoft.CmdPal.Ext.Weather.DockBands;

internal sealed partial class PinnedWeatherBand : ListItem, IDisposable
{
	private readonly GeocodingResult _location;
	private readonly OpenMeteoService _weatherService;
	private readonly WeatherSettingsManager _settings;
	private readonly WeatherBandCard _contentPage;
	private readonly Timer _updateTimer;
	private readonly CancellationTokenSource _cts = new();
	private bool _isDisposed;
	private int _isUpdating;

	internal ICommandItem? DockItem { get; set; }

	public PinnedWeatherBand(
		GeocodingResult location,
		OpenMeteoService weatherService,
		WeatherSettingsManager settings,
		WeatherBandCard contentPage)
	{
		_location = location ?? throw new ArgumentNullException(nameof(location));
		_weatherService = weatherService ?? throw new ArgumentNullException(nameof(weatherService));
		_settings = settings ?? throw new ArgumentNullException(nameof(settings));
		_contentPage = contentPage ?? throw new ArgumentNullException(nameof(contentPage));

		Command = _contentPage;
		Icon = Icons.WeatherIcon;
		Title = Resources.loading;
		Subtitle = _location.DisplayName;

		var intervalMs = _settings.UpdateIntervalMinutes * 60 * 1000;
		_updateTimer = new Timer(intervalMs);
		_updateTimer.Elapsed += async (s, e) => await UpdateWeatherAsync();
		_updateTimer.Start();

		_settings.Settings.SettingsChanged += OnSettingsChanged;

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
			var weather = await _weatherService.GetCurrentWeatherAsync(
				_location.Latitude,
				_location.Longitude,
				_settings.TemperatureUnit,
				_settings.WindSpeedUnit,
				_cts.Token);

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

				if (_settings.DockBandSubtitle == "highlow")
				{
					var forecast = await _weatherService.GetForecastAsync(
						_location.Latitude,
						_location.Longitude,
						_settings.TemperatureUnit,
						_cts.Token);

					if (forecast?.Daily?.TemperatureMax?.Count > 0 &&
						forecast.Daily.TemperatureMin?.Count > 0)
					{
						var high = forecast.Daily.TemperatureMax[0];
						var low = forecast.Daily.TemperatureMin[0];
						Subtitle = $"H: {high:F0}{unit}  L: {low:F0}{unit}";
					}
					else
					{
						Subtitle = _location.DisplayName;
					}
				}
				else
				{
					Subtitle = _location.DisplayName;
				}
			}
			else
			{
				Title = "--";
				Subtitle = $"{_location.DisplayName} — {Resources.weather_service_error}";
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
				$"Pinned band weather network error: {ex.Message}");

			if (Title == Resources.loading)
			{
				Title = "--";
				Subtitle = $"{_location.DisplayName} — {Resources.network_error}";
			}
		}
		catch (Exception ex)
		{
			WeatherLogger.LogToHost(
				MessageState.Error,
				$"Pinned band weather update error: {ex.Message}");

			if (Title == Resources.loading)
			{
				Title = "--";
				Subtitle = $"{_location.DisplayName} — {Resources.unavailable}";
			}
		}
		finally
		{
			Interlocked.Exchange(ref _isUpdating, 0);
		}
	}

	private async void OnSettingsChanged(object sender, Settings args)
	{
		await UpdateWeatherAsync();
	}

	public void Dispose()
	{
		if (!_isDisposed)
		{
			_cts.Cancel();
			_isDisposed = true;
			_settings.Settings.SettingsChanged -= OnSettingsChanged;
			_updateTimer?.Stop();
			_updateTimer?.Dispose();
			_cts.Dispose();
		}
	}
}
