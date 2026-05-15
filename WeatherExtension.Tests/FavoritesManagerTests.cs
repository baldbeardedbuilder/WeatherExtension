// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Microsoft.CmdPal.Ext.Weather.Models;
using Microsoft.CmdPal.Ext.Weather.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CmdPal.Ext.Weather.UnitTests;

[TestClass]
public class FavoritesManagerTests
{
    private string _tempFilePath = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"weather-test-favorites-{Guid.NewGuid()}.json");
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
    // Favorite() — add
    // ---------------------------------------------------------------

    [TestMethod]
    public void Favorite_AddsLocationToGetFavorites()
    {
        var manager = new FavoritesManager(_tempFilePath);
        var location = new GeocodingResult
        {
            Name = "Seattle",
            Latitude = 47.6,
            Longitude = -122.3,
            Admin1 = "Washington",
            Country = "United States",
        };

        manager.Favorite(location);

        var favorites = manager.GetFavorites();
        Assert.AreEqual(1, favorites.Count);
        Assert.AreEqual("Seattle", favorites[0].Name);
        Assert.AreEqual(47.6, favorites[0].Latitude);
        Assert.AreEqual(-122.3, favorites[0].Longitude);
    }

    [TestMethod]
    public void Favorite_SameLocationTwice_DoesNotDuplicate()
    {
        var manager = new FavoritesManager(_tempFilePath);
        var location = new GeocodingResult
        {
            Name = "Seattle",
            Latitude = 47.6,
            Longitude = -122.3,
        };

        manager.Favorite(location);
        manager.Favorite(location);

        Assert.AreEqual(1, manager.GetFavorites().Count);
    }

    [TestMethod]
    public void Favorite_LocationWithinThreshold_DoesNotDuplicate()
    {
        // Two locations within 0.01 lat/lon of each other should be treated as duplicates
        var manager = new FavoritesManager(_tempFilePath);
        var location1 = new GeocodingResult { Name = "Seattle", Latitude = 47.6000, Longitude = -122.3000 };
        var location2 = new GeocodingResult { Name = "Seattle", Latitude = 47.6005, Longitude = -122.3005 };

        manager.Favorite(location1);
        manager.Favorite(location2);

        Assert.AreEqual(1, manager.GetFavorites().Count);
    }

    [TestMethod]
    public void Favorite_LocationOutsideThreshold_AddsBoth()
    {
        // Two locations more than 0.01 apart should both be stored
        var manager = new FavoritesManager(_tempFilePath);
        var location1 = new GeocodingResult { Name = "Seattle", Latitude = 47.6, Longitude = -122.3 };
        var location2 = new GeocodingResult { Name = "Portland", Latitude = 45.5, Longitude = -122.7 };

        manager.Favorite(location1);
        manager.Favorite(location2);

        Assert.AreEqual(2, manager.GetFavorites().Count);
    }

    [TestMethod]
    public void Favorite_PersistsToFile()
    {
        var location = new GeocodingResult
        {
            Name = "Portland",
            Latitude = 45.5152,
            Longitude = -122.6784,
            Admin1 = "Oregon",
            Country = "United States",
        };

        var manager1 = new FavoritesManager(_tempFilePath);
        manager1.Favorite(location);

        var manager2 = new FavoritesManager(_tempFilePath);
        var favorites = manager2.GetFavorites();

        Assert.AreEqual(1, favorites.Count);
        Assert.AreEqual("Portland", favorites[0].Name);
        Assert.AreEqual(45.5152, favorites[0].Latitude);
        Assert.AreEqual(-122.6784, favorites[0].Longitude);
    }

    // ---------------------------------------------------------------
    // Unfavorite() — remove
    // ---------------------------------------------------------------

    [TestMethod]
    public void Unfavorite_RemovesLocationFromGetFavorites()
    {
        var manager = new FavoritesManager(_tempFilePath);
        var location = new GeocodingResult
        {
            Name = "Seattle",
            Latitude = 47.6,
            Longitude = -122.3,
        };

        manager.Favorite(location);
        manager.Unfavorite(location);

        Assert.AreEqual(0, manager.GetFavorites().Count);
    }

    [TestMethod]
    public void Unfavorite_NonFavoritedLocation_IsNoOp()
    {
        var manager = new FavoritesManager(_tempFilePath);
        var location = new GeocodingResult
        {
            Name = "Seattle",
            Latitude = 47.6,
            Longitude = -122.3,
        };

        // Should not throw
        manager.Unfavorite(location);

        Assert.AreEqual(0, manager.GetFavorites().Count);
    }

    [TestMethod]
    public void Unfavorite_PersistsRemovalToFile()
    {
        var location = new GeocodingResult { Name = "Seattle", Latitude = 47.6, Longitude = -122.3 };

        var manager1 = new FavoritesManager(_tempFilePath);
        manager1.Favorite(location);
        manager1.Unfavorite(location);

        var manager2 = new FavoritesManager(_tempFilePath);
        Assert.AreEqual(0, manager2.GetFavorites().Count);
    }

    [TestMethod]
    public void Unfavorite_OnlyRemovesMatchingLocation()
    {
        var manager = new FavoritesManager(_tempFilePath);
        var seattle = new GeocodingResult { Name = "Seattle", Latitude = 47.6, Longitude = -122.3 };
        var portland = new GeocodingResult { Name = "Portland", Latitude = 45.5, Longitude = -122.7 };

        manager.Favorite(seattle);
        manager.Favorite(portland);
        manager.Unfavorite(seattle);

        var favorites = manager.GetFavorites();
        Assert.AreEqual(1, favorites.Count);
        Assert.AreEqual("Portland", favorites[0].Name);
    }

    // ---------------------------------------------------------------
    // IsFavorite()
    // ---------------------------------------------------------------

    [TestMethod]
    public void IsFavorite_ReturnsTrueForFavoritedLocation()
    {
        var manager = new FavoritesManager(_tempFilePath);
        var location = new GeocodingResult { Name = "Seattle", Latitude = 47.6, Longitude = -122.3 };

        manager.Favorite(location);

        Assert.IsTrue(manager.IsFavorite(location));
    }

    [TestMethod]
    public void IsFavorite_ReturnsFalseForUnfavoritedLocation()
    {
        var manager = new FavoritesManager(_tempFilePath);
        var location = new GeocodingResult { Name = "Seattle", Latitude = 47.6, Longitude = -122.3 };

        Assert.IsFalse(manager.IsFavorite(location));
    }

    [TestMethod]
    public void IsFavorite_ReturnsFalseAfterUnfavorite()
    {
        var manager = new FavoritesManager(_tempFilePath);
        var location = new GeocodingResult { Name = "Seattle", Latitude = 47.6, Longitude = -122.3 };

        manager.Favorite(location);
        manager.Unfavorite(location);

        Assert.IsFalse(manager.IsFavorite(location));
    }

    // ---------------------------------------------------------------
    // GetFavorites()
    // ---------------------------------------------------------------

    [TestMethod]
    public void GetFavorites_WithNoFavorites_ReturnsEmpty()
    {
        var manager = new FavoritesManager(_tempFilePath);

        var favorites = manager.GetFavorites();

        Assert.IsNotNull(favorites);
        Assert.AreEqual(0, favorites.Count);
    }

    [TestMethod]
    public void GetFavorites_ReturnsAllFavoritedLocations()
    {
        var manager = new FavoritesManager(_tempFilePath);
        manager.Favorite(new GeocodingResult { Name = "Seattle", Latitude = 47.6, Longitude = -122.3 });
        manager.Favorite(new GeocodingResult { Name = "Portland", Latitude = 45.5, Longitude = -122.7 });
        manager.Favorite(new GeocodingResult { Name = "Vancouver", Latitude = 49.3, Longitude = -123.1 });

        Assert.AreEqual(3, manager.GetFavorites().Count);
    }

    [TestMethod]
    public void GetFavorites_ReturnsCopy_NotLiveReference()
    {
        var manager = new FavoritesManager(_tempFilePath);
        manager.Favorite(new GeocodingResult { Name = "Seattle", Latitude = 47.6, Longitude = -122.3 });

        var snapshot = manager.GetFavorites();
        manager.Favorite(new GeocodingResult { Name = "Portland", Latitude = 45.5, Longitude = -122.7 });

        // snapshot should still have count 1 — GetFavorites returns a copy
        Assert.AreEqual(1, snapshot.Count);
    }

    // ---------------------------------------------------------------
    // FavoritesChanged event
    // ---------------------------------------------------------------

    [TestMethod]
    public void FavoritesChanged_FiresOnFavorite()
    {
        var manager = new FavoritesManager(_tempFilePath);
        var location = new GeocodingResult { Name = "Seattle", Latitude = 47.6, Longitude = -122.3 };
        var eventFired = false;
        manager.FavoritesChanged += (_, _) => eventFired = true;

        manager.Favorite(location);

        Assert.IsTrue(eventFired, "FavoritesChanged should fire when a location is favorited");
    }

    [TestMethod]
    public void FavoritesChanged_FiresOnUnfavorite()
    {
        var manager = new FavoritesManager(_tempFilePath);
        var location = new GeocodingResult { Name = "Seattle", Latitude = 47.6, Longitude = -122.3 };
        manager.Favorite(location);

        var eventFired = false;
        manager.FavoritesChanged += (_, _) => eventFired = true;

        manager.Unfavorite(location);

        Assert.IsTrue(eventFired, "FavoritesChanged should fire when a location is unfavorited");
    }

    [TestMethod]
    public void FavoritesChanged_DoesNotFireOnDuplicateFavorite()
    {
        var manager = new FavoritesManager(_tempFilePath);
        var location = new GeocodingResult { Name = "Seattle", Latitude = 47.6, Longitude = -122.3 };
        manager.Favorite(location);

        var eventFired = false;
        manager.FavoritesChanged += (_, _) => eventFired = true;

        manager.Favorite(location); // duplicate — should be no-op

        Assert.IsFalse(eventFired, "FavoritesChanged should NOT fire when favoriting a duplicate location");
    }

    [TestMethod]
    public void FavoritesChanged_DoesNotFireOnUnfavoriteNonExistent()
    {
        var manager = new FavoritesManager(_tempFilePath);
        var location = new GeocodingResult { Name = "Seattle", Latitude = 47.6, Longitude = -122.3 };

        var eventFired = false;
        manager.FavoritesChanged += (_, _) => eventFired = true;

        manager.Unfavorite(location); // not in list — should be no-op

        Assert.IsFalse(eventFired, "FavoritesChanged should NOT fire when unfavoriting a location that isn't in the list");
    }

    // ---------------------------------------------------------------
    // Constructor / persistence
    // ---------------------------------------------------------------

    [TestMethod]
    public void Constructor_WithMissingFile_StartsEmpty()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"weather-test-no-file-{Guid.NewGuid()}.json");

        var manager = new FavoritesManager(nonExistentPath);

        Assert.AreEqual(0, manager.GetFavorites().Count);
    }

    [TestMethod]
    public void Constructor_LoadsExistingDataFromFile()
    {
        var location = new GeocodingResult
        {
            Name = "Vancouver",
            Latitude = 49.3,
            Longitude = -123.1,
            Admin1 = "British Columbia",
            Country = "Canada",
        };

        var manager1 = new FavoritesManager(_tempFilePath);
        manager1.Favorite(location);

        var manager2 = new FavoritesManager(_tempFilePath);

        Assert.AreEqual(1, manager2.GetFavorites().Count);
        Assert.AreEqual("Vancouver", manager2.GetFavorites()[0].Name);
    }
}
