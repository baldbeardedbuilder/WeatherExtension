// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Microsoft.CmdPal.Ext.Weather.Models;

internal sealed class PhotonFeature
{
	[JsonPropertyName("geometry")]
	public PhotonGeometry? Geometry { get; set; }

	[JsonPropertyName("properties")]
	public PhotonProperties? Properties { get; set; }
}

internal sealed class PhotonGeometry
{
	// GeoJSON coordinates are [longitude, latitude] — note the reversed order vs. convention.
	[JsonPropertyName("coordinates")]
	public double[]? Coordinates { get; set; }
}

internal sealed class PhotonProperties
{
	[JsonPropertyName("osm_id")]
	public long? OsmId { get; set; }

	[JsonPropertyName("name")]
	public string? Name { get; set; }

	[JsonPropertyName("city")]
	public string? City { get; set; }

	[JsonPropertyName("state")]
	public string? State { get; set; }

	[JsonPropertyName("county")]
	public string? County { get; set; }

	[JsonPropertyName("country")]
	public string? Country { get; set; }

	[JsonPropertyName("countrycode")]
	public string? CountryCode { get; set; }

	[JsonPropertyName("postcode")]
	public string? Postcode { get; set; }

	[JsonPropertyName("osm_value")]
	public string? OsmValue { get; set; }
}
