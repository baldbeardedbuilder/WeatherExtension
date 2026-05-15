// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BaldBeardedBuilder.WeatherExtension;
using Microsoft.CmdPal.Ext.Weather.Models;
using Microsoft.CommandPalette.Extensions;
using System.Text.Json;

namespace Microsoft.CmdPal.Ext.Weather.Services;

public sealed class FavoritesManager
{
	private readonly string _filePath;
	private List<PinnedLocation> _favoriteLocations = [];
	private readonly Lock _sync = new();

	public event EventHandler? FavoritesChanged;

	public FavoritesManager()
	{
		var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		var directory = Path.Combine(localAppData, "Microsoft.CmdPal");
		Directory.CreateDirectory(directory);
		_filePath = Path.Combine(directory, "favorite-weather-locations.json");
		LoadFromFile();
	}

	internal FavoritesManager(string filePath)
	{
		_filePath = filePath;
		LoadFromFile();
	}

	public void Favorite(GeocodingResult location)
	{
		lock (_sync)
		{
			if (_favoriteLocations.Any(p => Math.Abs(p.Latitude - location.Latitude) < 0.01 &&
										    Math.Abs(p.Longitude - location.Longitude) < 0.01))
			{
				return;
			}

			_favoriteLocations.Add(new PinnedLocation
			{
				Latitude = location.Latitude,
				Longitude = location.Longitude,
				DisplayName = location.DisplayName,
				Name = location.Name,
				Admin1 = location.Admin1,
				Country = location.Country,
			});

			SaveToFile();
		}

		FavoritesChanged?.Invoke(this, EventArgs.Empty);
	}

	public void Unfavorite(GeocodingResult location)
	{
		bool removed;
		lock (_sync)
		{
			var toRemove = _favoriteLocations.FirstOrDefault(p =>
				Math.Abs(p.Latitude - location.Latitude) < 0.01 &&
				Math.Abs(p.Longitude - location.Longitude) < 0.01);

			removed = toRemove != null && _favoriteLocations.Remove(toRemove);

			if (removed)
			{
				SaveToFile();
			}
		}

		if (removed)
		{
			FavoritesChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	public bool IsFavorite(GeocodingResult location)
	{
		lock (_sync)
		{
			return _favoriteLocations.Any(p =>
				Math.Abs(p.Latitude - location.Latitude) < 0.01 &&
				Math.Abs(p.Longitude - location.Longitude) < 0.01);
		}
	}

	public List<PinnedLocation> GetFavorites()
	{
		lock (_sync)
		{
			return new List<PinnedLocation>(_favoriteLocations);
		}
	}

	private void LoadFromFile()
	{
		try
		{
			if (File.Exists(_filePath))
			{
				var json = File.ReadAllText(_filePath);
				var locations = JsonSerializer.Deserialize<List<PinnedLocation>>(json, WeatherJsonContext.Default.ListPinnedLocation);
				if (locations != null)
				{
					_favoriteLocations = locations;
				}
			}
		}
		catch (Exception ex)
		{
			WeatherLogger.LogToHost(
				MessageState.Error,
				$"Failed to load favorite locations: {ex.Message}");
		}
	}

	private void SaveToFile()
	{
		try
		{
			var json = JsonSerializer.Serialize(_favoriteLocations, WeatherJsonContext.Default.ListPinnedLocation);
			File.WriteAllText(_filePath, json);
		}
		catch (Exception ex)
		{
			WeatherLogger.LogToHost(
				MessageState.Error,
				$"Failed to save favorite locations: {ex.Message}");
		}
	}
}
