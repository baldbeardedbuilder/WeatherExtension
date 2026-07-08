// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using BaldBeardedBuilder.WeatherExtension;
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
	private readonly DockPinsManager _dockPinsManager = new();
	private readonly WeatherListPage _weatherPage;
	private readonly WeatherSettingsPage _settingsPage;
	private readonly SubmitBugPage _submitBugPage = new();
	private readonly ICommandItem[] _topLevelItems;
	private readonly Lock _bandsSync = new();

	// Cache band instances by their lat/lon key. The host calls
	// GetDockBands() not just on DockPinsChanged but also when the user
	// hovers a band, opens dock customization, etc. Re-creating bands on
	// every call meant each hover bounced the band's Title back to
	// "Loading weather…" while UpdateWeatherAsync re-ran from scratch and
	// re-issued network calls. By keeping a stable band per location and
	// only adding/disposing entries on actual dock-pin changes, the
	// resolved title persists across re-queries and the user only sees
	// the loading state on the very first appearance.
	private readonly Dictionary<string, BandEntry> _bandsByKey = new(StringComparer.Ordinal);

	private readonly record struct BandEntry(PinnedWeatherBand Band, WrappedDockItem DockItem);

	public WeatherCommandsProvider()
	{
		Id = "com.baldbeardedbuilder.cmdpal.weather";
		DisplayName = Resources.plugin_name;
		Icon = Icons.WeatherIcon;

		// One-time migration: earlier versions drove dock bands from Favorites.
		// Seed the new independent dock-pin store from existing favorites so
		// users who relied on that behavior don't lose their dock content when
		// the source switches. Runs only when the dock-pin store is still empty
		// and the user actually has favorites.
		MigrateFavoritesToDockPinsIfNeeded();

		// Dock pins are the single source of truth for dock bands. When the
		// user pins/unpins a location — from the search list or the right-click
		// context menu — the dock should reflect that change immediately, so we
		// re-emit ItemsChanged on every update.
		_dockPinsManager.DockPinsChanged += OnDockPinsChanged;

		Settings = _settingsManager.Settings;

		// Use our own settings page so saving keeps the user inside the
		// settings sheet instead of bouncing to the root command palette.
		// The toolkit's built-in SettingsPage hard-codes CommandResult.GoHome().
		_settingsPage = new WeatherSettingsPage(_settingsManager);

		// Create main weather page
		_weatherPage = new WeatherListPage(_weatherService, _geocodingService, _settingsManager, _favoritesManager, _dockPinsManager);

		_topLevelItems =
		[
			new CommandItem(_weatherPage)
			{
				Icon = Icons.WeatherIcon,
				Title = Resources.plugin_name,
				MoreCommands = [new CommandContextItem(_settingsPage), new CommandContextItem(_submitBugPage)],
			},
		];
	}

	private void MigrateFavoritesToDockPinsIfNeeded()
	{
		try
		{
			if (_dockPinsManager.GetDockPins().Count > 0)
			{
				return;
			}

			foreach (var favorite in _favoritesManager.GetFavorites())
			{
				_dockPinsManager.PinToDock(favorite.ToGeocodingResult());
			}
		}
		catch (Exception ex)
		{
			WeatherLogger.LogToHost(
				MessageState.Error,
				$"Dock pin migration failed: {ex.Message}");
		}
	}

	public override ICommandItem[] TopLevelCommands() => _topLevelItems;

	public override ICommandItem[] GetDockBands()
	{
		var dockPins = _dockPinsManager.GetDockPins();
		var dockItems = new List<ICommandItem>(dockPins.Count);

		List<PinnedWeatherBand> bandsToDispose = [];

		lock (_bandsSync)
		{
			// Reconcile against the cache so a hover or any other
			// non-pin-changing GetDockBands() call returns the same
			// band instances with their already-resolved weather data
			// instead of fresh "Loading weather…" placeholders.
			var fresh = new Dictionary<string, BandEntry>(StringComparer.Ordinal);
			foreach (var pin in dockPins)
			{
				var key = BandKey(pin.Latitude, pin.Longitude);
				if (fresh.ContainsKey(key))
				{
					// User pinned two locations that round-trip to the
					// same lat/lon at our F4 precision — keep the first.
					continue;
				}

				if (_bandsByKey.TryGetValue(key, out var existing) && !existing.Band.IsDisposed)
				{
					fresh[key] = existing;
					dockItems.Add(existing.DockItem);
				}
				else
				{
					var entry = CreateBand(pin.ToGeocodingResult());
					fresh[key] = entry;
					dockItems.Add(entry.DockItem);
				}
			}

			// Dispose bands whose location is no longer pinned to the dock.
			foreach (var (key, entry) in _bandsByKey)
			{
				if (!fresh.ContainsKey(key))
				{
					bandsToDispose.Add(entry.Band);
				}
			}

			_bandsByKey.Clear();
			foreach (var (key, entry) in fresh)
			{
				_bandsByKey[key] = entry;
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
		var bandCard = new WeatherBandCard(_weatherService, _geocodingService, _settingsManager, _favoritesManager, location);
		var pinnedBand = new PinnedWeatherBand(location, _weatherService, _settingsManager, bandCard);

		var dockItem = new WrappedDockItem(
			[pinnedBand],
			FormattableString.Invariant(
				$"com.baldbeardedbuilder.cmdpal.weather.pinnedBand.{location.Latitude}_{location.Longitude}"),
			WeatherFormatter.DockBandTitle(location.DisplayName));

		dockItem.Icon = Icons.WeatherIcon;
		pinnedBand.DockItem = dockItem;

		return new BandEntry(pinnedBand, dockItem);
	}

	private void OnDockPinsChanged(object? sender, EventArgs e)
	{
		// Reconcile immediately — don't wait for the host to call GetDockBands().
		// If the user re-pins a location before the host polls, a stale
		// disposed band would otherwise be reused from the cache.
		_ = GetDockBands();

		// Tell the host to re-query GetDockBands() so the band list reflects
		// the user's latest pin/unpin action without forcing a
		// full extension reload.
		RaiseItemsChanged(0);
	}

	public override void Dispose()
	{
		_dockPinsManager.DockPinsChanged -= OnDockPinsChanged;
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
		_weatherService?.Dispose();
		_geocodingService?.Dispose();
		base.Dispose();
		GC.SuppressFinalize(this);
	}
}
