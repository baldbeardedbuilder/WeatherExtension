// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using Microsoft.CmdPal.Ext.Weather.Services;

namespace Microsoft.CmdPal.Ext.Weather.UnitTests;

[TestClass]
public class OpenMeteoServiceCultureTests
{
	private const string MinimalCurrentWeatherJson = """
		{
			"latitude": 41.0,
			"longitude": 28.97,
			"current": {
				"time": "2026-05-20T12:00",
				"temperature_2m": 22.0,
				"relative_humidity_2m": 60,
				"apparent_temperature": 22.0,
				"weather_code": 0,
				"wind_speed_10m": 5.0,
				"wind_direction_10m": 180
			}
		}
		""";

	[TestMethod]
	public async Task GetCurrentWeatherAsync_UnderTurkishCulture_FormatsCoordinatesWithDots()
	{
		var originalCulture = Thread.CurrentThread.CurrentCulture;
		Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
		try
		{
			Uri? capturedUri = null;
			var handler = new RecordingHandler(request =>
			{
				capturedUri = request.RequestUri;
				return new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent(MinimalCurrentWeatherJson),
				};
			});

			using var service = new OpenMeteoService(handler);
			var result = await service.GetCurrentWeatherAsync(41.0063810, 28.9758715, "celsius", "kmh", CancellationToken.None);

			Assert.IsNotNull(capturedUri, "Expected the service to make an HTTP request.");
			AssertCoordinatesAreInvariant(capturedUri!, expectedLat: "41.006381", expectedLon: "28.9758715");
			Assert.IsNotNull(result, "Successful 200 response should deserialize.");
		}
		finally
		{
			Thread.CurrentThread.CurrentCulture = originalCulture;
		}
	}

	[TestMethod]
	public async Task GetForecastAsync_UnderTurkishCulture_FormatsCoordinatesWithDots()
	{
		var originalCulture = Thread.CurrentThread.CurrentCulture;
		Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
		try
		{
			Uri? capturedUri = null;
			var handler = new RecordingHandler(request =>
			{
				capturedUri = request.RequestUri;
				return new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent("""{ "latitude":41.0, "longitude":28.97, "daily":{} }"""),
				};
			});

			using var service = new OpenMeteoService(handler);
			await service.GetForecastAsync(41.0063810, 28.9758715, "celsius", CancellationToken.None);

			Assert.IsNotNull(capturedUri);
			AssertCoordinatesAreInvariant(capturedUri!, expectedLat: "41.006381", expectedLon: "28.9758715");
		}
		finally
		{
			Thread.CurrentThread.CurrentCulture = originalCulture;
		}
	}

	[TestMethod]
	public async Task GetHourlyForecastAsync_UnderTurkishCulture_FormatsCoordinatesWithDots()
	{
		var originalCulture = Thread.CurrentThread.CurrentCulture;
		Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
		try
		{
			Uri? capturedUri = null;
			var handler = new RecordingHandler(request =>
			{
				capturedUri = request.RequestUri;
				return new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent("""{ "latitude":41.0, "longitude":28.97, "hourly":{} }"""),
				};
			});

			using var service = new OpenMeteoService(handler);
			await service.GetHourlyForecastAsync(41.0063810, 28.9758715, "celsius", "kmh", CancellationToken.None);

			Assert.IsNotNull(capturedUri);
			AssertCoordinatesAreInvariant(capturedUri!, expectedLat: "41.006381", expectedLon: "28.9758715");
		}
		finally
		{
			Thread.CurrentThread.CurrentCulture = originalCulture;
		}
	}

	private static void AssertCoordinatesAreInvariant(Uri uri, string expectedLat, string expectedLon)
	{
		// Open-Meteo rejects comma-formatted floats with HTTP 400. Coordinates
		// must always be invariant-culture decimals regardless of OS locale.
		// We can't assert against the entire query string because parameter list
		// values legitimately contain commas (e.g. current=temperature_2m,...).
		var parsed = System.Web.HttpUtility.ParseQueryString(uri.Query);

		var lat = parsed["latitude"];
		var lon = parsed["longitude"];

		Assert.AreEqual(expectedLat, lat, $"latitude must be dot-formatted regardless of culture. Query='{uri.Query}'");
		Assert.AreEqual(expectedLon, lon, $"longitude must be dot-formatted regardless of culture. Query='{uri.Query}'");
		Assert.IsFalse(lat!.Contains(',', StringComparison.Ordinal), "latitude must not contain a culture-specific comma.");
		Assert.IsFalse(lon!.Contains(',', StringComparison.Ordinal), "longitude must not contain a culture-specific comma.");
	}

	private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> factory) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return Task.FromResult(factory(request));
		}
	}
}
