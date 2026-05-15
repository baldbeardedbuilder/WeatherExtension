// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.CmdPal.Ext.Weather.Commands;
using Microsoft.CmdPal.Ext.Weather.Models;
using Microsoft.CmdPal.Ext.Weather.Services;

namespace Microsoft.CmdPal.Ext.Weather.UnitTests;

[TestClass]
public class FavoriteCommandTests
{
    private string _tempFilePath = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"weather-test-fav-cmd-{Guid.NewGuid()}.json");
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    // ---------------------------------------------------------------
    // FavoriteLocationCommand
    // ---------------------------------------------------------------

    [TestMethod]
    public void FavoriteLocationCommand_Invoke_FavoritesLocation()
    {
        var manager = new FavoritesManager(_tempFilePath);
        var location = new GeocodingResult
        {
            Name = "Seattle",
            Latitude = 47.6,
            Longitude = -122.3,
        };
        var command = new FavoriteLocationCommand(location, manager);

        command.Invoke();

        Assert.IsTrue(manager.IsFavorite(location));
    }

    [TestMethod]
    public void FavoriteLocationCommand_Invoke_ReturnsKeepOpen()
    {
        var manager = new FavoritesManager(_tempFilePath);
        var location = new GeocodingResult { Name = "Seattle", Latitude = 47.6, Longitude = -122.3 };
        var command = new FavoriteLocationCommand(location, manager);

        var result = command.Invoke();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void FavoriteLocationCommand_HasCorrectName()
    {
        var manager = new FavoritesManager(_tempFilePath);
        var location = new GeocodingResult { Name = "Seattle", Latitude = 47.6, Longitude = -122.3 };
        var command = new FavoriteLocationCommand(location, manager);

        Assert.IsNotNull(command.Name);
        Assert.IsFalse(string.IsNullOrEmpty(command.Name));
    }

    [TestMethod]
    public void FavoriteLocationCommand_HasIcon()
    {
        var manager = new FavoritesManager(_tempFilePath);
        var location = new GeocodingResult { Name = "Seattle", Latitude = 47.6, Longitude = -122.3 };
        var command = new FavoriteLocationCommand(location, manager);

        Assert.IsNotNull(command.Icon);
    }

    // ---------------------------------------------------------------
    // UnfavoriteLocationCommand
    // ---------------------------------------------------------------

    [TestMethod]
    public void UnfavoriteLocationCommand_Invoke_UnfavoritesLocation()
    {
        var manager = new FavoritesManager(_tempFilePath);
        var location = new GeocodingResult
        {
            Name = "Portland",
            Latitude = 45.5152,
            Longitude = -122.6784,
        };
        manager.Favorite(location);

        var command = new UnfavoriteLocationCommand(location, manager);
        command.Invoke();

        Assert.IsFalse(manager.IsFavorite(location));
    }

    [TestMethod]
    public void UnfavoriteLocationCommand_Invoke_ReturnsKeepOpen()
    {
        var manager = new FavoritesManager(_tempFilePath);
        var location = new GeocodingResult { Name = "Portland", Latitude = 45.5, Longitude = -122.7 };
        manager.Favorite(location);
        var command = new UnfavoriteLocationCommand(location, manager);

        var result = command.Invoke();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void UnfavoriteLocationCommand_HasCorrectName()
    {
        var manager = new FavoritesManager(_tempFilePath);
        var location = new GeocodingResult { Name = "Portland", Latitude = 45.5, Longitude = -122.7 };
        var command = new UnfavoriteLocationCommand(location, manager);

        Assert.IsNotNull(command.Name);
        Assert.IsFalse(string.IsNullOrEmpty(command.Name));
    }

    [TestMethod]
    public void UnfavoriteLocationCommand_HasIcon()
    {
        var manager = new FavoritesManager(_tempFilePath);
        var location = new GeocodingResult { Name = "Portland", Latitude = 45.5, Longitude = -122.7 };
        var command = new UnfavoriteLocationCommand(location, manager);

        Assert.IsNotNull(command.Icon);
    }

    // ---------------------------------------------------------------
    // Integration: Favorite then Unfavorite via commands
    // ---------------------------------------------------------------

    [TestMethod]
    public void FavoriteThenUnfavorite_ViaCommands_LocationNotInFavorites()
    {
        var manager = new FavoritesManager(_tempFilePath);
        var location = new GeocodingResult { Name = "Vancouver", Latitude = 49.3, Longitude = -123.1 };

        new FavoriteLocationCommand(location, manager).Invoke();
        new UnfavoriteLocationCommand(location, manager).Invoke();

        Assert.IsFalse(manager.IsFavorite(location));
        Assert.AreEqual(0, manager.GetFavorites().Count);
    }
}
