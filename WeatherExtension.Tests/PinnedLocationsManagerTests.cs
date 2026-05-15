// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.CmdPal.Ext.Weather.Models;
using Microsoft.CmdPal.Ext.Weather.Services;

namespace Microsoft.CmdPal.Ext.Weather.UnitTests;

[TestClass]
public class PinnedLocationsManagerTests
{
    private string _tempFilePath = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"weather-test-pinned-{Guid.NewGuid()}.json");
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    [TestMethod]
    public void PinLocation_AddsLocationToGetPinnedLocations()
    {
        var manager = new PinnedLocationsManager(_tempFilePath);
        var location = new GeocodingResult
        {
            Name = "Seattle",
            Latitude = 47.6062,
            Longitude = -122.3321
        };

        manager.Pin(location);

        var pinned = manager.GetPinnedLocations();
        Assert.AreEqual(1, pinned.Count);
        Assert.AreEqual("Seattle", pinned[0].Name);
        Assert.AreEqual(47.6062, pinned[0].Latitude);
        Assert.AreEqual(-122.3321, pinned[0].Longitude);
    }

    [TestMethod]
    public void PinDuplicateLocation_DoesNotCreateDuplicates()
    {
        var manager = new PinnedLocationsManager(_tempFilePath);
        var location = new GeocodingResult
        {
            Name = "Seattle",
            Latitude = 47.6062,
            Longitude = -122.3321
        };

        manager.Pin(location);
        manager.Pin(location);

        var pinned = manager.GetPinnedLocations();
        Assert.AreEqual(1, pinned.Count);
    }

    [TestMethod]
    public void UnpinLocation_RemovesLocationFromGetPinnedLocations()
    {
        var manager = new PinnedLocationsManager(_tempFilePath);
        var location = new GeocodingResult
        {
            Name = "Seattle",
            Latitude = 47.6062,
            Longitude = -122.3321
        };

        manager.Pin(location);
        manager.Unpin(location);

        var pinned = manager.GetPinnedLocations();
        Assert.AreEqual(0, pinned.Count);
    }

    [TestMethod]
    public void IsPinned_ReturnsTrueForPinnedLocation()
    {
        var manager = new PinnedLocationsManager(_tempFilePath);
        var location = new GeocodingResult
        {
            Name = "Seattle",
            Latitude = 47.6062,
            Longitude = -122.3321
        };

        manager.Pin(location);

        Assert.IsTrue(manager.IsPinned(location));
    }

    [TestMethod]
    public void IsPinned_ReturnsFalseForUnpinnedLocation()
    {
        var manager = new PinnedLocationsManager(_tempFilePath);
        var location = new GeocodingResult
        {
            Name = "Seattle",
            Latitude = 47.6062,
            Longitude = -122.3321
        };

        Assert.IsFalse(manager.IsPinned(location));
    }

    [TestMethod]
    public void Persistence_PinnedLocationsPersistedAcrossInstances()
    {
        var location = new GeocodingResult
        {
            Name = "Portland",
            Latitude = 45.5152,
            Longitude = -122.6784
        };

        var manager1 = new PinnedLocationsManager(_tempFilePath);
        manager1.Pin(location);

        var manager2 = new PinnedLocationsManager(_tempFilePath);
        var pinned = manager2.GetPinnedLocations();

        Assert.AreEqual(1, pinned.Count);
        Assert.AreEqual("Portland", pinned[0].Name);
        Assert.AreEqual(45.5152, pinned[0].Latitude);
        Assert.AreEqual(-122.6784, pinned[0].Longitude);
    }

    [TestMethod]
    public void GetPinnedLocations_WithNoPins_ReturnsEmpty()
    {
        var manager = new PinnedLocationsManager(_tempFilePath);

        var pinned = manager.GetPinnedLocations();

        Assert.IsNotNull(pinned);
        Assert.AreEqual(0, pinned.Count);
    }

    [TestMethod]
    public void PinMultipleLocations_AllAppearInGetPinnedLocations()
    {
        var manager = new PinnedLocationsManager(_tempFilePath);
        var location1 = new GeocodingResult
        {
            Name = "Seattle",
            Latitude = 47.6062,
            Longitude = -122.3321
        };
        var location2 = new GeocodingResult
        {
            Name = "Portland",
            Latitude = 45.5152,
            Longitude = -122.6784
        };
        var location3 = new GeocodingResult
        {
            Name = "Vancouver",
            Latitude = 49.2827,
            Longitude = -123.1207
        };

        manager.Pin(location1);
        manager.Pin(location2);
        manager.Pin(location3);

        var pinned = manager.GetPinnedLocations();
        Assert.AreEqual(3, pinned.Count);
    }
}
