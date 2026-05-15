// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BaldBeardedBuilder.WeatherExtension;
using Microsoft.CmdPal.Ext.Weather.Models;
using Microsoft.CommandPalette.Extensions;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Microsoft.CmdPal.Ext.Weather.Services;

public sealed partial class GeocodingService : IGeocodingService
{
	private readonly HttpClient _httpClient;
	private const string NominatimUrl = "https://nominatim.openstreetmap.org/search";
	private const string PhotonUrl = "https://photon.komoot.io/api/";
	private const int MinSearchLength = 3;
	internal const int MaxFallbackAttempts = 3;

	[GeneratedRegex(@"^\d{5}(-\d{4})?$")]
	private static partial Regex UsZipCodeRegex();

	[GeneratedRegex(@"^[A-Z]\d[A-Z]\s?\d[A-Z]\d$", RegexOptions.IgnoreCase)]
	private static partial Regex CanadaPostalCodeRegex();

	[GeneratedRegex(@"^(([Gg][Ii][Rr]\s?0[Aa]{2})|((([A-Za-z][0-9]{1,2})|(([A-Za-z][A-Ha-hJ-Yj-y][0-9]{1,2})|(([A-Za-z][0-9][A-Za-z])|([A-Za-z][A-Ha-hJ-Yj-y][0-9]?[A-Za-z]))))\s?[0-9][A-Za-z]{2}))$", RegexOptions.IgnoreCase)]
	private static partial Regex UkPostalCodeRegex();

	[GeneratedRegex(@"^\d{4,6}$")]
	private static partial Regex InternationalPostalCodeRegex();

	public GeocodingService()
		: this(new HttpClientHandler())
	{
	}

	internal GeocodingService(HttpMessageHandler handler)
	{
		_httpClient = new HttpClient(handler)
		{
			Timeout = TimeSpan.FromSeconds(10),
		};
		_httpClient.DefaultRequestHeaders.Add("User-Agent", "PowerToys-CmdPal-Weather/1.0");
	}

	public async Task<List<GeocodingResult>> SearchLocationAsync(string query, CancellationToken ct = default)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(query))
			{
				return [];
			}

			var trimmedQuery = query.Trim();

			if (trimmedQuery.Length < MinSearchLength)
			{
				return [];
			}

			// Check if input looks like a postal code
			if (IsPostalCode(trimmedQuery))
			{
				var results = await SearchPostalCodeAsync(trimmedQuery, ct).ConfigureAwait(false);
				if (results.Count > 0)
				{
					return results;
				}
			}

			// Progressive fallback: try the full query first, then strip
			// trailing comma-separated segments until results are found.
			// E.g. "Birmingham, Alabama, US" → "Birmingham, Alabama" → "Birmingham"
			var searchResults = await SearchWithProgressiveFallbackAsync(trimmedQuery, ct).ConfigureAwait(false);

			return RankResults(trimmedQuery, searchResults);
		}
		catch (Exception ex)
		{
			WeatherLogger.LogToHost(
				MessageState.Error,
				$"Geocoding error: {ex.Message}");
			return [];
		}
	}

	private async Task<List<GeocodingResult>> SearchWithProgressiveFallbackAsync(string query, CancellationToken ct)
	{
		var currentQuery = query;
		// MaxFallbackAttempts caps the total number of outbound HTTP calls across
		// both Nominatim and Photon combined, not per-provider. Each provider call
		// consumes one slot from this budget.
		var remainingCalls = MaxFallbackAttempts;
		var nominatimAlive = true;

		while (!string.IsNullOrWhiteSpace(currentQuery) && remainingCalls > 0)
		{
			ct.ThrowIfCancellationRequested();

			if (nominatimAlive)
			{
				List<GeocodingResult> results;
				try
				{
					results = await SearchNominatimAsync(currentQuery, ct).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					WeatherLogger.LogToHost(
						MessageState.Info,
						$"Nominatim search failed, switching to Photon: {ex.Message}");
					results = [];
					// Don't waste further budget on a provider that just threw — likely blocked.
					nominatimAlive = false;
				}

				remainingCalls--;

				if (results.Count > 0)
				{
					return results;
				}

				if (remainingCalls <= 0)
				{
					break;
				}
			}

			ct.ThrowIfCancellationRequested();

			var photonResults = await SearchPhotonAsync(currentQuery, placesOnly: true, ct).ConfigureAwait(false);
			remainingCalls--;

			if (photonResults.Count > 0)
			{
				return photonResults;
			}

			// Strip the last comma-separated segment and retry
			var lastComma = currentQuery.LastIndexOf(',');
			if (lastComma <= 0)
			{
				break;
			}

			currentQuery = currentQuery[..lastComma].Trim();
		}

		return [];
	}

	private static bool IsPostalCode(string input)
	{
		return UsZipCodeRegex().IsMatch(input) ||
			   CanadaPostalCodeRegex().IsMatch(input) ||
			   UkPostalCodeRegex().IsMatch(input) ||
			   InternationalPostalCodeRegex().IsMatch(input);
	}

	private async Task<List<GeocodingResult>> SearchPostalCodeAsync(string postalCode, CancellationToken ct)
	{
		try
		{
			var url = $"{NominatimUrl}?postalcode={Uri.EscapeDataString(postalCode)}&format=json&addressdetails=1&limit=10";
			var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);

			if (!response.IsSuccessStatusCode)
			{
				return await SearchPhotonAsync(postalCode, placesOnly: false, ct).ConfigureAwait(false);
			}

			var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
			var nominatimResults = JsonSerializer.Deserialize<List<NominatimResult>>(content, WeatherJsonContext.Default.ListNominatimResult);

			if (nominatimResults == null)
			{
				WeatherLogger.LogToHost(
					MessageState.Info,
					$"Nominatim deserialization returned null. Status: {response.StatusCode}, Content length: {content.Length}");
				return await SearchPhotonAsync(postalCode, placesOnly: false, ct).ConfigureAwait(false);
			}

			if (nominatimResults.Count == 0)
			{
				return await SearchPhotonAsync(postalCode, placesOnly: false, ct).ConfigureAwait(false);
			}

			return ConvertNominatimResults(nominatimResults);
		}
		catch (Exception ex)
		{
			WeatherLogger.LogToHost(
				MessageState.Error,
				$"Nominatim postal code lookup error: {ex.Message}");
			return await SearchPhotonAsync(postalCode, placesOnly: false, ct).ConfigureAwait(false);
		}
	}

	private async Task<List<GeocodingResult>> SearchNominatimAsync(string query, CancellationToken ct)
	{
		var url = $"{NominatimUrl}?q={Uri.EscapeDataString(query)}&format=json&addressdetails=1&limit=10";
		var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);

		if (!response.IsSuccessStatusCode)
		{
			WeatherLogger.LogToHost(
				MessageState.Info,
				$"Nominatim API returned status {response.StatusCode}");
			return [];
		}

		var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
		var nominatimResults = JsonSerializer.Deserialize<List<NominatimResult>>(content, WeatherJsonContext.Default.ListNominatimResult);

		if (nominatimResults == null)
		{
			WeatherLogger.LogToHost(
				MessageState.Info,
				$"Nominatim deserialization returned null. Status: {response.StatusCode}, Content length: {content.Length}");
			return [];
		}

		return ConvertNominatimResults(nominatimResults);
	}

	private async Task<List<GeocodingResult>> SearchPhotonAsync(string query, bool placesOnly, CancellationToken ct)
	{
		try
		{
			var url = $"{PhotonUrl}?q={Uri.EscapeDataString(query)}&limit=10";
			if (placesOnly)
			{
				url += "&osm_tag=place";
			}

			var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);

			if (!response.IsSuccessStatusCode)
			{
				WeatherLogger.LogToHost(
					MessageState.Info,
					$"Photon API returned status {response.StatusCode}");
				return [];
			}

			var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
			var result = JsonSerializer.Deserialize<PhotonResult>(content, WeatherJsonContext.Default.PhotonResult);

			if (result == null)
			{
				WeatherLogger.LogToHost(
					MessageState.Info,
					$"Photon deserialization returned null. Content length: {content.Length}");
				return [];
			}

			return ConvertPhotonResults(result.Features ?? []);
		}
		catch (Exception ex)
		{
			WeatherLogger.LogToHost(
				MessageState.Error,
				$"Photon geocoding error: {ex.Message}");
			return [];
		}
	}

	internal static List<GeocodingResult> ConvertPhotonResults(List<PhotonFeature> features)
	{
		var results = new List<GeocodingResult>();

		foreach (var feature in features)
		{
			var coords = feature.Geometry?.Coordinates;
			if (coords == null || coords.Length < 2)
			{
				continue;
			}

			var name = feature.Properties?.Name
				?? feature.Properties?.City;

			if (string.IsNullOrWhiteSpace(name))
			{
				// Skip entries that don't have a usable place name to avoid blank items and unstable ranking.
				continue;
			}

			// GeoJSON uses [longitude, latitude] order — swapped from what most people expect.
			var longitude = coords[0];
			var latitude = coords[1];

			results.Add(new GeocodingResult
			{
				Id = feature.Properties?.OsmId ?? 0,
				Latitude = latitude,
				Longitude = longitude,
				Name = name,
				Admin1 = feature.Properties?.State,
				Admin2 = feature.Properties?.County,
				Country = feature.Properties?.Country,
				CountryCode = feature.Properties?.CountryCode?.ToUpperInvariant(),
			});
		}

		return results;
	}

	internal static List<GeocodingResult> ConvertNominatimResults(List<NominatimResult> nominatimResults)
	{
		var results = new List<GeocodingResult>();

		foreach (var nr in nominatimResults)
		{
			var rawPlaceName = nr.Name
				?? nr.Address?.City
				?? nr.Address?.Town
				?? nr.Address?.Village
				?? nr.DisplayName?.Split(',')[0].Trim();

			if (string.IsNullOrWhiteSpace(rawPlaceName))
			{
				// Skip entries that don't have a usable place name to avoid blank items and unstable ranking.
				continue;
			}

			var placeName = rawPlaceName;
			results.Add(new GeocodingResult
			{
				Id = nr.PlaceId,
				Latitude = nr.Lat,
				Longitude = nr.Lon,
				Name = placeName,
				Admin1 = nr.Address?.State,
				Country = nr.Address?.Country ?? ExtractCountryFromDisplayName(nr.DisplayName),
				CountryCode = nr.Address?.CountryCode?.ToUpperInvariant(),
			});
		}

		return results;
	}

	internal static List<GeocodingResult> RankResults(string query, List<GeocodingResult> results)
	{
		if (string.IsNullOrWhiteSpace(query) || results.Count <= 1)
		{
			return results;
		}

		// Use just the city part for matching (everything before the first comma)
		var cityPart = query.Contains(',') ? query.Split(',')[0].Trim() : query.Trim();

		return [.. results.OrderByDescending(r => ScoreResult(cityPart, r))];
	}

	private static int ScoreResult(string cityQuery, GeocodingResult result)
	{
		var name = result.Name ?? string.Empty;

		if (string.Equals(name, cityQuery, StringComparison.OrdinalIgnoreCase))
		{
			return 3;
		}

		if (name.StartsWith(cityQuery, StringComparison.OrdinalIgnoreCase))
		{
			return 2;
		}

		if (name.Contains(cityQuery, StringComparison.OrdinalIgnoreCase))
		{
			return 1;
		}

		return 0;
	}

	private static string ExtractCountryFromDisplayName(string? displayName)
	{
		if (string.IsNullOrWhiteSpace(displayName))
		{
			return string.Empty;
		}

		var parts = displayName.Split(',');
		return parts.Length > 0 ? parts[^1].Trim() : string.Empty;
	}

	public void Dispose()
	{
		_httpClient?.Dispose();
	}
}
