// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
	private List<PinnedWeatherBand> _pinnedBands = [];

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

		Settings = _settingsManager.Settings;

		// Use our own settings page so saving keeps the user inside the
		// settings sheet instead of bouncing to the root command palette.
		// The toolkit's built-in SettingsPage hard-codes CommandResult.GoHome().
		_settingsPage = new WeatherSettingsPage(_settingsManager);

		// Create main weather page
		_weatherPage = new WeatherListPage(_weatherService, _geocodingService, _settingsManager, _favoritesManager);

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
		var dockItems = new List<ICommandItem>();
		var newBands = new List<PinnedWeatherBand>();

		// One band per favorite. Toggling a favorite is the only knob the
		// user has to control what shows up here, which keeps the mental
		// model simple: "the places I starred are the places I see in the
		// dock."
		foreach (var favorite in _favoritesManager.GetFavorites())
		{
			AddBandFor(favorite.ToGeocodingResult(), dockItems, newBands);
		}

		// Replace the tracked bands and dispose any prior generation. Done
		// inside the lock so a concurrent OnFavoritesChanged can't
		// double-dispose or leak the previous list.
		List<PinnedWeatherBand> previous;
		lock (_bandsSync)
		{
			previous = _pinnedBands;
			_pinnedBands = newBands;
		}

		foreach (var band in previous)
		{
			band.Dispose();
		}

		return dockItems.ToArray();
	}

	private void AddBandFor(
		Microsoft.CmdPal.Ext.Weather.Models.GeocodingResult location,
		List<ICommandItem> dockItems,
		List<PinnedWeatherBand> bandTracker)
	{
		var bandCard = new WeatherBandCard(_weatherService, _geocodingService, _settingsManager, _favoritesManager, location);
		var pinnedBand = new PinnedWeatherBand(location, _weatherService, _settingsManager, bandCard);
		bandTracker.Add(pinnedBand);

		var pinnedWrappedBand = new WrappedDockItem(
			[pinnedBand],
			FormattableString.Invariant(
				$"com.baldbeardedbuilder.cmdpal.weather.pinnedBand.{location.Latitude}_{location.Longitude}"),
			$"Weather - {location.DisplayName}");

		pinnedWrappedBand.Icon = Icons.WeatherIcon;
		pinnedBand.DockItem = pinnedWrappedBand;

		dockItems.Add(pinnedWrappedBand);
	}

	private void OnFavoritesChanged(object? sender, EventArgs e)
	{
		// Tell the host to re-query GetDockBands() so the band list reflects
		// the user's latest favorite/unfavorite action without forcing a
		// full extension reload.
		RaiseItemsChanged(0);
	}

	public override void Dispose()
	{
		_favoritesManager.FavoritesChanged -= OnFavoritesChanged;
		List<PinnedWeatherBand> bandsToDispose;
		lock (_bandsSync)
		{
			bandsToDispose = _pinnedBands;
			_pinnedBands = [];
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
