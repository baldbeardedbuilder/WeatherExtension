// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Microsoft.CmdPal.Ext.Weather.DockBands;
using Microsoft.CmdPal.Ext.Weather.Pages;
using Microsoft.CmdPal.Ext.Weather.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Microsoft.CmdPal.Ext.Weather;

public sealed partial class WeatherCommandsProvider : CommandProvider
{
	private readonly WeatherSettingsManager _settingsManager = new();
	private readonly OpenMeteoService _weatherService = new();
	private readonly GeocodingService _geocodingService = new();
	private readonly FavoritesManager _favoritesManager = new();
	private readonly WeatherListPage _weatherPage;
	private readonly WeatherSettingsPage _settingsPage;
	private readonly ICommandItem[] _topLevelItems;
	private readonly Lock _bandsSync = new();

	// Cache band instances by their lat/lon key. The host calls
	// GetDockBands() not just on FavoritesChanged but also when the user
	// hovers a band, opens dock customization, etc. Re-creating bands on
	// every call meant each hover bounced the band's Title back to
	// "Loading weather…" while UpdateWeatherAsync re-ran from scratch and
	// re-issued network calls. By keeping a stable band per location and
	// only adding/disposing entries on actual favorite changes, the
	// resolved title persists across re-queries and the user only sees
	// the loading state on the very first appearance.
	private readonly Dictionary<string, BandEntry> _bandsByKey = new(StringComparer.Ordinal);

	private readonly record struct BandEntry(PinnedWeatherBand Band, WrappedDockItem DockItem);

	public WeatherCommandsProvider()
	{
		Id = "com.baldbeardedbuilder.cmdpal.weather";
		DisplayName = Resources.plugin_name;
		Icon = Icons.WeatherIcon;

		// Favorites are the single source of truth for dock bands. When the
		// user toggles a favorite — from the search list, the band card, or
		// the right-click context menu — the dock should reflect that change
		// immediately, so we re-emit ItemsChanged on every update.
		_favoritesManager.FavoritesChanged += OnFavoritesChanged;
		_settingsManager.RefreshDefaultLocationChoices(_favoritesManager.GetFavorites());

		Settings = _settingsManager.Settings;
		_settingsManager.Settings.SettingsChanged += OnSettingsChanged;

		// Use our own settings page so saving keeps the user inside the
		// settings sheet instead of bouncing to the root command palette.
		// The toolkit's built-in SettingsPage hard-codes CommandResult.GoHome().
		_settingsPage = new WeatherSettingsPage(_settingsManager);

		// Create main weather page
		_weatherPage = new WeatherListPage(_weatherService, _geocodingService, _settingsManager, _favoritesManager, _settingsPage);

		_topLevelItems =
		[
			new CommandItem(_weatherPage)
			{
				Icon = Icons.WeatherIcon,
				Title = Resources.plugin_name,
				MoreCommands = [new CommandContextItem(_settingsPage)],
			},
		];
	}

	public override ICommandItem[] TopLevelCommands() => _topLevelItems;

	public override ICommandItem[] GetDockBands()
	{
		BaldBeardedBuilder.WeatherExtension.WeatherLogger.Debug("GetDockBands() start");
		var favorites = _favoritesManager.GetFavorites();
		var dockItems = new List<ICommandItem>();

		if (favorites.Count == 0)
		{
			lock (_bandsSync)
			{
				foreach (var entry in _bandsByKey.Values)
				{
					entry.Band.Dispose();
				}
				_bandsByKey.Clear();
			}
			return [];
		}

		var defaultKey = _settingsManager.DefaultLocationKey;
		Microsoft.CmdPal.Ext.Weather.Models.PinnedLocation? selectedFavorite = null;

		if (defaultKey != "auto")
		{
			selectedFavorite = favorites.FirstOrDefault(f => BandKey(f.Latitude, f.Longitude) == defaultKey);
		}
		
		if (selectedFavorite == null)
		{
			selectedFavorite = favorites[0];
		}

		var key = BandKey(selectedFavorite.Latitude, selectedFavorite.Longitude);
		List<PinnedWeatherBand> bandsToDispose = [];

		lock (_bandsSync)
		{
			var fresh = new Dictionary<string, BandEntry>(StringComparer.Ordinal);
			BaldBeardedBuilder.WeatherExtension.WeatherLogger.Debug($"GetDockBands: Selected favorite = {selectedFavorite.DisplayName}");

			if (_bandsByKey.TryGetValue(key, out var existing) && !existing.Band.IsDisposed)
			{
				BaldBeardedBuilder.WeatherExtension.WeatherLogger.Debug($"GetDockBands: Reusing existing for {key}");
				fresh[key] = existing;
				dockItems.Add(existing.DockItem);
			}
			else
			{
				BaldBeardedBuilder.WeatherExtension.WeatherLogger.Debug($"GetDockBands: Creating new for {key}");
				var entry = CreateBand(selectedFavorite.ToGeocodingResult());
				fresh[key] = entry;
				dockItems.Add(entry.DockItem);
			}

			// Dispose bands whose location is no longer the primary one.
			foreach (var (oldKey, entry) in _bandsByKey)
			{
				if (!fresh.ContainsKey(oldKey))
				{
					BaldBeardedBuilder.WeatherExtension.WeatherLogger.Debug($"GetDockBands: Scheduling dispose for {oldKey}");
					bandsToDispose.Add(entry.Band);
				}
			}

			_bandsByKey.Clear();
			foreach (var (k, entry) in fresh)
			{
				_bandsByKey[k] = entry;
			}
		}

		foreach (var band in bandsToDispose)
		{
			band.Dispose();
		}

		return dockItems.ToArray();
	}

	private static string BandKey(double latitude, double longitude)
		=> FormattableString.Invariant($"{latitude:F4}_{longitude:F4}");

	private BandEntry CreateBand(Microsoft.CmdPal.Ext.Weather.Models.GeocodingResult location)
	{
		var bandCard = new WeatherBandCard(_weatherService, _geocodingService, _settingsManager, _favoritesManager, location, _settingsPage);
		var pinnedBand = new PinnedWeatherBand(location, _weatherService, _settingsManager, bandCard, _settingsPage);

		var dockItem = new WrappedDockItem(
			[pinnedBand],
			"com.baldbeardedbuilder.cmdpal.weather.pinnedBand.primary",
			WeatherFormatter.DockBandTitle(location.DisplayName));

		dockItem.Icon = Icons.WeatherIcon;
		pinnedBand.DockItem = dockItem;

		return new BandEntry(pinnedBand, dockItem);
	}

	private void OnFavoritesChanged(object? sender, EventArgs e)
	{
		BaldBeardedBuilder.WeatherExtension.WeatherLogger.Debug("OnFavoritesChanged triggered");
		_settingsManager.RefreshDefaultLocationChoices(_favoritesManager.GetFavorites());
		_settingsManager.RaiseSettingsChanged();

		// Reconcile immediately — don't wait for the host to call GetDockBands().
		// If the user re-favorites a location before the host polls, a stale
		// disposed band would otherwise be reused from the cache.
		_ = GetDockBands();

		// Tell the host to re-query GetDockBands() so the band list reflects
		// the user's latest favorite/unfavorite action without forcing a
		// full extension reload.
		RaiseItemsChanged(0);
	}

	private void OnSettingsChanged(object? sender, Microsoft.CommandPalette.Extensions.Toolkit.Settings e)
	{
		BaldBeardedBuilder.WeatherExtension.WeatherLogger.Debug("OnSettingsChanged triggered");
		RaiseItemsChanged(0);
	}

	public override void Dispose()
	{
		_favoritesManager.FavoritesChanged -= OnFavoritesChanged;
		_settingsManager.Settings.SettingsChanged -= OnSettingsChanged;
		List<PinnedWeatherBand> bandsToDispose;
		lock (_bandsSync)
		{
			bandsToDispose = _bandsByKey.Values.Select(e => e.Band).ToList();
			_bandsByKey.Clear();
		}

		foreach (var band in bandsToDispose)
		{
			band.Dispose();
		}

		_weatherPage?.Dispose();
		_settingsPage?.Dispose();
		_weatherService?.Dispose();
		_geocodingService?.Dispose();
		base.Dispose();
		GC.SuppressFinalize(this);
	}
}
