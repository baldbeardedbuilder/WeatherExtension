// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CmdPal.Ext.Weather.Models;

namespace Microsoft.CmdPal.Ext.Weather.Services;

/// <summary>
/// Stores the locations the user has explicitly pinned to the Command Palette
/// Dock. This is a separate concept from Favorites: a location can be favorited
/// without being docked and vice versa. Dock bands are driven exclusively by
/// this store.
/// </summary>
public sealed class DockPinsManager
{
	private readonly JsonFileStore<PinnedLocation> _store;

	public event EventHandler? DockPinsChanged;

	public DockPinsManager()
	{
		var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		var directory = Path.Combine(localAppData, "Microsoft.CmdPal");
		Directory.CreateDirectory(directory);
		var filePath = Path.Combine(directory, "dock-pinned-weather-locations.json");
		_store = new JsonFileStore<PinnedLocation>(filePath, WeatherJsonContext.Default.ListPinnedLocation, "dock-pinned locations");
	}

	internal DockPinsManager(string filePath)
	{
		_store = new JsonFileStore<PinnedLocation>(filePath, WeatherJsonContext.Default.ListPinnedLocation, "dock-pinned locations");
	}

	public void PinToDock(GeocodingResult location)
	{
		var added = _store.Add(
			new PinnedLocation
			{
				Latitude = location.Latitude,
				Longitude = location.Longitude,
				DisplayName = location.DisplayName,
				Name = location.Name,
				Admin1 = location.Admin1,
				Country = location.Country,
			},
			p => Math.Abs(p.Latitude - location.Latitude) < 0.01 &&
				 Math.Abs(p.Longitude - location.Longitude) < 0.01);

		if (added)
		{
			DockPinsChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	public void UnpinFromDock(GeocodingResult location)
	{
		var removed = _store.Remove(
			p => Math.Abs(p.Latitude - location.Latitude) < 0.01 &&
				 Math.Abs(p.Longitude - location.Longitude) < 0.01);

		if (removed)
		{
			DockPinsChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	public bool IsPinnedToDock(GeocodingResult location)
	{
		return _store.Any(
			p => Math.Abs(p.Latitude - location.Latitude) < 0.01 &&
				 Math.Abs(p.Longitude - location.Longitude) < 0.01);
	}

	public List<PinnedLocation> GetDockPins()
	{
		return _store.GetAll();
	}
}
