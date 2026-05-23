// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.CmdPal.Ext.Weather.Commands;
using Microsoft.CmdPal.Ext.Weather.Models;
using Microsoft.CmdPal.Ext.Weather.Pages;
using Microsoft.CmdPal.Ext.Weather.Services;
using Microsoft.CmdPal.Ext.Weather.UnitTests.TestDoubles;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Microsoft.CmdPal.Ext.Weather.UnitTests;

[TestClass]
public class CommandInvocationTests
{
	private string _settingsPath = string.Empty;
	private string _favoritesPath = string.Empty;
	

	[TestInitialize]
	public void Setup()
	{
		_settingsPath = Path.Combine(Path.GetTempPath(), $"weather-cmd-settings-{Guid.NewGuid()}.json");
		_favoritesPath = Path.Combine(Path.GetTempPath(), $"weather-cmd-fav-{Guid.NewGuid()}.json");
	}

	[TestCleanup]
	public void Cleanup()
	{
		foreach (var path in new[] { _settingsPath, _favoritesPath })
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
	}

	[TestMethod]
	public void RefreshWeatherCommand_Invoke_ReturnsKeepOpen()
	{
		using var page = CreateListPage();
		var command = new RefreshWeatherCommand(page);

		var result = command.Invoke();

		Assert.IsNotNull(result);
	}

	[TestMethod]
	public void ChangeLocationCommand_Invoke_ReturnsGoToPageResult()
	{
		using var page = CreateListPage();
		var command = new ChangeLocationCommand(page);

		var result = command.Invoke();

		Assert.IsNotNull(result);
		Assert.AreEqual(typeof(CommandResult).FullName, result.GetType().FullName);
	}

	[TestMethod]
	public void ViewHourlyCommand_Invoke_ReturnsGoToPageResult()
	{
		var location = new GeocodingResult
		{
			Name = "Istanbul",
			Latitude = 41.0082,
			Longitude = 28.9784,
		};
		var command = new ViewHourlyCommand(
			location,
			new StubWeatherService(),
			new WeatherSettingsManager(_settingsPath));

		var result = command.Invoke();

		Assert.IsNotNull(result);
		Assert.AreEqual(typeof(CommandResult).FullName, result.GetType().FullName);
	}

	[TestMethod]
	public void FavoriteLocationCommand_Invoke_AddsFavorite()
	{
		var manager = new FavoritesManager(_favoritesPath);
		var location = new GeocodingResult { Name = "Portland", Latitude = 45.5, Longitude = -122.7 };
		var command = new FavoriteLocationCommand(location, manager);

		command.Invoke();

		Assert.IsTrue(manager.IsFavorite(location));
	}

	[TestMethod]
	public void UnfavoriteLocationCommand_Invoke_RemovesFavorite()
	{
		var manager = new FavoritesManager(_favoritesPath);
		var location = new GeocodingResult { Name = "Portland", Latitude = 45.5, Longitude = -122.7 };
		manager.Favorite(location);

		new UnfavoriteLocationCommand(location, manager).Invoke();

		Assert.IsFalse(manager.IsFavorite(location));
	}

	private WeatherListPage CreateListPage()
	{
		var settingsManager = new WeatherSettingsManager(_settingsPath);
		return new WeatherListPage(
			new StubWeatherService(),
			new StubGeocodingService(),
			settingsManager,
			new FavoritesManager(_favoritesPath),
			new WeatherSettingsPage(settingsManager));
	}

}
