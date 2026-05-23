// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BaldBeardedBuilder.WeatherExtension;
using Microsoft.CmdPal.Ext.Weather.Commands;
using Microsoft.CmdPal.Ext.Weather.Models;
using Microsoft.CmdPal.Ext.Weather.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Windows.System;

namespace Microsoft.CmdPal.Ext.Weather.Pages;

internal sealed partial class WeatherListPage : DynamicListPage, IDisposable
{
	private const int MinSearchLength = 3;

	private readonly IWeatherService _weatherService;
	private readonly IGeocodingService _geocodingService;
	private readonly WeatherSettingsManager _settingsManager;
	private readonly FavoritesManager _favoritesManager;
	private readonly WeatherSettingsPage _settingsPage;
	private readonly Lock _sync = new();
	private readonly CancellationTokenSource _cts = new();

	private IListItem[] _items = [];
	private bool _isLoading;
	private int _favoritesLoadGeneration;
	private string _lastSearchQuery = string.Empty;
	private CancellationTokenSource _searchCts = new();

	public WeatherListPage(
		IWeatherService weatherService,
		IGeocodingService geocodingService,
		WeatherSettingsManager settingsManager,
		FavoritesManager favoritesManager,
		WeatherSettingsPage settingsPage)
	{
		ArgumentNullException.ThrowIfNull(weatherService);
		ArgumentNullException.ThrowIfNull(geocodingService);
		ArgumentNullException.ThrowIfNull(settingsManager);
		ArgumentNullException.ThrowIfNull(favoritesManager);

		_weatherService = weatherService;
		_geocodingService = geocodingService;
		_settingsManager = settingsManager;
		_favoritesManager = favoritesManager;
		_settingsPage = settingsPage;

		Name = Resources.plugin_name;
		Title = Resources.plugin_name;
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
		var generation = Interlocked.Increment(ref _favoritesLoadGeneration);

		try
		{
			var favorites = _favoritesManager.GetFavorites();
			if (favorites.Count == 0)
			{
				lock (_sync)
				{
					if (generation != _favoritesLoadGeneration)
					{
						return;
					}

					_items = [];
					_isLoading = false;
				}

				EmptyContent = BuildSearchHintCard(Resources.no_favorites_hint);

				RaiseItemsChanged();
				return;
			}

			EmptyContent = null;

			lock (_sync)
			{
				if (generation != _favoritesLoadGeneration)
				{
					return;
				}

				_items = [];
				_isLoading = true;
			}

			RaiseItemsChanged();

			using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(searchCt, _cts.Token);

			foreach (var pinned in favorites)
			{
				if (generation != _favoritesLoadGeneration)
				{
					return;
				}

				var location = pinned.ToGeocodingResult();
				try
				{
					var weatherData = await _weatherService.GetCurrentWeatherAsync(
						location.Latitude,
						location.Longitude,
						_settingsManager.TemperatureUnit,
						_settingsManager.WindSpeedUnit,
						linkedCts.Token).ConfigureAwait(false);

					if (generation != _favoritesLoadGeneration)
					{
						return;
					}

					if (weatherData != null)
					{
						lock (_sync)
						{
							if (generation != _favoritesLoadGeneration)
							{
								return;
							}

							_items = [.. _items, CreateWeatherItem(location, weatherData)];
						}

						RaiseItemsChanged();
					}
				}
				catch (Exception ex) when (ex is not OperationCanceledException)
				{
					WeatherLogger.LogToHost(
						MessageState.Error,
						$"Failed to load weather for favorite {location.DisplayName}: {ex.Message}");
				}
			}

		}
		catch (OperationCanceledException)
		{
			// Expected when cancelled by a newer favorites reload or page dispose.
		}
		catch (Exception ex)
		{
			WeatherLogger.LogToHost(
				MessageState.Error,
				$"Failed to load favorite locations: {ex.Message}");
		}
		finally
		{
			// If this generation was superseded or cancelled after setting
			// _isLoading = true, we must clear the flag or GetItems() keeps
			// returning "Loading weather data…" forever.
			lock (_sync)
			{
				if (generation == _favoritesLoadGeneration)
				{
					_isLoading = false;
				}
			}

			if (generation == _favoritesLoadGeneration)
			{
				RaiseItemsChanged();
			}
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
		var detailPage = new WeatherDetailPage(
			location,
			_weatherService,
			_settingsManager,
			_settingsPage);

		var moreCommands = new List<ICommandContextItem>
		{
			new CommandContextItem(new RefreshWeatherCommand(this)),
			new CommandContextItem(_settingsPage) { Title = Resources.settings_page_title },
		};

		if (_favoritesManager.IsFavorite(location))
		{
			moreCommands.Add(new CommandContextItem(new UnfavoriteLocationCommand(location, _favoritesManager))
			{
				RequestedShortcut = new KeyChord(VirtualKeyModifiers.Control, (int)VirtualKey.D, 0),
			});
		}
		else
		{
			moreCommands.Add(new CommandContextItem(new FavoriteLocationCommand(location, _favoritesManager))
			{
				RequestedShortcut = new KeyChord(VirtualKeyModifiers.Control, (int)VirtualKey.D, 0),
			});
		}

		var tags = new List<ITag>();
		if (_favoritesManager.IsFavorite(location))
		{
			tags.Add(new Tag(Resources.favorite_tag) { Icon = new IconInfo("\u2B50") });
		}

		var item = new ListItem(detailPage)
		{
			Title = location.DisplayName,
			Subtitle = WeatherFormatter.CurrentSubtitle(current, _settingsManager.TemperatureUnit),
			Icon = Icons.GetIconForWeatherCode(current.WeatherCode),
			Tags = tags.ToArray(),
			Details = new Details
			{
				Title = location.DisplayName,
				Body = WeatherFormatter.CurrentSubtitle(current, _settingsManager.TemperatureUnit),
				Metadata =
				[
					new DetailsElement { Key = Resources.humidity, Data = new DetailsLink(WeatherFormatter.Humidity(current.RelativeHumidity)) },
					new DetailsElement { Key = Resources.wind_speed, Data = new DetailsLink(WeatherFormatter.Wind(current.WindSpeed, _settingsManager.WindSpeedUnit)) },
					new DetailsElement { Key = Resources.wind_direction, Data = new DetailsLink(WeatherFormatter.CompassDirection(current.WindDirection)) },
				],
			},
			MoreCommands = moreCommands.ToArray(),
		};

		return item;
	}

	private static string GetWindDirection(int degrees) => WeatherFormatter.CompassDirection(degrees);

	// Builds a richer empty-state hint card so first-time users see what they
	// can type and which keyboard shortcuts apply, instead of a single line
	// like "No favorites".
	private static ListItem BuildSearchHintCard(string headline, bool includeSearchFormatHint = false)
	{
		// The empty-state panel renders Title + Subtitle only (not Details.Body),
		// so stack every hint line in Subtitle. Opening the row shows markdown.
		return new ListItem(new SearchHintPage(headline, includeSearchFormatHint))
		{
			Title = headline,
			Subtitle = SearchHints.BuildListHintEmptySubtitle(),
			Icon = Icons.WeatherIcon,
			Details = new Details
			{
				Title = headline,
				Body = SearchHints.BuildListHintBody(),
			},
		};
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
			EmptyContent = BuildSearchHintCard(Resources.search_min_chars);
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
				EmptyContent = BuildSearchHintCard(Resources.no_locations_found, includeSearchFormatHint: true);

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
		else
		{
			// Reflect the new favorite/unfavorite state in any item currently
			// shown by the active search results without forcing a fresh
			// network round trip.
			RefreshWeather();
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
