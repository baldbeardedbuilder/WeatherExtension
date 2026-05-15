// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CmdPal.Ext.Weather.Models;
using Microsoft.CmdPal.Ext.Weather.Services;

namespace Microsoft.CmdPal.Ext.Weather.UnitTests;

[TestClass]
public class ConvertPhotonResultsTests
{
	// ── helpers ─────────────────────────────────────────────────────────────

	private static PhotonFeature MakeFeature(
		double lon,
		double lat,
		string? name = "TestCity",
		string? city = null,
		string? state = null,
		string? county = null,
		string? country = null,
		string? countryCode = null,
		long? osmId = null,
		string? osmValue = null,
		double[]? coordsOverride = null)
	{
		return new PhotonFeature
		{
			Geometry = new PhotonGeometry
			{
				Coordinates = coordsOverride ?? [lon, lat],
			},
			Properties = new PhotonProperties
			{
				Name = name,
				City = city,
				State = state,
				County = county,
				Country = country,
				CountryCode = countryCode,
				OsmId = osmId,
				OsmValue = osmValue,
			},
		};
	}

	// ── basic field mapping ──────────────────────────────────────────────────

	[TestMethod]
	public void ConvertPhotonResults_WithCityOnlyFeature_ReturnsSingleResultWithName()
	{
		var features = new List<PhotonFeature>
		{
			MakeFeature(lon: -86.80, lat: 33.52, name: "Birmingham"),
		};

		var results = GeocodingService.ConvertPhotonResults(features);

		Assert.AreEqual(1, results.Count);
		Assert.AreEqual("Birmingham", results[0].Name);
		Assert.IsNull(results[0].Admin1);
		Assert.IsNull(results[0].Country);
	}

	[TestMethod]
	public void ConvertPhotonResults_WithFullProperties_MapsAllFieldsCorrectly()
	{
		var features = new List<PhotonFeature>
		{
			MakeFeature(
				lon: -86.80,
				lat: 33.52,
				name: "Birmingham",
				state: "Alabama",
				county: "Jefferson County",
				country: "United States",
				countryCode: "US",
				osmId: 111031),
		};

		var results = GeocodingService.ConvertPhotonResults(features);

		Assert.AreEqual(1, results.Count);
		var r = results[0];
		Assert.AreEqual("Birmingham", r.Name);
		Assert.AreEqual("Alabama", r.Admin1);
		Assert.AreEqual("Jefferson County", r.Admin2);
		Assert.AreEqual("United States", r.Country);
		Assert.AreEqual("US", r.CountryCode);
		Assert.AreEqual(111031L, r.Id);
	}

	// ── CRITICAL: coordinate order ───────────────────────────────────────────

	/// <summary>
	/// GeoJSON encodes coordinates as [longitude, latitude].
	/// This test uses an asymmetric pair (10.5 != 20.5) so a swap bug cannot
	/// accidentally produce a passing test.
	/// </summary>
	[TestMethod]
	public void ConvertPhotonResults_GeoJsonCoordinates_LongitudeIsIndex0LatitudeIsIndex1()
	{
		var features = new List<PhotonFeature>
		{
			MakeFeature(lon: 10.5, lat: 20.5, name: "Anywhere"),
		};

		var results = GeocodingService.ConvertPhotonResults(features);

		Assert.AreEqual(1, results.Count);
		Assert.AreEqual(10.5, results[0].Longitude, "Longitude must come from coordinates[0].");
		Assert.AreEqual(20.5, results[0].Latitude, "Latitude must come from coordinates[1].");
	}

	// ── postcode feature ─────────────────────────────────────────────────────

	[TestMethod]
	public void ConvertPhotonResults_WithPostcodeFeature_ReturnsResultWithPostcodeAsName()
	{
		// When placesOnly=false (postal-code path), postcode entries come through.
		// Properties.Name holds the postcode string; City is absent.
		var features = new List<PhotonFeature>
		{
			MakeFeature(lon: -86.80, lat: 33.52, name: "35201", city: null, osmValue: "postcode"),
		};

		var results = GeocodingService.ConvertPhotonResults(features);

		Assert.AreEqual(1, results.Count);
		Assert.AreEqual("35201", results[0].Name);
	}

	// ── Name fallback to City ────────────────────────────────────────────────

	[TestMethod]
	public void ConvertPhotonResults_WithNullNameAndCitySet_UsesCityAsName()
	{
		var features = new List<PhotonFeature>
		{
			MakeFeature(lon: -86.80, lat: 33.52, name: null, city: "Birmingham"),
		};

		var results = GeocodingService.ConvertPhotonResults(features);

		Assert.AreEqual(1, results.Count);
		Assert.AreEqual("Birmingham", results[0].Name);
	}

	// ── OsmId fallback ───────────────────────────────────────────────────────

	[TestMethod]
	public void ConvertPhotonResults_WithNullOsmId_SetsIdToZero()
	{
		var features = new List<PhotonFeature>
		{
			MakeFeature(lon: -86.80, lat: 33.52, name: "Birmingham", osmId: null),
		};

		var results = GeocodingService.ConvertPhotonResults(features);

		Assert.AreEqual(1, results.Count);
		Assert.AreEqual(0L, results[0].Id);
	}

	// ── CountryCode casing ───────────────────────────────────────────────────

	[TestMethod]
	public void ConvertPhotonResults_WithLowercaseCountryCode_ReturnsUppercased()
	{
		var features = new List<PhotonFeature>
		{
			MakeFeature(lon: -0.09, lat: 51.50, name: "London", countryCode: "gb"),
		};

		var results = GeocodingService.ConvertPhotonResults(features);

		Assert.AreEqual(1, results.Count);
		Assert.AreEqual("GB", results[0].CountryCode);
	}

	// ── empty input ──────────────────────────────────────────────────────────

	[TestMethod]
	public void ConvertPhotonResults_WithEmptyList_ReturnsEmptyList()
	{
		var results = GeocodingService.ConvertPhotonResults([]);

		Assert.AreEqual(0, results.Count);
	}

	// ── skip rules (individual) ──────────────────────────────────────────────

	[TestMethod]
	public void ConvertPhotonResults_WithNullGeometry_SkipsFeature()
	{
		var features = new List<PhotonFeature>
		{
			new() { Geometry = null, Properties = new PhotonProperties { Name = "Nowhere" } },
		};

		var results = GeocodingService.ConvertPhotonResults(features);

		Assert.AreEqual(0, results.Count);
	}

	[TestMethod]
	public void ConvertPhotonResults_WithSingleCoordinate_SkipsFeature()
	{
		var features = new List<PhotonFeature>
		{
			new()
			{
				Geometry = new PhotonGeometry { Coordinates = [42.0] },
				Properties = new PhotonProperties { Name = "Nowhere" },
			},
		};

		var results = GeocodingService.ConvertPhotonResults(features);

		Assert.AreEqual(0, results.Count);
	}

	[TestMethod]
	public void ConvertPhotonResults_WithNullCoordinates_SkipsFeature()
	{
		var features = new List<PhotonFeature>
		{
			new()
			{
				Geometry = new PhotonGeometry { Coordinates = null },
				Properties = new PhotonProperties { Name = "Nowhere" },
			},
		};

		var results = GeocodingService.ConvertPhotonResults(features);

		Assert.AreEqual(0, results.Count);
	}

	[TestMethod]
	public void ConvertPhotonResults_WithNullNameAndNullCity_SkipsFeature()
	{
		var features = new List<PhotonFeature>
		{
			MakeFeature(lon: -86.80, lat: 33.52, name: null, city: null),
		};

		var results = GeocodingService.ConvertPhotonResults(features);

		Assert.AreEqual(0, results.Count);
	}

	[TestMethod]
	public void ConvertPhotonResults_WithWhitespaceNameAndNullCity_SkipsFeature()
	{
		var features = new List<PhotonFeature>
		{
			MakeFeature(lon: -86.80, lat: 33.52, name: "   ", city: null),
		};

		var results = GeocodingService.ConvertPhotonResults(features);

		Assert.AreEqual(0, results.Count);
	}

	// ── combined invalid + valid ─────────────────────────────────────────────

	[TestMethod]
	public void ConvertPhotonResults_WithMixedValidAndInvalidFeatures_ReturnsOnlyValid()
	{
		var features = new List<PhotonFeature>
		{
			// (a) null Geometry
			new() { Geometry = null, Properties = new PhotonProperties { Name = "Ghost A" } },
			// (b) only one coordinate
			new()
			{
				Geometry = new PhotonGeometry { Coordinates = [-86.0] },
				Properties = new PhotonProperties { Name = "Ghost B" },
			},
			// (c) no usable name -- both Name and City are null
			MakeFeature(lon: -86.80, lat: 33.52, name: null, city: null),
			// (d) valid
			MakeFeature(lon: -86.80, lat: 33.52, name: "Birmingham", state: "Alabama", country: "United States"),
		};

		var results = GeocodingService.ConvertPhotonResults(features);

		Assert.AreEqual(1, results.Count);
		Assert.AreEqual("Birmingham", results[0].Name);
	}

	// ── multiple valid features ──────────────────────────────────────────────

	[TestMethod]
	public void ConvertPhotonResults_WithMultipleValidFeatures_ReturnsAllInOrder()
	{
		var features = new List<PhotonFeature>
		{
			MakeFeature(lon: -86.80, lat: 33.52, name: "Birmingham", state: "Alabama", countryCode: "US", osmId: 1),
			MakeFeature(lon: -1.90, lat: 52.48, name: "Birmingham", state: "England", countryCode: "GB", osmId: 2),
		};

		var results = GeocodingService.ConvertPhotonResults(features);

		Assert.AreEqual(2, results.Count);
		Assert.AreEqual("Alabama", results[0].Admin1);
		Assert.AreEqual("England", results[1].Admin1);
	}
}
