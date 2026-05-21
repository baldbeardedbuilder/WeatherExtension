// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CmdPal.Ext.Weather.Models;
using Microsoft.CmdPal.Ext.Weather.Services;

namespace Microsoft.CmdPal.Ext.Weather.UnitTests;

[TestClass]
public class GeocodingServiceTests
{
	[DataTestMethod]
	[DataRow("90210")] // Valid US ZIP code
	[DataRow("K1A 0B1")] // Valid Canadian postal code
	[DataRow("k1a 0b1")] // Valid Canadian postal code in lowercase
	[DataRow("SW1A 1AA")] // Valid UK postal codes
	[DataRow("SW1A1AA")] // Valid UK postal codes without space
	[DataRow("sw1a 1aa")] // Valid UK postal codes in lowercase
	[DataRow("M1 1AJ")] // Valid UK postal codes
	public async Task SearchLocationAsync_WithPostalCodeFormats_UsesPostalCodeQueryParameter(string input)
	{
		var seenUris = new List<Uri>();
		var handler = new CountingHttpHandler(_ =>
		new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("[]"),
		},
		request => seenUris.Add(request.RequestUri!));

		using var service = new GeocodingService(handler);
		var results = await service.SearchLocationAsync(input, CancellationToken.None);

		var decodedQuery = Uri.UnescapeDataString(seenUris[0].Query);
		Assert.IsTrue(decodedQuery.Contains($"postalcode={input}", StringComparison.Ordinal), "Expected postcode input to be sent via postalcode parameter.");
	}

	[TestMethod]
	public void RankResults_WithEmptyQuery_ReturnsOriginalOrder()
	{
		var results = new List<GeocodingResult>
		{
			new() { Name = "Portland" },
			new() { Name = "Seattle" },
		};

		var ranked = GeocodingService.RankResults(string.Empty, results);

		Assert.AreEqual("Portland", ranked[0].Name);
		Assert.AreEqual("Seattle", ranked[1].Name);
	}

	[TestMethod]
	public void RankResults_WithSingleResult_ReturnsOriginalList()
	{
		var results = new List<GeocodingResult>
		{
			new() { Name = "Seattle" },
		};

		var ranked = GeocodingService.RankResults("Seattle", results);

		Assert.AreEqual(1, ranked.Count);
		Assert.AreEqual("Seattle", ranked[0].Name);
	}

	[TestMethod]
	public void RankResults_ExactMatchRankedFirst()
	{
		var results = new List<GeocodingResult>
		{
			new() { Name = "New Seattle" },
			new() { Name = "Seattle Heights" },
			new() { Name = "Seattle" },
		};

		var ranked = GeocodingService.RankResults("Seattle", results);

		Assert.AreEqual("Seattle", ranked[0].Name);
	}

	[TestMethod]
	public void RankResults_StartsWithRankedBeforeContains()
	{
		var results = new List<GeocodingResult>
		{
			new() { Name = "East Seattle" },
			new() { Name = "Seattle Heights" },
		};

		var ranked = GeocodingService.RankResults("Seattle", results);

		Assert.AreEqual("Seattle Heights", ranked[0].Name);
		Assert.AreEqual("East Seattle", ranked[1].Name);
	}

	[TestMethod]
	public void RankResults_IsCaseInsensitive()
	{
		var results = new List<GeocodingResult>
		{
			new() { Name = "Seattle Heights" },
			new() { Name = "SEATTLE" },
		};

		var ranked = GeocodingService.RankResults("seattle", results);

		Assert.AreEqual("SEATTLE", ranked[0].Name);
		Assert.AreEqual("Seattle Heights", ranked[1].Name);
	}

	[TestMethod]
	public void RankResults_WithCommaInQuery_UsesOnlyCityPart()
	{
		var results = new List<GeocodingResult>
		{
			new() { Name = "Portland" },
			new() { Name = "Seattle" },
		};

		var ranked = GeocodingService.RankResults("Seattle, WA", results);

		Assert.AreEqual("Seattle", ranked[0].Name);
		Assert.AreEqual("Portland", ranked[1].Name);
	}

	[TestMethod]
	public void RankResults_NoMatchResultsRetainOriginalRelativeOrder()
	{
		var results = new List<GeocodingResult>
		{
			new() { Name = "Portland" },
			new() { Name = "Denver" },
			new() { Name = "Houston" },
		};

		var ranked = GeocodingService.RankResults("Seattle", results);

		Assert.AreEqual(3, ranked.Count);
		Assert.AreEqual("Portland", ranked[0].Name);
		Assert.AreEqual("Denver", ranked[1].Name);
		Assert.AreEqual("Houston", ranked[2].Name);
	}

	[TestMethod]
	public void RankResults_ExactMatchBeforeStartsWithBeforeContains()
	{
		var results = new List<GeocodingResult>
		{
			new() { Name = "East Seattle" },
			new() { Name = "Seattle" },
			new() { Name = "Seattle Heights" },
		};

		var ranked = GeocodingService.RankResults("Seattle", results);

		Assert.AreEqual("Seattle", ranked[0].Name);
		Assert.AreEqual("Seattle Heights", ranked[1].Name);
		Assert.AreEqual("East Seattle", ranked[2].Name);
	}

	[TestMethod]
	public void RankResults_WithEmptyResults_ReturnsEmptyList()
	{
		var ranked = GeocodingService.RankResults("Seattle", []);

		Assert.AreEqual(0, ranked.Count);
	}

	[TestMethod]
	public async Task SearchLocationAsync_WhenAlreadyCanceled_ReturnsEmptyWithoutCallingApi()
	{
		var callCount = 0;
		var handler = new CountingHttpHandler(_ =>
		{
			callCount++;
			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("[]"),
			};
		});

		using var service = new GeocodingService(handler);
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		var results = await service.SearchLocationAsync("Seattle, WA, US", cts.Token);

		Assert.AreEqual(0, results.Count);
		Assert.AreEqual(0, callCount, "No API calls should be made when cancellation is already requested.");
	}

	[TestMethod]
	public void MaxFallbackAttempts_IsFiveToAllowSegmentRetry()
	{
		Assert.AreEqual(5, GeocodingService.MaxFallbackAttempts);
	}

	[TestMethod]
	public async Task SearchLocationAsync_LongCommaInput_CapsAttemptsAtMaxFallbackAttempts()
	{
		var callCount = 0;
		var handler = new CountingHttpHandler(_ =>
		{
			callCount++;
			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("[]"),
			};
		});

		using var service = new GeocodingService(handler);
		// 5 comma-separated segments would produce 5 attempts without the cap
		var results = await service.SearchLocationAsync("City, Region, Province, Country, Extra", CancellationToken.None);

		Assert.AreEqual(0, results.Count);
		Assert.IsTrue(callCount <= GeocodingService.MaxFallbackAttempts,
			$"Expected at most {GeocodingService.MaxFallbackAttempts} API calls, but got {callCount}.");
	}


	// ── Nominatim→Photon fallback integration tests ─────────────────────────

	private const string ValidPhotonBirminghamJson = """
		{
			"type": "FeatureCollection",
			"features": [
				{
					"type": "Feature",
					"geometry": {
						"type": "Point",
						"coordinates": [-86.8025, 33.5207]
					},
					"properties": {
						"name": "Birmingham",
						"state": "Alabama",
						"country": "United States",
						"osm_key": "place",
						"osm_value": "city"
					}
				}
			]
		}
		""";

	private const string ValidPhotonTokyoJson = """
		{
			"type": "FeatureCollection",
			"features": [
				{
					"type": "Feature",
					"geometry": {
						"type": "Point",
						"coordinates": [139.6917, 35.6895]
					},
					"properties": {
						"name": "Tokyo",
						"country": "Japan",
						"osm_key": "place",
						"osm_value": "city"
					}
				}
			]
		}
		""";

	[TestMethod]
	public async Task SearchLocationAsync_NominatimFails_FallsBackToPhoton()
	{
		var callCount = 0;
		var handler = new CountingHttpHandler(request =>
		{
			callCount++;
			if (request.RequestUri!.Host.Contains("nominatim.openstreetmap.org", StringComparison.OrdinalIgnoreCase))
			{
				return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
			}

			// Photon
			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(ValidPhotonBirminghamJson),
			};
		});

		using var service = new GeocodingService(handler);
		var results = await service.SearchLocationAsync("Birmingham, Alabama", CancellationToken.None);

		Assert.IsTrue(results.Count > 0, "Expected non-empty results from Photon fallback");
		Assert.AreEqual(2, callCount, "Expected 1 failed Nominatim call + 1 successful Photon call");

		// Photon coordinates are [lon, lat] — ConvertPhotonResults swaps them
		Assert.AreEqual(33.5207, results[0].Latitude, 0.001, "Latitude should match Photon payload");
		Assert.AreEqual(-86.8025, results[0].Longitude, 0.001, "Longitude should match Photon payload");
	}

	[TestMethod]
	public async Task SearchLocationAsync_NominatimThrows_FallsBackToPhoton()
	{
		var callCount = 0;
		var handler = new CountingHttpHandler(request =>
		{
			callCount++;
			if (request.RequestUri!.Host.Contains("nominatim.openstreetmap.org", StringComparison.OrdinalIgnoreCase))
			{
				throw new HttpRequestException("Connection refused");
			}

			// Photon
			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(ValidPhotonTokyoJson),
			};
		});

		using var service = new GeocodingService(handler);
		var results = await service.SearchLocationAsync("Tokyo, Japan", CancellationToken.None);

		Assert.IsTrue(results.Count > 0, "Expected non-empty results from Photon fallback after Nominatim throws");
		Assert.AreEqual(2, callCount, "Expected 1 thrown Nominatim call + 1 successful Photon call");

		// Photon coordinates are [lon, lat]
		Assert.AreEqual(35.6895, results[0].Latitude, 0.001, "Latitude should match Photon payload");
		Assert.AreEqual(139.6917, results[0].Longitude, 0.001, "Longitude should match Photon payload");
	}

	[TestMethod]
	public async Task SearchLocationAsync_PostalCode_NominatimFails_FallsBackToPhoton()
	{
		var callCount = 0;
		var handler = new CountingHttpHandler(request =>
		{
			callCount++;
			if (request.RequestUri!.Host.Contains("nominatim.openstreetmap.org", StringComparison.OrdinalIgnoreCase))
			{
				return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
			}

			// Photon
			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(ValidPhotonBirminghamJson),
			};
		});

		using var service = new GeocodingService(handler);
		// US zip triggers SearchPostalCodeAsync path
		var results = await service.SearchLocationAsync("90210", CancellationToken.None);

		Assert.IsTrue(results.Count > 0, "Expected non-empty results from Photon fallback for postal code");
		// The postal code path tries Nominatim's postalcode= endpoint, then a
		// free-text Nominatim retry, before finally falling back to Photon.
		Assert.AreEqual(3, callCount, "Expected 2 failed Nominatim postal-code calls + 1 successful Photon call");
	}

	[TestMethod]
	public async Task SearchLocationAsync_BothProvidersFail_ReturnsEmpty()
	{
		var callCount = 0;
		var handler = new CountingHttpHandler(request =>
		{
			callCount++;
			return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
		});

		using var service = new GeocodingService(handler);
		var results = await service.SearchLocationAsync("Birmingham, Alabama", CancellationToken.None);

		Assert.AreEqual(0, results.Count, "Expected empty results when both providers fail");
		Assert.IsTrue(
			callCount <= GeocodingService.MaxFallbackAttempts,
			$"Expected at most {GeocodingService.MaxFallbackAttempts} HTTP calls, got {callCount}");
	}

	// ── Header policy / locale stability ───────────────────────────────────

	[TestMethod]
	public async Task SearchLocationAsync_SendsContactUserAgentAndAcceptLanguage()
	{
		HttpRequestMessage? captured = null;
		var handler = new CountingHttpHandler(
			_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]") },
			request => captured ??= request);

		using var service = new GeocodingService(handler);
		await service.SearchLocationAsync("Seattle", CancellationToken.None);

		Assert.IsNotNull(captured, "Expected at least one outgoing request");

		var ua = string.Join(' ', captured!.Headers.UserAgent.Select(p => p.ToString()));
		Assert.IsTrue(
			ua.Contains("PowerToys-CmdPal-Weather", StringComparison.Ordinal),
			$"User-Agent should identify the extension. Got: '{ua}'");
		Assert.IsTrue(
			ua.Contains("github.com", StringComparison.OrdinalIgnoreCase),
			$"User-Agent should include a contact URL per Nominatim policy. Got: '{ua}'");

		var acceptLang = string.Join(',', captured.Headers.AcceptLanguage.Select(v => v.Value));
		Assert.IsTrue(
			acceptLang.Contains("en", StringComparison.OrdinalIgnoreCase),
			$"Accept-Language should request English to keep display names stable. Got: '{acceptLang}'");
	}

	// ── Postal code free-text retry ────────────────────────────────────────

	private const string ValidNominatimIstanbulPostcodeJson = """
		[{
			"place_id": 378361098,
			"licence": "Data",
			"lat": "41.0077058",
			"lon": "28.9795504",
			"class": "boundary",
			"type": "postal_code",
			"name": "34122",
			"display_name": "34122, Cankurtaran Mahallesi, İstanbul, Fatih, İstanbul, Marmara Bölgesi, Türkiye",
			"address": {
				"postcode": "34122",
				"suburb": "Cankurtaran Mahallesi",
				"city": "İstanbul",
				"town": "Fatih",
				"country": "Türkiye",
				"country_code": "tr"
			}
		}]
		""";

	[TestMethod]
	public async Task SearchLocationAsync_PostalCodePath_PrefersAddressLocalityForName()
	{
		var handler = new CountingHttpHandler(request =>
		{
			Assert.IsTrue(request.RequestUri!.Host.Contains("nominatim", StringComparison.OrdinalIgnoreCase));
			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(ValidNominatimIstanbulPostcodeJson),
			};
		});

		using var service = new GeocodingService(handler);
		var results = await service.SearchLocationAsync("34122", CancellationToken.None);

		Assert.AreEqual(1, results.Count);
		// Prior code surfaced the postcode itself ("34122") as Name, hiding
		// the actual locality. The address-derived name keeps results readable.
		Assert.AreEqual("İstanbul", results[0].Name);
		Assert.AreEqual("TR", results[0].CountryCode);
	}

	[TestMethod]
	public async Task SearchLocationAsync_PostalCodeEmpty_RetriesAsFreeTextNominatim()
	{
		var requestUris = new List<Uri>();
		var nominatimCalls = 0;
		var handler = new CountingHttpHandler(
			request =>
			{
				if (request.RequestUri!.Host.Contains("nominatim", StringComparison.OrdinalIgnoreCase))
				{
					nominatimCalls++;
					var query = request.RequestUri.Query;
					// First call uses postalcode=, second uses q=
					if (query.Contains("postalcode=", StringComparison.Ordinal))
					{
						return new HttpResponseMessage(HttpStatusCode.OK)
						{
							Content = new StringContent("[]"),
						};
					}

					return new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new StringContent(ValidNominatimIstanbulPostcodeJson),
					};
				}

				Assert.Fail("Photon should not be reached when free-text Nominatim retry succeeds.");
				return new HttpResponseMessage(HttpStatusCode.OK);
			},
			request => requestUris.Add(request.RequestUri!));

		using var service = new GeocodingService(handler);
		var results = await service.SearchLocationAsync("34122", CancellationToken.None);

		Assert.AreEqual(2, nominatimCalls, "Expected one postalcode= call followed by one free-text q= retry.");
		Assert.IsTrue(results.Count > 0, "Free-text Nominatim retry should surface postal code results.");
		Assert.IsTrue(requestUris[0].Query.Contains("postalcode=", StringComparison.Ordinal));
		Assert.IsTrue(requestUris[1].Query.Contains("q=", StringComparison.Ordinal));
	}

	// ── Photon broad retry when osm_tag=place returns nothing ──────────────

	private const string ValidPhotonGenericLocationJson = """
		{
			"type": "FeatureCollection",
			"features": [
				{
					"type": "Feature",
					"geometry": { "type": "Point", "coordinates": [28.9784, 41.0082] },
					"properties": {
						"name": "İstanbul",
						"country": "Türkiye",
						"osm_key": "boundary",
						"osm_value": "administrative"
					}
				}
			]
		}
		""";

	[TestMethod]
	public async Task SearchLocationAsync_PhotonPlaceFilterEmpty_RetriesWithoutFilter()
	{
		var photonQueries = new List<string>();
		var handler = new CountingHttpHandler(request =>
		{
			if (request.RequestUri!.Host.Contains("nominatim", StringComparison.OrdinalIgnoreCase))
			{
				return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
			}

			var query = Uri.UnescapeDataString(request.RequestUri.Query);
			photonQueries.Add(query);

			// First Photon call uses osm_tag=place and returns nothing.
			// The retry without that filter should succeed.
			if (query.Contains("osm_tag=place", StringComparison.Ordinal))
			{
				return new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent("""{ "type":"FeatureCollection", "features":[] }"""),
				};
			}

			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(ValidPhotonGenericLocationJson),
			};
		});

		using var service = new GeocodingService(handler);
		var results = await service.SearchLocationAsync("istanbul", CancellationToken.None);

		Assert.IsTrue(results.Count > 0, "Broad Photon retry should surface results when place-filter is empty.");
		Assert.AreEqual(2, photonQueries.Count, "Expected exactly two Photon calls: place-filtered then broad.");
		Assert.IsTrue(photonQueries[0].Contains("osm_tag=place", StringComparison.Ordinal));
		Assert.IsFalse(photonQueries[1].Contains("osm_tag=place", StringComparison.Ordinal));
	}

}

/// <summary>
/// An <see cref="HttpMessageHandler"/> that delegates each request to a factory function,
/// allowing tests to control and count outgoing HTTP calls.
/// </summary>
internal sealed class CountingHttpHandler(
	Func<HttpRequestMessage, HttpResponseMessage> responseFactory,
	Action<HttpRequestMessage>? onRequest = null) : HttpMessageHandler
{
	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		onRequest?.Invoke(request);
		return Task.FromResult(responseFactory(request));
	}
}
