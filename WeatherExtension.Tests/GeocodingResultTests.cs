// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CmdPal.Ext.Weather.Models;

namespace Microsoft.CmdPal.Ext.Weather.UnitTests;

[TestClass]
public class GeocodingResultTests
{
    [TestMethod]
    public void DisplayName_WithAllParts_ReturnsFormattedString()
    {
        var result = new GeocodingResult
        {
            Name = "Seattle",
            Admin1 = "Washington",
            Country = "United States",
        };

        Assert.AreEqual("Seattle, Washington, United States", result.DisplayName);
    }

    [TestMethod]
    public void DisplayName_WithMissingAdmin1_ReturnsNameAndCountry()
    {
        var result = new GeocodingResult
        {
            Name = "Seattle",
            Admin1 = null,
            Country = "United States",
        };

        Assert.AreEqual("Seattle, United States", result.DisplayName);
    }

    [TestMethod]
    public void DisplayName_WithMissingCountry_ReturnsNameAndAdmin1()
    {
        var result = new GeocodingResult
        {
            Name = "Seattle",
            Admin1 = "Washington",
            Country = null,
        };

        Assert.AreEqual("Seattle, Washington", result.DisplayName);
    }

    [TestMethod]
    public void DisplayName_WithOnlyName_ReturnsName()
    {
        var result = new GeocodingResult
        {
            Name = "Seattle",
            Admin1 = null,
            Country = null,
        };

        Assert.AreEqual("Seattle", result.DisplayName);
    }

    [TestMethod]
    public void DisplayName_WithEmptyStrings_IgnoresEmptyStrings()
    {
        var result = new GeocodingResult
        {
            Name = "Seattle",
            Admin1 = string.Empty,
            Country = "United States",
        };

        Assert.AreEqual("Seattle, United States", result.DisplayName);
    }

    [TestMethod]
    public void DisplayName_WithWhitespaceStrings_IgnoresWhitespace()
    {
        var result = new GeocodingResult
        {
            Name = "Seattle",
            Admin1 = "   ",
            Country = "United States",
        };

        Assert.AreEqual("Seattle, United States", result.DisplayName);
    }

    [TestMethod]
    public void DisplayName_WithAllNullFields_ReturnsEmptyString()
    {
        var result = new GeocodingResult
        {
            Name = null,
            Admin1 = null,
            Country = null,
        };

        Assert.AreEqual(string.Empty, result.DisplayName);
    }

    [TestMethod]
    public void DisplayName_WithAllEmptyFields_ReturnsEmptyString()
    {
        var result = new GeocodingResult
        {
            Name = string.Empty,
            Admin1 = string.Empty,
            Country = string.Empty,
        };

        Assert.AreEqual(string.Empty, result.DisplayName);
    }

    [TestMethod]
    public void DisplayName_WithOnlyAdmin1AndCountry_ReturnsThemJoined()
    {
        var result = new GeocodingResult
        {
            Name = null,
            Admin1 = "Washington",
            Country = "United States",
        };

        Assert.AreEqual("Washington, United States", result.DisplayName);
    }

    [TestMethod]
    public void DisplayName_WithOnlyCountry_ReturnsCountry()
    {
        var result = new GeocodingResult
        {
            Name = null,
            Admin1 = null,
            Country = "United States",
        };

        Assert.AreEqual("United States", result.DisplayName);
    }
}
