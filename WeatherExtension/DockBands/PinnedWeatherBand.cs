// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BaldBeardedBuilder.WeatherExtension;
using Microsoft.CmdPal.Ext.Weather.Models;
using Microsoft.CmdPal.Ext.Weather.Pages;
using Microsoft.CmdPal.Ext.Weather.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System.Globalization;
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

	internal bool IsDisposed => _isDisposed;

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
		Title = Resources.dock_band_loading;
		Subtitle = _location.DisplayName;

		var intervalMs = _settings.UpdateIntervalMinutes * 60 * 1000;
		_updateTimer = new Timer(intervalMs);
		_updateTimer.Elapsed += OnTimerElapsed;
		_updateTimer.Start();

		_settings.Settings.SettingsChanged += OnSettingsChanged;

		_ = UpdateWeatherAsync();
	}

	private async void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
	{
		// async void on a Timer.Elapsed handler is unavoidable, but we must
		// not let exceptions bubble — the timer thread would crash the
		// extension host. Funnel everything into UpdateWeatherAsync's own
		// try/catch and log anything that still escapes.
		try
		{
			await UpdateWeatherAsync().ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			WeatherLogger.LogToHost(
				MessageState.Error,
				$"Pinned band timer tick failed: {ex.Message}");
		}
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
				_cts.Token).ConfigureAwait(false);

			// After every await we may have been disposed. Bail early so we
			// don't write back to ListItem properties on a stale band — the
			// host has already replaced this instance with the next dock
			// generation.
			if (_isDisposed)
			{
				return;
			}

			if (weather?.Current != null)
			{
				var tempUnit = _settings.TemperatureUnit;
				var current = weather.Current;
				var condition = Icons.GetWeatherDescription(current.WeatherCode);
				Title = string.Format(
					CultureInfo.CurrentCulture,
					"{0} {1}",
					WeatherFormatter.Temperature(current.Temperature, tempUnit),
					condition);
				Icon = Icons.GetIconForWeatherCode(current.WeatherCode);

				if (DockItem is CommandItem dockCommandItem)
				{
					dockCommandItem.Icon = Icon;
				}

				if (_settings.DockBandSubtitle == "highlow")
				{
					var forecast = await _weatherService.GetForecastAsync(
						_location.Latitude,
						_location.Longitude,
						tempUnit,
						_cts.Token).ConfigureAwait(false);

					if (_isDisposed)
					{
						return;
					}

					if (forecast?.Daily?.TemperatureMax?.Count > 0 &&
						forecast.Daily.TemperatureMin?.Count > 0)
					{
						var high = forecast.Daily.TemperatureMax[0];
						var low = forecast.Daily.TemperatureMin[0];
						Subtitle = string.Format(
							CultureInfo.CurrentCulture,
							"{0} {1}  {2} {3}",
							Resources.high,
							WeatherFormatter.Temperature(high, tempUnit),
							Resources.low,
							WeatherFormatter.Temperature(low, tempUnit));
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
			if (!_isDisposed)
			{
				await _contentPage.LoadWeatherDataAsync().ConfigureAwait(false);
			}
		}
		catch (OperationCanceledException)
		{
			// Dispose or a overlapping refresh cancelled this run — avoid leaving
			// the dock chip stuck on the loading sentinel.
			if (!_isDisposed && Title == Resources.dock_band_loading)
			{
				Title = "--";
				Subtitle = _location.DisplayName;
			}
		}
		catch (HttpRequestException ex)
		{
			WeatherLogger.LogToHost(
				MessageState.Error,
				$"Pinned band weather network error: {ex.Message}");

			if (Title == Resources.dock_band_loading)
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

			if (Title == Resources.dock_band_loading)
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
		if (_isDisposed)
		{
			return;
		}

		try
		{
			await UpdateWeatherAsync().ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			WeatherLogger.LogToHost(
				MessageState.Error,
				$"Pinned band settings refresh failed: {ex.Message}");
		}
	}

	public void Dispose()
	{
		if (_isDisposed)
		{
			return;
		}

		// Order matters: stop new triggers (timer + event) before signalling
		// in-flight work to cancel, then dispose the cancellation source and
		// the inner content page so its own subscriptions get released too.
		_isDisposed = true;
		_settings.Settings.SettingsChanged -= OnSettingsChanged;
		_updateTimer.Elapsed -= OnTimerElapsed;
		_updateTimer.Stop();
		_updateTimer.Dispose();

		try
		{
			_cts.Cancel();
		}
		catch (ObjectDisposedException)
		{
			// Already disposed elsewhere; nothing to do.
		}

		_cts.Dispose();
		_contentPage.Dispose();
	}
}
