// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BaldBeardedBuilder.WeatherExtension;
using Microsoft.CmdPal.Ext.Weather.Models;
using Microsoft.CommandPalette.Extensions;
using System.Globalization;
using System.Text.Json;

namespace Microsoft.CmdPal.Ext.Weather.Services;

public sealed partial class OpenMeteoService : IWeatherService, IDisposable
{
	private readonly HttpClient _httpClient;
	private const string BaseUrl = "https://api.open-meteo.com/v1/forecast";
	private const string ConnectivityProbeUrl = "https://connectivitycheck.gstatic.com/generate_204";
	private const int CacheExpirationMinutes = 15;

	private WeatherData? _cachedWeather;
	private DateTime _weatherCacheTime;
	private string _weatherCacheKey = string.Empty;

	private ForecastData? _cachedForecast;
	private DateTime _forecastCacheTime;
	private string _forecastCacheKey = string.Empty;

	private HourlyForecastData? _cachedHourly;
	private DateTime _hourlyCacheTime;
	private string _hourlyCacheKey = string.Empty;

	private readonly Lock _cacheLock = new();

	public OpenMeteoService()
	{
		_httpClient = new HttpClient
		{
			Timeout = TimeSpan.FromSeconds(10),
		};
		_httpClient.DefaultRequestHeaders.Add("User-Agent", "PowerToys-CmdPal-Weather/1.0");
	}

	internal OpenMeteoService(HttpMessageHandler handler)
	{
		_httpClient = new HttpClient(handler)
		{
			Timeout = TimeSpan.FromSeconds(10),
		};
		_httpClient.DefaultRequestHeaders.Add("User-Agent", "PowerToys-CmdPal-Weather/1.0");
	}

	public async Task<WeatherData?> GetCurrentWeatherAsync(
		double latitude,
		double longitude,
		string temperatureUnit = "celsius",
		string windSpeedUnit = "kmh",
		CancellationToken ct = default)
	{
		try
		{
			var cacheKey = FormattableString.Invariant($"{latitude},{longitude},{temperatureUnit},{windSpeedUnit}");
			lock (_cacheLock)
			{
				if (_cachedWeather != null &&
					_weatherCacheKey == cacheKey &&
					(DateTime.UtcNow - _weatherCacheTime).TotalMinutes < CacheExpirationMinutes)
				{
					return _cachedWeather;
				}
			}

			var url = string.Create(
				CultureInfo.InvariantCulture,
				$"{BaseUrl}?latitude={latitude}&longitude={longitude}&current=temperature_2m,relative_humidity_2m,apparent_temperature,weather_code,wind_speed_10m,wind_direction_10m&temperature_unit={temperatureUnit}&wind_speed_unit={windSpeedUnit}&timezone=auto");

			var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);

			if (!response.IsSuccessStatusCode)
			{
				WeatherLogger.LogToHost(
					MessageState.Error,
					$"Weather API returned status {response.StatusCode}");
				await ProbeConnectivityAsync(Resources.connectivity_endpoint_current_weather, ct).ConfigureAwait(false);
				return null;
			}

			var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
			var weatherData = JsonSerializer.Deserialize(content, WeatherJsonContext.Default.WeatherData);

			if (weatherData == null)
			{
				WeatherLogger.LogToHost(
					MessageState.Info,
					$"Weather deserialization returned null. Status: {response.StatusCode}, Content length: {content.Length}");
			}

			if (weatherData != null)
			{
				lock (_cacheLock)
				{
					_cachedWeather = weatherData;
					_weatherCacheTime = DateTime.UtcNow;
					_weatherCacheKey = cacheKey;
				}
			}

			return weatherData;
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			WeatherLogger.LogToHost(
				MessageState.Error,
				$"Weather fetch error: {ex.Message}");
			await ProbeConnectivityAsync(Resources.connectivity_endpoint_current_weather, ct).ConfigureAwait(false);
			return null;
		}
	}

	public async Task<ForecastData?> GetForecastAsync(
		double latitude,
		double longitude,
		string temperatureUnit = "celsius",
		CancellationToken ct = default)
	{
		try
		{
			var cacheKey = FormattableString.Invariant($"{latitude},{longitude},{temperatureUnit}");
			lock (_cacheLock)
			{
				if (_cachedForecast != null &&
					_forecastCacheKey == cacheKey &&
					(DateTime.UtcNow - _forecastCacheTime).TotalMinutes < CacheExpirationMinutes)
				{
					return _cachedForecast;
				}
			}

			var url = string.Create(
				CultureInfo.InvariantCulture,
				$"{BaseUrl}?latitude={latitude}&longitude={longitude}&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max&temperature_unit={temperatureUnit}&timezone=auto");

			var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);

			if (!response.IsSuccessStatusCode)
			{
				WeatherLogger.LogToHost(
					MessageState.Error,
					$"Forecast API returned status {response.StatusCode}");
				await ProbeConnectivityAsync(Resources.connectivity_endpoint_forecast, ct).ConfigureAwait(false);
				return null;
			}

			var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
			var forecastData = JsonSerializer.Deserialize(content, WeatherJsonContext.Default.ForecastData);

			if (forecastData == null)
			{
				WeatherLogger.LogToHost(
					MessageState.Info,
					$"Forecast deserialization returned null. Status: {response.StatusCode}, Content length: {content.Length}");
			}

			if (forecastData != null)
			{
				lock (_cacheLock)
				{
					_cachedForecast = forecastData;
					_forecastCacheTime = DateTime.UtcNow;
					_forecastCacheKey = cacheKey;
				}
			}

			return forecastData;
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			WeatherLogger.LogToHost(
				MessageState.Error,
				$"Forecast fetch error: {ex.Message}");
			await ProbeConnectivityAsync(Resources.connectivity_endpoint_forecast, ct).ConfigureAwait(false);
			return null;
		}
	}

	public async Task<HourlyForecastData?> GetHourlyForecastAsync(
		double latitude,
		double longitude,
		string temperatureUnit = "celsius",
		string windSpeedUnit = "kmh",
		CancellationToken ct = default)
	{
		try
		{
			var cacheKey = FormattableString.Invariant($"{latitude},{longitude},{temperatureUnit},{windSpeedUnit}");
			lock (_cacheLock)
			{
				if (_cachedHourly != null &&
					_hourlyCacheKey == cacheKey &&
					(DateTime.UtcNow - _hourlyCacheTime).TotalMinutes < CacheExpirationMinutes)
				{
					return _cachedHourly;
				}
			}

			var url = string.Create(
				CultureInfo.InvariantCulture,
				$"{BaseUrl}?latitude={latitude}&longitude={longitude}&hourly=temperature_2m,apparent_temperature,weather_code,precipitation_probability,wind_speed_10m,relative_humidity_2m&temperature_unit={temperatureUnit}&wind_speed_unit={windSpeedUnit}&forecast_days=2&timezone=auto");

			var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);

			if (!response.IsSuccessStatusCode)
			{
				WeatherLogger.LogToHost(
					MessageState.Error,
					$"Hourly forecast API returned status {response.StatusCode}");
				await ProbeConnectivityAsync(Resources.connectivity_endpoint_hourly_forecast, ct).ConfigureAwait(false);
				return null;
			}

			var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
			var hourlyData = JsonSerializer.Deserialize(content, WeatherJsonContext.Default.HourlyForecastData);

			if (hourlyData == null)
			{
				WeatherLogger.LogToHost(
					MessageState.Info,
					$"Hourly forecast deserialization returned null. Status: {response.StatusCode}, Content length: {content.Length}");
			}

			if (hourlyData != null)
			{
				lock (_cacheLock)
				{
					_cachedHourly = hourlyData;
					_hourlyCacheTime = DateTime.UtcNow;
					_hourlyCacheKey = cacheKey;
				}
			}

			return hourlyData;
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			WeatherLogger.LogToHost(
				MessageState.Error,
				$"Hourly forecast fetch error: {ex.Message}");
			await ProbeConnectivityAsync(Resources.connectivity_endpoint_hourly_forecast, ct).ConfigureAwait(false);
			return null;
		}
	}

	private async Task ProbeConnectivityAsync(string failedEndpoint, CancellationToken ct)
	{
		try
		{
			using var probeRequest = new HttpRequestMessage(HttpMethod.Head, ConnectivityProbeUrl);
			probeRequest.Headers.Add("User-Agent", "PowerToys-CmdPal-Weather/1.0");

			using var probeCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			probeCts.CancelAfter(TimeSpan.FromSeconds(2));

			var probeResponse = await _httpClient.SendAsync(probeRequest, probeCts.Token).ConfigureAwait(false);

			if (probeResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
			{
				// Got expected 204 — we have internet, weather API is specifically unreachable
				WeatherLogger.LogToHost(
					MessageState.Error,
					$"{Resources.connectivity_api_blocked} ({failedEndpoint})");
			}
			else
			{
				// Non-204 response (e.g., captive portal redirect) — treat as no internet
				WeatherLogger.LogToHost(
					MessageState.Warning,
					Resources.connectivity_no_internet);
			}
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			// Caller cancelled — don't log, just return
			return;
		}
		catch
		{
			// Probe failed (timeout or network error) — no internet connection
			WeatherLogger.LogToHost(
				MessageState.Warning,
				Resources.connectivity_no_internet);
		}
	}

	public void Dispose()
	{
		_httpClient?.Dispose();
	}
}
