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
	private readonly PinnedLocationsManager _pinnedLocationsManager = new();
	private readonly FavoritesManager _favoritesManager = new();
	private readonly WeatherListPage _weatherPage;
	private readonly ICommandItem[] _topLevelItems;
	private List<PinnedWeatherBand> _pinnedBands = [];

	public WeatherCommandsProvider()
	{
		Id = "com.baldbeardedbuilder.cmdpal.weather";
		DisplayName = Resources.plugin_name;
		Icon = Icons.WeatherIcon;
		Settings = _settingsManager.Settings;

		// Create main weather page
		_weatherPage = new WeatherListPage(_weatherService, _geocodingService, _settingsManager, _pinnedLocationsManager, _favoritesManager);

		_topLevelItems =
		[
			new CommandItem(_weatherPage)
			{
				Icon = Icons.WeatherIcon,
				Title = Resources.plugin_name,
				MoreCommands = [new CommandContextItem(_settingsManager.Settings.SettingsPage)],
			},
		];

		_pinnedLocationsManager.PinnedLocationsChanged += OnPinnedLocationsChanged;
	}

	public override ICommandItem[] TopLevelCommands() => _topLevelItems;

	public override ICommandItem[] GetDockBands()
	{
		var dockItems = new List<ICommandItem>();

		var pinnedLocations= _pinnedLocationsManager.GetPinnedLocations();
		foreach (var pinnedLocation in pinnedLocations)
		{
			var location = pinnedLocation.ToGeocodingResult();
			var bandCard = new WeatherBandCard(_weatherService, _geocodingService, _settingsManager, _favoritesManager, location);
			var pinnedBand = new PinnedWeatherBand(location, _weatherService, _settingsManager, bandCard);
			_pinnedBands.Add(pinnedBand);

			var pinnedWrappedBand = new WrappedDockItem(
				[pinnedBand],
				$"com.baldbeardedbuilder.cmdpal.weather.pinnedBand.{pinnedLocation.Latitude}_{pinnedLocation.Longitude}",
				$"Weather - {pinnedLocation.DisplayName}");

			pinnedWrappedBand.Icon = Icons.WeatherIcon;
			pinnedBand.DockItem = pinnedWrappedBand;

			dockItems.Add(pinnedWrappedBand);
		}

		return dockItems.ToArray();
	}

	private void OnPinnedLocationsChanged(object? sender, EventArgs e)
	{
		foreach (var band in _pinnedBands)
		{
			band.Dispose();
		}
		_pinnedBands.Clear();
	}


	public override void Dispose()
	{
		_pinnedLocationsManager.PinnedLocationsChanged -= OnPinnedLocationsChanged;
		foreach (var band in _pinnedBands)
		{
			band.Dispose();
		}
		_pinnedBands.Clear();
		_weatherPage?.Dispose();
		_weatherService?.Dispose();
		_geocodingService?.Dispose();
		base.Dispose();
		GC.SuppressFinalize(this);
	}
}
