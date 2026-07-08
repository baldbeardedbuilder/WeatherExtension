// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.CmdPal.Ext.Weather.Models;
using Microsoft.CmdPal.Ext.Weather.Services;

namespace Microsoft.CmdPal.Ext.Weather.UnitTests;

[TestClass]
public class DockPinsManagerTests
{
    private string _tempFilePath = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"weather-test-dockpins-{Guid.NewGuid()}.json");
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
    // PinToDock() — add
    // ---------------------------------------------------------------

    [TestMethod]
    public void PinToDock_AddsLocationToGetDockPins()
    {
        var manager = new DockPinsManager(_tempFilePath);
        var location = new GeocodingResult
        {
            Name = "Seattle",
            Latitude = 47.6,
            Longitude = -122.3,
            Admin1 = "Washington",
            Country = "United States",
        };

        manager.PinToDock(location);

        var pins = manager.GetDockPins();
        Assert.AreEqual(1, pins.Count);
        Assert.AreEqual("Seattle", pins[0].Name);
        Assert.AreEqual(47.6, pins[0].Latitude);
        Assert.AreEqual(-122.3, pins[0].Longitude);
    }

    [TestMethod]
    public void PinToDock_SameLocationTwice_DoesNotDuplicate()
    {
        var manager = new DockPinsManager(_tempFilePath);
        var location = new GeocodingResult { Name = "Seattle", Latitude = 47.6, Longitude = -122.3 };

        manager.PinToDock(location);
        manager.PinToDock(location);

        Assert.AreEqual(1, manager.GetDockPins().Count);
    }

    [TestMethod]
    public void PinToDock_LocationWithinThreshold_DoesNotDuplicate()
    {
        var manager = new DockPinsManager(_tempFilePath);
        var location1 = new GeocodingResult { Name = "Seattle", Latitude = 47.6000, Longitude = -122.3000 };
        var location2 = new GeocodingResult { Name = "Seattle", Latitude = 47.6005, Longitude = -122.3005 };

        manager.PinToDock(location1);
        manager.PinToDock(location2);

        Assert.AreEqual(1, manager.GetDockPins().Count);
    }

    [TestMethod]
    public void PinToDock_LocationOutsideThreshold_AddsBoth()
    {
        var manager = new DockPinsManager(_tempFilePath);
        var location1 = new GeocodingResult { Name = "Seattle", Latitude = 47.6, Longitude = -122.3 };
        var location2 = new GeocodingResult { Name = "Portland", Latitude = 45.5, Longitude = -122.7 };

        manager.PinToDock(location1);
        manager.PinToDock(location2);

        Assert.AreEqual(2, manager.GetDockPins().Count);
    }

    [TestMethod]
    public void PinToDock_PersistsToFile()
    {
        var location = new GeocodingResult
        {
            Name = "Portland",
            Latitude = 45.5152,
            Longitude = -122.6784,
            Admin1 = "Oregon",
            Country = "United States",
        };

        var manager1 = new DockPinsManager(_tempFilePath);
        manager1.PinToDock(location);

        var manager2 = new DockPinsManager(_tempFilePath);
        var pins = manager2.GetDockPins();

        Assert.AreEqual(1, pins.Count);
        Assert.AreEqual("Portland", pins[0].Name);
        Assert.AreEqual(45.5152, pins[0].Latitude);
        Assert.AreEqual(-122.6784, pins[0].Longitude);
    }

    // ---------------------------------------------------------------
    // UnpinFromDock() — remove
    // ---------------------------------------------------------------

    [TestMethod]
    public void UnpinFromDock_RemovesLocationFromGetDockPins()
    {
        var manager = new DockPinsManager(_tempFilePath);
        var location = new GeocodingResult { Name = "Seattle", Latitude = 47.6, Longitude = -122.3 };

        manager.PinToDock(location);
        manager.UnpinFromDock(location);

        Assert.AreEqual(0, manager.GetDockPins().Count);
    }

    [TestMethod]
    public void UnpinFromDock_NonPinnedLocation_IsNoOp()
    {
        var manager = new DockPinsManager(_tempFilePath);
        var location = new GeocodingResult { Name = "Seattle", Latitude = 47.6, Longitude = -122.3 };

        manager.UnpinFromDock(location);

        Assert.AreEqual(0, manager.GetDockPins().Count);
    }

    [TestMethod]
    public void UnpinFromDock_PersistsRemovalToFile()
    {
        var location = new GeocodingResult { Name = "Seattle", Latitude = 47.6, Longitude = -122.3 };

        var manager1 = new DockPinsManager(_tempFilePath);
        manager1.PinToDock(location);
        manager1.UnpinFromDock(location);

        var manager2 = new DockPinsManager(_tempFilePath);
        Assert.AreEqual(0, manager2.GetDockPins().Count);
    }

    [TestMethod]
    public void UnpinFromDock_OnlyRemovesMatchingLocation()
    {
        var manager = new DockPinsManager(_tempFilePath);
        var seattle = new GeocodingResult { Name = "Seattle", Latitude = 47.6, Longitude = -122.3 };
        var portland = new GeocodingResult { Name = "Portland", Latitude = 45.5, Longitude = -122.7 };

        manager.PinToDock(seattle);
        manager.PinToDock(portland);
        manager.UnpinFromDock(seattle);

        var pins = manager.GetDockPins();
        Assert.AreEqual(1, pins.Count);
        Assert.AreEqual("Portland", pins[0].Name);
    }

    // ---------------------------------------------------------------
    // IsPinnedToDock()
    // ---------------------------------------------------------------

    [TestMethod]
    public void IsPinnedToDock_ReturnsTrueForPinnedLocation()
    {
        var manager = new DockPinsManager(_tempFilePath);
        var location = new GeocodingResult { Name = "Seattle", Latitude = 47.6, Longitude = -122.3 };

        manager.PinToDock(location);

        Assert.IsTrue(manager.IsPinnedToDock(location));
    }

    [TestMethod]
    public void IsPinnedToDock_ReturnsFalseForUnpinnedLocation()
    {
        var manager = new DockPinsManager(_tempFilePath);
        var location = new GeocodingResult { Name = "Seattle", Latitude = 47.6, Longitude = -122.3 };

        Assert.IsFalse(manager.IsPinnedToDock(location));
    }

    [TestMethod]
    public void IsPinnedToDock_ReturnsFalseAfterUnpin()
    {
        var manager = new DockPinsManager(_tempFilePath);
        var location = new GeocodingResult { Name = "Seattle", Latitude = 47.6, Longitude = -122.3 };

        manager.PinToDock(location);
        manager.UnpinFromDock(location);

        Assert.IsFalse(manager.IsPinnedToDock(location));
    }

    // ---------------------------------------------------------------
    // GetDockPins()
    // ---------------------------------------------------------------

    [TestMethod]
    public void GetDockPins_WithNoPins_ReturnsEmpty()
    {
        var manager = new DockPinsManager(_tempFilePath);

        var pins = manager.GetDockPins();

        Assert.IsNotNull(pins);
        Assert.AreEqual(0, pins.Count);
    }

    [TestMethod]
    public void GetDockPins_ReturnsAllPinnedLocations()
    {
        var manager = new DockPinsManager(_tempFilePath);
        manager.PinToDock(new GeocodingResult { Name = "Seattle", Latitude = 47.6, Longitude = -122.3 });
        manager.PinToDock(new GeocodingResult { Name = "Portland", Latitude = 45.5, Longitude = -122.7 });
        manager.PinToDock(new GeocodingResult { Name = "Vancouver", Latitude = 49.3, Longitude = -123.1 });

        Assert.AreEqual(3, manager.GetDockPins().Count);
    }

    // ---------------------------------------------------------------
    // DockPinsChanged event
    // ---------------------------------------------------------------

    [TestMethod]
    public void DockPinsChanged_FiresOnPin()
    {
        var manager = new DockPinsManager(_tempFilePath);
        var location = new GeocodingResult { Name = "Seattle", Latitude = 47.6, Longitude = -122.3 };
        var eventFired = false;
        manager.DockPinsChanged += (_, _) => eventFired = true;

        manager.PinToDock(location);

        Assert.IsTrue(eventFired, "DockPinsChanged should fire when a location is pinned");
    }

    [TestMethod]
    public void DockPinsChanged_FiresOnUnpin()
    {
        var manager = new DockPinsManager(_tempFilePath);
        var location = new GeocodingResult { Name = "Seattle", Latitude = 47.6, Longitude = -122.3 };
        manager.PinToDock(location);

        var eventFired = false;
        manager.DockPinsChanged += (_, _) => eventFired = true;

        manager.UnpinFromDock(location);

        Assert.IsTrue(eventFired, "DockPinsChanged should fire when a location is unpinned");
    }

    [TestMethod]
    public void DockPinsChanged_DoesNotFireOnDuplicatePin()
    {
        var manager = new DockPinsManager(_tempFilePath);
        var location = new GeocodingResult { Name = "Seattle", Latitude = 47.6, Longitude = -122.3 };
        manager.PinToDock(location);

        var eventFired = false;
        manager.DockPinsChanged += (_, _) => eventFired = true;

        manager.PinToDock(location);

        Assert.IsFalse(eventFired, "DockPinsChanged should NOT fire when pinning a duplicate location");
    }

    [TestMethod]
    public void DockPinsChanged_DoesNotFireOnUnpinNonExistent()
    {
        var manager = new DockPinsManager(_tempFilePath);
        var location = new GeocodingResult { Name = "Seattle", Latitude = 47.6, Longitude = -122.3 };

        var eventFired = false;
        manager.DockPinsChanged += (_, _) => eventFired = true;

        manager.UnpinFromDock(location);

        Assert.IsFalse(eventFired, "DockPinsChanged should NOT fire when unpinning a location that isn't pinned");
    }

    // ---------------------------------------------------------------
    // Constructor / persistence
    // ---------------------------------------------------------------

    [TestMethod]
    public void Constructor_WithMissingFile_StartsEmpty()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"weather-test-no-dock-{Guid.NewGuid()}.json");

        var manager = new DockPinsManager(nonExistentPath);

        Assert.AreEqual(0, manager.GetDockPins().Count);
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

        var manager1 = new DockPinsManager(_tempFilePath);
        manager1.PinToDock(location);

        var manager2 = new DockPinsManager(_tempFilePath);

        Assert.AreEqual(1, manager2.GetDockPins().Count);
        Assert.AreEqual("Vancouver", manager2.GetDockPins()[0].Name);
    }

    [TestMethod]
    public void DockPins_AreIndependentFromFavorites()
    {
        var favoritesPath = Path.Combine(Path.GetTempPath(), $"weather-test-fav-indep-{Guid.NewGuid()}.json");
        try
        {
            var favorites = new FavoritesManager(favoritesPath);
            var dockPins = new DockPinsManager(_tempFilePath);
            var location = new GeocodingResult { Name = "Seattle", Latitude = 47.6, Longitude = -122.3 };

            favorites.Favorite(location);

            // Favoriting a location must not pin it to the dock.
            Assert.IsFalse(dockPins.IsPinnedToDock(location));
            Assert.AreEqual(0, dockPins.GetDockPins().Count);

            dockPins.PinToDock(location);

            // Pinning to the dock must not affect favorites membership either way.
            Assert.IsTrue(favorites.IsFavorite(location));
            Assert.IsTrue(dockPins.IsPinnedToDock(location));
        }
        finally
        {
            if (File.Exists(favoritesPath))
            {
                File.Delete(favoritesPath);
            }
        }
    }
}
