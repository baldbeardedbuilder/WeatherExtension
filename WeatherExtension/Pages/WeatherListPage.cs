// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BaldBeardedBuilder.WeatherExtension;
using Microsoft.CmdPal.Ext.Weather.Commands;
using Microsoft.CmdPal.Ext.Weather.Models;
using Microsoft.CmdPal.Ext.Weather.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Microsoft.CmdPal.Ext.Weather.Pages;

internal sealed partial class WeatherListPage : DynamicListPage, IDisposable
{
	private const int MinSearchLength = 3;

	private readonly IWeatherService _weatherService;
	private readonly IGeocodingService _geocodingService;
	private readonly WeatherSettingsManager _settingsManager;
	private readonly PinnedLocationsManager _pinnedLocationsManager;
	private readonly FavoritesManager _favoritesManager;
	private readonly Lock _sync = new();
	private readonly CancellationTokenSource _cts = new();

	private IListItem[] _items = [];
	private bool _isLoading;
	private string _lastSearchQuery = string.Empty;
	private CancellationTokenSource _searchCts = new();

	public WeatherListPage(
		IWeatherService weatherService,
		IGeocodingService geocodingService,
		WeatherSettingsManager settingsManager,
		PinnedLocationsManager pinnedLocationsManager,
		FavoritesManager favoritesManager)
	{
		ArgumentNullException.ThrowIfNull(weatherService);
		ArgumentNullException.ThrowIfNull(geocodingService);
		ArgumentNullException.ThrowIfNull(settingsManager);
		ArgumentNullException.ThrowIfNull(pinnedLocationsManager);
		ArgumentNullException.ThrowIfNull(favoritesManager);

		_weatherService = weatherService;
		_geocodingService = geocodingService;
		_settingsManager = settingsManager;
		_pinnedLocationsManager = pinnedLocationsManager;
		_favoritesManager = favoritesManager;

		Name = "Weather";
		Title = "Weather";
		Icon = Icons.WeatherIcon;
		Id = "com.baldbeardedbuilder.cmdpal.weather.list";
		PlaceholderText = Resources.search_placeholder;
		ShowDetails = true;

		LoadFavoriteLocations();

		_settingsManager.Settings.SettingsChanged += OnSettingsChanged;
		_favoritesManager.FavoritesChanged += OnFavoritesChanged;
	}

	private async void LoadFavoriteLocations(CancellationToken searchCt = default)
	{
		try
		{
			var favorites = _favoritesManager.GetFavorites();
			if (favorites.Count == 0)
			{
				lock (_sync)
				{
					_items = [];
					_isLoading = false;
				}

				EmptyContent = new ListItem(new NoOpCommand())
				{
					Title = Resources.no_favorites_hint,
					Icon = Icons.WeatherIcon,
				};

				RaiseItemsChanged();
				return;
			}

			EmptyContent = null;

			using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(searchCt, _cts.Token);
			var items = new List<IListItem>();

			foreach (var pinned in favorites)
			{
				var location = pinned.ToGeocodingResult();
				try
				{
					var weatherData = await _weatherService.GetCurrentWeatherAsync(
						location.Latitude,
						location.Longitude,
						_settingsManager.TemperatureUnit,
						_settingsManager.WindSpeedUnit,
						linkedCts.Token).ConfigureAwait(false);

					if (weatherData != null)
					{
						items.Add(CreateWeatherItem(location, weatherData));
					}
				}
				catch (Exception ex) when (ex is not OperationCanceledException)
				{
					WeatherLogger.LogToHost(
						MessageState.Error,
						$"Failed to load weather for favorite {location.DisplayName}: {ex.Message}");
				}
			}

			lock (_sync)
			{
				_items = items.ToArray();
				_isLoading = false;
			}

			RaiseItemsChanged();
		}
		catch (OperationCanceledException)
		{
			// Expected when cancelled by a newer search or page is disposed
		}
		catch (Exception ex)
		{
			WeatherLogger.LogToHost(
				MessageState.Error,
				$"Failed to load favorite locations: {ex.Message}");
		}
	}

	private async Task LoadWeatherForLocation(GeocodingResult location, CancellationToken ct)
	{
		try
		{
			// Clear any prior empty-state hint immediately — regardless of whether load succeeds.
			EmptyContent = null;

			var weatherData = await _weatherService.GetCurrentWeatherAsync(
				location.Latitude,
				location.Longitude,
				_settingsManager.TemperatureUnit,
				_settingsManager.WindSpeedUnit,
				ct).ConfigureAwait(false);

			if (weatherData?.Current == null)
			{
				EmptyContent = new ListItem(new NoOpCommand())
				{
					Title = Resources.weather_service_error,
					Icon = Icons.WeatherIcon,
				};

				lock (_sync)
				{
					_isLoading = false;
				}

				RaiseItemsChanged();
				return;
			}

			var items = new List<IListItem>
			{
				CreateWeatherItem(location, weatherData),
			};

			lock (_sync)
			{
				_items = items.ToArray();
				_isLoading = false;
			}

			RaiseItemsChanged();
		}
		catch (Exception ex)
		{
			lock (_sync)
			{
				_isLoading = false;
			}

			WeatherLogger.LogToHost(
				MessageState.Error,
				$"Failed to load weather for location: {ex.Message}");

			RaiseItemsChanged();
		}
	}

	private ListItem CreateWeatherItem(GeocodingResult location, WeatherData weatherData)
	{
		if (weatherData?.Current == null)
		{
			return new ListItem(new NoOpCommand())
			{
				Title = location.DisplayName,
				Subtitle = Resources.no_data_available,
				Icon = Icons.WeatherIcon,
			};
		}

		var current = weatherData.Current;
		var tempUnit = _settingsManager.TemperatureUnit == "celsius" ? "\u00B0C" : "\u00B0F";
		var windUnit = _settingsManager.WindSpeedUnit == "mph" ? "mph" : "km/h";
		var condition = Icons.GetWeatherDescription(current.WeatherCode);

		var detailPage = new WeatherDetailPage(
			location,
			_weatherService,
			_settingsManager);

		var moreCommands = new List<ICommandContextItem>
		{
			new CommandContextItem(new RefreshWeatherCommand(this)),
		};

		if (_pinnedLocationsManager.IsPinned(location))
		{
			moreCommands.Add(new CommandContextItem(new UnpinFromDockCommand(location, _pinnedLocationsManager)));
		}
		else
		{
			moreCommands.Add(new CommandContextItem(new PinToDockCommand(location, _pinnedLocationsManager)));
		}

		if (_favoritesManager.IsFavorite(location))
		{
			moreCommands.Add(new CommandContextItem(new UnfavoriteLocationCommand(location, _favoritesManager)));
		}
		else
		{
			moreCommands.Add(new CommandContextItem(new FavoriteLocationCommand(location, _favoritesManager)));
		}

		var item = new ListItem(detailPage)
		{
			Title = location.DisplayName,
			Subtitle = $"{condition} \u2014 {current.Temperature:F0}{tempUnit} (feels like {current.ApparentTemperature:F0}{tempUnit})",
			Icon = Icons.GetIconForWeatherCode(current.WeatherCode),
			Details = new Details
			{
				Title = location.DisplayName,
				Body = $"{condition} \u2014 {current.Temperature:F0}{tempUnit} (feels like {current.ApparentTemperature:F0}{tempUnit})",
				Metadata =
				[
					new DetailsElement { Key = "Humidity", Data = new DetailsLink($"{current.RelativeHumidity}%") },
					new DetailsElement { Key = "Wind", Data = new DetailsLink($"{current.WindSpeed:F1} {windUnit}") },
					new DetailsElement { Key = "Wind Direction", Data = new DetailsLink(GetWindDirection(current.WindDirection)) },
				],
			},
			MoreCommands = moreCommands.ToArray(),
		};

		return item;
	}

	private static string GetWindDirection(int degrees)
	{
		var directions = new[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
		var index = (int)Math.Round(degrees / 45.0) % 8;
		return directions[index];
	}

	public override void UpdateSearchText(string oldSearch, string newSearch)
	{
		if (string.IsNullOrWhiteSpace(newSearch))
		{
			var ct = ResetSearchToken();
			_lastSearchQuery = string.Empty;
			LoadFavoriteLocations(ct);
			return;
		}

		if (newSearch.Trim().Length < MinSearchLength)
		{
			ResetSearchToken();
			_lastSearchQuery = newSearch;
			EmptyContent = new ListItem(new EmptySearchHintPage())
			{
				Title = Resources.search_min_chars,
				Icon = Icons.WeatherIcon,
			};
			lock (_sync)
			{
				_items = [];
				_isLoading = false;
			}

			RaiseItemsChanged();
			return;
		}

		if (newSearch == _lastSearchQuery)
		{
			return;
		}

		_lastSearchQuery = newSearch;
		var searchCt = ResetSearchToken();
		_ = PerformDebouncedSearchAsync(newSearch, searchCt);
	}

	private CancellationToken ResetSearchToken()
	{
		var oldCts = _searchCts;
		_searchCts = new CancellationTokenSource();
		oldCts.Cancel();
		oldCts.Dispose();
		return _searchCts.Token;
	}

	private async Task PerformDebouncedSearchAsync(string query, CancellationToken searchCt)
	{
		try
		{
			using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(searchCt, _cts.Token);
			await Task.Delay(300, linkedCts.Token).ConfigureAwait(false);
			await PerformSearchAsync(query, linkedCts.Token).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
		}
	}

	private async Task PerformSearchAsync(string query, CancellationToken ct)
	{
		try
		{
			lock (_sync)
			{
				_isLoading = true;
			}

			RaiseItemsChanged();

			var locations = await _geocodingService.SearchLocationAsync(query, ct).ConfigureAwait(false);

			if (ct.IsCancellationRequested)
			{
				lock (_sync)
				{
					_isLoading = false;
				}

				RaiseItemsChanged();
				return;
			}

			if (locations.Count == 0)
			{
				EmptyContent = new ListItem(new EmptySearchHintPage())
				{
					Title = Resources.no_locations_found,
					Subtitle = Resources.search_format_hint,
					Icon = Icons.WeatherIcon,
				};

				lock (_sync)
				{
					_items = [];
					_isLoading = false;
				}

				RaiseItemsChanged();
				return;
			}

			if (locations.Count == 1)
			{
				await LoadWeatherForLocation(locations[0], ct).ConfigureAwait(false);
				return;
			}

			var items = new List<IListItem>();
			foreach (var location in locations.Take(10))
			{
				var weatherData = await _weatherService.GetCurrentWeatherAsync(
					location.Latitude,
					location.Longitude,
					_settingsManager.TemperatureUnit,
					_settingsManager.WindSpeedUnit,
					ct).ConfigureAwait(false);

				if (weatherData != null)
				{
					items.Add(CreateWeatherItem(location, weatherData));
				}
			}

			if (ct.IsCancellationRequested)
			{
				lock (_sync)
				{
					_isLoading = false;
				}

				RaiseItemsChanged();
				return;
			}

			EmptyContent = null;
			lock (_sync)
			{
				_items = items.ToArray();
				_isLoading = false;
			}

			RaiseItemsChanged();
		}
		catch (OperationCanceledException)
		{
			lock (_sync)
			{
				_isLoading = false;
			}

			RaiseItemsChanged();
		}
		catch (HttpRequestException ex)
		{
			lock (_sync)
			{
				_isLoading = false;
				_items = [new ListItem(new NoOpCommand())
				{
					Title = Resources.network_error,
					Icon = Icons.WeatherIcon,
				}];
			}

			WeatherLogger.LogToHost(
				MessageState.Error,
				$"Search network error: {ex.Message}");

			RaiseItemsChanged();
		}
		catch (Exception ex)
		{
			lock (_sync)
			{
				_isLoading = false;
			}

			WeatherLogger.LogToHost(
				MessageState.Error,
				$"Search error: {ex.Message}");

			RaiseItemsChanged();
		}
	}

	private void OnSettingsChanged(object sender, Settings args)
	{
		RefreshWeather();
	}

	private void OnFavoritesChanged(object? sender, EventArgs e)
	{
		if (string.IsNullOrWhiteSpace(_lastSearchQuery))
		{
			var ct = ResetSearchToken();
			LoadFavoriteLocations(ct);
		}
	}

	public void RefreshWeather()
	{
		var ct = ResetSearchToken();
		if (!string.IsNullOrWhiteSpace(_lastSearchQuery))
		{
			_ = PerformDebouncedSearchAsync(_lastSearchQuery, ct);
		}
		else
		{
			LoadFavoriteLocations(ct);
		}
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
		_settingsManager.Settings.SettingsChanged -= OnSettingsChanged;
		_favoritesManager.FavoritesChanged -= OnFavoritesChanged;
		_searchCts?.Cancel();
		_searchCts?.Dispose();
		_cts?.Cancel();
		_cts?.Dispose();
	}
}
