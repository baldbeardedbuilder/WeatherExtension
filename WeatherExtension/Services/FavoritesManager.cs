// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CmdPal.Ext.Weather.Models;

namespace Microsoft.CmdPal.Ext.Weather.Services;

public sealed class FavoritesManager
{
	private readonly JsonFileStore<PinnedLocation> _store;

	public event EventHandler? FavoritesChanged;

	public FavoritesManager()
	{
		var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		var directory = Path.Combine(localAppData, "Microsoft.CmdPal");
		Directory.CreateDirectory(directory);
		var filePath = Path.Combine(directory, "favorite-weather-locations.json");
		_store = new JsonFileStore<PinnedLocation>(filePath, WeatherJsonContext.Default.ListPinnedLocation, "favorite locations");
	}

	internal FavoritesManager(string filePath)
	{
		_store = new JsonFileStore<PinnedLocation>(filePath, WeatherJsonContext.Default.ListPinnedLocation, "favorite locations");
	}

	public void Favorite(GeocodingResult location)
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
			FavoritesChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	public void Unfavorite(GeocodingResult location)
	{
		var removed = _store.Remove(
			p => Math.Abs(p.Latitude - location.Latitude) < 0.01 &&
				 Math.Abs(p.Longitude - location.Longitude) < 0.01);

		if (removed)
		{
			FavoritesChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	public bool IsFavorite(GeocodingResult location)
	{
		return _store.Any(
			p => Math.Abs(p.Latitude - location.Latitude) < 0.01 &&
				 Math.Abs(p.Longitude - location.Longitude) < 0.01);
	}

	public List<PinnedLocation> GetFavorites()
	{
		return _store.GetAll();
	}
}
