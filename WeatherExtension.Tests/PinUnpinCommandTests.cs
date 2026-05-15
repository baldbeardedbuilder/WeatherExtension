// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.CmdPal.Ext.Weather.Commands;
using Microsoft.CmdPal.Ext.Weather.Models;
using Microsoft.CmdPal.Ext.Weather.Services;

namespace Microsoft.CmdPal.Ext.Weather.UnitTests;

[TestClass]
public class PinUnpinCommandTests
{
    private string _tempFilePath = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"weather-test-cmd-{Guid.NewGuid()}.json");
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
    public void PinToDockCommand_InvokePinsLocation()
    {
        var manager = new PinnedLocationsManager(_tempFilePath);
        var location = new GeocodingResult
        {
            Name = "Seattle",
            Latitude = 47.6062,
            Longitude = -122.3321
        };
        var command = new PinToDockCommand(location, manager);

        command.Invoke();

        Assert.IsTrue(manager.IsPinned(location));
    }

    [TestMethod]
    public void UnpinFromDockCommand_InvokeUnpinsLocation()
    {
        var manager = new PinnedLocationsManager(_tempFilePath);
        var location = new GeocodingResult
        {
            Name = "Portland",
            Latitude = 45.5152,
            Longitude = -122.6784
        };
        manager.Pin(location);

        var command = new UnpinFromDockCommand(location, manager);
        command.Invoke();

        Assert.IsFalse(manager.IsPinned(location));
    }

    [TestMethod]
    public void PinToDockCommand_HasCorrectName()
    {
        var manager = new PinnedLocationsManager(_tempFilePath);
        var location = new GeocodingResult
        {
            Name = "Seattle",
            Latitude = 47.6062,
            Longitude = -122.3321
        };
        var command = new PinToDockCommand(location, manager);

        Assert.IsNotNull(command.Name);
        Assert.IsFalse(string.IsNullOrEmpty(command.Name));
    }

    [TestMethod]
    public void PinToDockCommand_HasIcon()
    {
        var manager = new PinnedLocationsManager(_tempFilePath);
        var location = new GeocodingResult
        {
            Name = "Seattle",
            Latitude = 47.6062,
            Longitude = -122.3321
        };
        var command = new PinToDockCommand(location, manager);

        Assert.IsNotNull(command.Icon);
    }

    [TestMethod]
    public void UnpinFromDockCommand_HasCorrectName()
    {
        var manager = new PinnedLocationsManager(_tempFilePath);
        var location = new GeocodingResult
        {
            Name = "Portland",
            Latitude = 45.5152,
            Longitude = -122.6784
        };
        var command = new UnpinFromDockCommand(location, manager);

        Assert.IsNotNull(command.Name);
        Assert.IsFalse(string.IsNullOrEmpty(command.Name));
    }

    [TestMethod]
    public void UnpinFromDockCommand_HasIcon()
    {
        var manager = new PinnedLocationsManager(_tempFilePath);
        var location = new GeocodingResult
        {
            Name = "Portland",
            Latitude = 45.5152,
            Longitude = -122.6784
        };
        var command = new UnpinFromDockCommand(location, manager);

        Assert.IsNotNull(command.Icon);
    }
}
