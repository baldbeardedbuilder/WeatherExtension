// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CmdPal.Ext.Weather.Models;

namespace Microsoft.CmdPal.Ext.Weather.Services;

public sealed class PinnedLocationsManager
{
	private readonly JsonFileStore<PinnedLocation> _store;

	public event EventHandler? PinnedLocationsChanged;

	public PinnedLocationsManager()
	{
		var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		var directory = Path.Combine(localAppData, "Microsoft.CmdPal");
		Directory.CreateDirectory(directory);
		var filePath = Path.Combine(directory, "pinned-weather-locations.json");
		_store = new JsonFileStore<PinnedLocation>(filePath, WeatherJsonContext.Default.ListPinnedLocation, "pinned locations");
	}

	internal PinnedLocationsManager(string filePath)
	{
		_store = new JsonFileStore<PinnedLocation>(filePath, WeatherJsonContext.Default.ListPinnedLocation, "pinned locations");
	}

	public void Pin(GeocodingResult location)
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
			PinnedLocationsChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	public void Unpin(GeocodingResult location)
	{
		var removed = _store.Remove(
			p => Math.Abs(p.Latitude - location.Latitude) < 0.01 &&
				 Math.Abs(p.Longitude - location.Longitude) < 0.01);

		if (removed)
		{
			PinnedLocationsChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	public bool IsPinned(GeocodingResult location)
	{
		return _store.Any(
			p => Math.Abs(p.Latitude - location.Latitude) < 0.01 &&
				 Math.Abs(p.Longitude - location.Longitude) < 0.01);
	}

	public List<PinnedLocation> GetPinnedLocations()
	{
		return _store.GetAll();
	}
}
