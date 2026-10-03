// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text.Json;
using Microsoft.CmdPal.Ext.Weather.Models;
using Microsoft.CmdPal.Ext.Weather.Pages;
using Microsoft.CmdPal.Ext.Weather.Services;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Microsoft.CmdPal.Ext.Weather.UnitTests;

[TestClass]
public sealed class WeatherCardPresentationTests
{
	[TestMethod]
	[DataRow(5, 59, (int)DaylightPhase.BeforeSunrise, 0.0)]
	[DataRow(6, 0, (int)DaylightPhase.Daytime, 0.0)]
	[DataRow(12, 0, (int)DaylightPhase.Daytime, 0.5)]
	[DataRow(17, 59, (int)DaylightPhase.Daytime, 719.0 / 720)]
	[DataRow(18, 0, (int)DaylightPhase.AfterSunset, 1.0)]
	[DataRow(23, 0, (int)DaylightPhase.AfterSunset, 1.0)]
	public void Build_DaylightBoundaries_ClampProgressAndLabelPhase(int hour, int minute, int phaseValue, double progress)
	{
		var data = Build(new DateTimeOffset(2026, 10, 2, hour, minute, 0, TimeSpan.Zero));
		var phase = (DaylightPhase)phaseValue;

		Assert.AreEqual(phase, data.Daylight.Phase);
		Assert.AreEqual(progress, data.Daylight.Progress, 0.000001);
		Assert.AreEqual("06:00", data.Daylight.Sunrise);
		Assert.AreEqual("18:00", data.Daylight.Sunset);
		Assert.AreEqual("12 hr 0 min", data.Daylight.Duration);
		Assert.AreEqual(phase switch
		{
			DaylightPhase.BeforeSunrise => Resources.card_before_sunrise,
			DaylightPhase.AfterSunset => Resources.card_after_sunset,
			_ => Resources.card_daytime,
		}, data.Daylight.Status);
	}

	[TestMethod]
	public void Build_LocationAcrossDateBoundary_SelectsLocationDayAndFutureHours()
	{
		var forecast = Forecast();
		var weather = Weather("Asia/Tokyo");
		var hourly = Hourly("2026-10-03");
		var now = new DateTimeOffset(2026, 10, 2, 23, 0, 0, TimeSpan.Zero);
		var data = Build(now, weather, forecast, hourly);

		Assert.AreEqual("08:00", data.Daylight.LocalTime);
		Assert.AreEqual("26°C / 16°C", data.TodayHighLow);
		Assert.AreEqual(1.0 / 6, data.Daylight.Progress, 0.000001);
		Assert.AreEqual("13:00", data.Hours[0].Time);
		Assert.AreEqual("Sun", data.Days[0].Time);
		Assert.AreEqual(2, data.Days.Count);
	}

	[TestMethod]
	[DataRow("2026-03-08", 16)]
	[DataRow("2026-11-01", 17)]
	public void Build_DaylightSavingDates_UsesTimezoneOffset(string date, int utcHour)
	{
		var localDate = DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
		var forecast = Forecast(date);
		var now = new DateTimeOffset(localDate.Year, localDate.Month, localDate.Day, utcHour, 0, 0, TimeSpan.Zero);
		var data = Build(now, Weather("America/New_York"), forecast, Hourly(date));

		Assert.AreEqual("12:00", data.Daylight.LocalTime);
		Assert.AreEqual(0.5, data.Daylight.Progress, 0.000001);
		Assert.AreEqual("12 hr 0 min", data.Daylight.Duration);
	}

	[TestMethod]
	[DataRow(null)]
	[DataRow("not-a-timezone")]
	public void Build_UnavailableTimezone_KeepsCurrentReadingsWithoutGuessingForecastTimes(string? timezone)
	{
		var data = Build(Noon, Weather(timezone));

		Assert.AreEqual("24°C", data.CurrentTemp);
		Assert.AreEqual(DaylightPhase.Unavailable, data.Daylight.Phase);
		Assert.AreEqual("--", data.Daylight.LocalTime);
		Assert.AreEqual(0, data.Hours.Count);
		Assert.AreEqual(0, data.Days.Count);
		Assert.AreEqual(1, data.Diagnostics.Count);
		StringAssert.Contains(data.Diagnostics[0], "timezone");
	}

	[TestMethod]
	[DataRow(null)]
	[DataRow("")]
	[DataRow(" ")]
	public void Build_MissingResponseTimezone_UsesLocationTimezone(string? timezone)
	{
		var location = new GeocodingResult { Name = "Tokyo", Timezone = "Asia/Tokyo" };
		var data = WeatherCardPresentation.Build(location, Weather(timezone), null, null,
			"celsius", "kmh", true, Noon);

		Assert.AreEqual("21:00", data.Daylight.LocalTime);
		Assert.AreEqual(DaylightPhase.Unavailable, data.Daylight.Phase);
	}

	[TestMethod]
	public void Build_BlankCurrentTimezone_UsesForecastTimezone()
	{
		var forecast = Forecast();
		forecast.Timezone = "Asia/Tokyo";
		var data = Build(Noon, Weather(" "), forecast);

		Assert.AreEqual("21:00", data.Daylight.LocalTime);
		Assert.AreEqual(DaylightPhase.AfterSunset, data.Daylight.Phase);
		Assert.AreEqual(0, data.Diagnostics.Count);
	}

	[TestMethod]
	[DataRow(null, "2026-10-02T18:00")]
	[DataRow("bad-date", "2026-10-02T18:00")]
	[DataRow("2026-10-02T18:00", "2026-10-02T06:00")]
	[DataRow("2026-10-02T06:00", "2026-10-02T06:00")]
	[DataRow("2026-10-01T06:00", "2026-10-01T18:00")]
	public void Build_InvalidDaylight_KeepsForecastAndExplicitUnavailableStatus(string? sunrise, string sunset)
	{
		var forecast = Forecast();
		forecast.Daily!.Sunrise![0] = sunrise!;
		forecast.Daily.Sunset![0] = sunset;
		var data = Build(Noon, forecast: forecast);

		Assert.AreEqual(DaylightPhase.Unavailable, data.Daylight.Phase);
		Assert.AreEqual(Resources.card_daylight_unavailable, data.Daylight.Status);
		Assert.AreEqual("--", data.Daylight.Sunrise);
		Assert.AreEqual("12:00", data.Daylight.LocalTime);
		Assert.AreEqual(3, data.Hours.Count);
		Assert.AreEqual(3, data.Days.Count);
		Assert.AreEqual(1, data.Diagnostics.Count);
	}

	[TestMethod]
	public void Build_NonexistentDstTime_MarksDaylightUnavailable()
	{
		var forecast = Forecast("2026-03-08");
		forecast.Daily!.Sunrise![0] = "2026-03-08T02:30";
		var data = Build(new DateTimeOffset(2026, 3, 8, 16, 0, 0, TimeSpan.Zero),
			Weather("America/New_York"), forecast, Hourly("2026-03-08"));

		Assert.AreEqual(DaylightPhase.Unavailable, data.Daylight.Phase);
		Assert.AreEqual(Resources.card_daylight_unavailable, data.Daylight.Status);
	}

	[TestMethod]
	public void Build_AmbiguousDstTime_MarksDaylightUnavailable()
	{
		var forecast = Forecast("2026-11-01");
		forecast.Daily!.Sunrise![0] = "2026-11-01T01:30";
		var data = Build(new DateTimeOffset(2026, 11, 1, 17, 0, 0, TimeSpan.Zero),
			Weather("America/New_York"), forecast, Hourly("2026-11-01"));

		Assert.AreEqual(DaylightPhase.Unavailable, data.Daylight.Phase);
		Assert.AreEqual(Resources.card_daylight_unavailable, data.Daylight.Status);
	}

	[TestMethod]
	public void Build_DaylightAcrossDstChange_UsesElapsedTimeForDurationAndProgress()
	{
		var forecast = Forecast("2026-03-08");
		forecast.Daily!.Sunrise![0] = "2026-03-08T00:30";
		forecast.Daily.Sunset![0] = "2026-03-08T18:30";
		var data = Build(new DateTimeOffset(2026, 3, 8, 14, 0, 0, TimeSpan.Zero),
			Weather("America/New_York"), forecast, Hourly("2026-03-08"));

		Assert.AreEqual("10:00", data.Daylight.LocalTime);
		Assert.AreEqual("17 hr 0 min", data.Daylight.Duration);
		Assert.AreEqual(0.5, data.Daylight.Progress, 0.000001);
	}

	[TestMethod]
	public void Build_MissingForecasts_LeavesCurrentConditionsAndLocalTimeAvailable()
	{
		var data = WeatherCardPresentation.Build(Location(), Weather(), null, null,
			"celsius", "kmh", true, Noon);

		Assert.AreEqual("24°C", data.CurrentTemp);
		Assert.AreEqual(string.Empty, data.Status);
		Assert.AreEqual("12:00", data.Daylight.LocalTime);
		Assert.AreEqual(Resources.card_daylight_unavailable, data.Daylight.Status);
		Assert.AreEqual(0, data.Hours.Count);
		Assert.AreEqual(0, data.Days.Count);
	}

	[TestMethod]
	public void Build_InvalidForecastReadings_UsesUnavailableValues()
	{
		var hourly = Hourly();
		hourly.Hourly!.Temperature = [double.NaN, double.PositiveInfinity, double.NegativeInfinity];
		hourly.Hourly.PrecipitationProbability = [-1, 101];
		var data = Build(Noon, hourly: hourly);

		Assert.AreEqual(3, data.Hours.Count);
		foreach (var hour in data.Hours)
		{
			Assert.AreEqual("--", hour.Temperature);
			Assert.AreEqual("--", hour.Rain);
		}
	}

	[TestMethod]
	public void Build_ShortForecastArrays_DoesNotInventZeroReadings()
	{
		var forecast = Forecast();
		forecast.Daily!.TemperatureMax = [25];
		forecast.Daily.TemperatureMin = null;
		forecast.Daily.WeatherCode = [];
		forecast.Daily.PrecipitationProbabilityMax = [10];
		var hourly = Hourly();
		hourly.Hourly!.Temperature = [];
		hourly.Hourly.WeatherCode = null;
		hourly.Hourly.PrecipitationProbability = [];
		var data = Build(Noon, forecast: forecast, hourly: hourly);

		Assert.AreEqual("25°C / --", data.TodayHighLow);
		Assert.AreEqual(3, data.Hours.Count);
		Assert.AreEqual(3, data.Days.Count);
		foreach (var hour in data.Hours)
		{
			Assert.AreEqual("--", hour.Temperature);
			Assert.AreEqual("--", hour.Rain);
			Assert.IsNull(hour.WeatherCode);
			Assert.AreEqual(Resources.card_condition_unknown, hour.Condition);
		}

		foreach (var day in data.Days)
		{
			Assert.AreEqual("-- / --", day.Temperature);
			Assert.AreEqual("--", day.Rain);
			Assert.IsNull(day.WeatherCode);
		}
	}

	[TestMethod]
	public void Build_ClockAndUnitPreferences_FormatsTimesAndReadings()
	{
		var data = WeatherCardPresentation.Build(Location(), Weather(), Forecast(), Hourly(),
			"fahrenheit", "mph", false, Noon);

		Assert.AreEqual("24°F", data.CurrentTemp);
		Assert.AreEqual("26°F", data.FeelsLike);
		Assert.AreEqual("6.1 mph", data.Wind);
		Assert.AreEqual("6:00 AM", data.Daylight.Sunrise);
		Assert.AreEqual("12:00 PM", data.Daylight.LocalTime);
		Assert.AreEqual("1 PM", data.Hours[0].Time);
		Assert.AreEqual("10% rain", data.Hours[0].Rain);
		Assert.AreEqual("50% rain", data.Days[0].Rain);
	}

	[TestMethod]
	public void Build_ExactCurrentHour_SkipsItAndMarksNightForecast()
	{
		var data = Build(new DateTimeOffset(2026, 10, 2, 13, 0, 0, TimeSpan.Zero));

		Assert.AreEqual(3, data.Hours.Count);
		Assert.AreEqual("14:00", data.Hours[0].Time);
		Assert.AreEqual("19:00", data.Hours[2].Time);
		Assert.IsTrue(data.Hours[2].IsNight);
		Assert.IsFalse(data.Hours[0].IsNight);
	}

	[TestMethod]
	public void Build_MissingCurrentWeather_PreservesValidDaylightAndForecasts()
	{
		var data = WeatherCardPresentation.Build(Location(), null, Forecast(), Hourly(),
			"celsius", "kmh", true, Noon);

		Assert.AreEqual(Resources.weather_service_error, data.Status);
		Assert.AreEqual("--", data.CurrentTemp);
		Assert.AreEqual(DaylightPhase.Daytime, data.Daylight.Phase);
		Assert.AreEqual(3, data.Hours.Count);
		Assert.AreEqual(3, data.Days.Count);
	}

	[TestMethod]
	public void Serialize_QuotedLocationAndUnavailableState_PreservesTextAndOmitsCalculationFields()
	{
		const string location = "A \"quoted\" city\\district\n${literal}";
		var data = WeatherCardPresentation.Empty(location, Resources.loading_data);
		using var json = JsonDocument.Parse(WeatherCardPresentation.Serialize(data));
		var root = json.RootElement;

		Assert.AreEqual(location, root.GetProperty("locationName").GetString());
		Assert.AreEqual(Resources.loading_data, root.GetProperty("status").GetString());
		Assert.AreEqual(Resources.card_daylight_unavailable, root.GetProperty("daylight").GetProperty("status").GetString());
		Assert.AreEqual(0, root.GetProperty("hours").GetArrayLength());
		Assert.AreEqual(0, root.GetProperty("days").GetArrayLength());
		Assert.IsFalse(root.TryGetProperty("diagnostics", out _));
		Assert.IsFalse(root.GetProperty("daylight").TryGetProperty("progress", out _));
		Assert.IsFalse(root.GetProperty("daylight").TryGetProperty("phase", out _));
	}

	[TestMethod]
	public void Template_UsesAdaptiveCard15AndNativeImagesWithAccessibleText()
	{
		using var template = JsonDocument.Parse(WeatherCardTemplate.Json);
		Assert.AreEqual("AdaptiveCard", template.RootElement.GetProperty("type").GetString());
		Assert.AreEqual("1.5", template.RootElement.GetProperty("version").GetString());
		var images = FindElements(template.RootElement, "Image").ToList();
		Assert.AreEqual(4, images.Count);
		foreach (var image in images)
		{
			Assert.IsTrue(image.TryGetProperty("altText", out var alt));
			Assert.IsFalse(string.IsNullOrWhiteSpace(alt.GetString()));
			Assert.IsTrue(image.TryGetProperty("$when", out _), "Unavailable images must not be rendered.");
		}

		StringAssert.Contains(WeatherCardTemplate.Json, "${daylight.status}");
		StringAssert.Contains(WeatherCardTemplate.Json, "${rain}");
		StringAssert.Contains(WeatherCardTemplate.Json, "${labels.forecastUnavailable}");
	}

	[TestMethod]
	public async Task PopulateImages_DaylightAndNight_ProducesDecodableLocalPngs()
	{
		var day = Build(Noon);
		await WeatherCardImages.PopulateAsync(day);
		var night = Build(new DateTimeOffset(2026, 10, 2, 23, 0, 0, TimeSpan.Zero));
		await WeatherCardImages.PopulateAsync(night);
		var dayPixels = await Decode(day.Daylight.Image);
		var nightPixels = await Decode(night.Daylight.Image);

		Assert.AreEqual(1440u, dayPixels.Width);
		Assert.AreEqual(192u, dayPixels.Height);
		Assert.AreEqual(dayPixels.Bytes.Length, nightPixels.Bytes.Length);
		Assert.IsFalse(dayPixels.Bytes.SequenceEqual(nightPixels.Bytes));
		var condition = await Decode(day.CurrentImage);
		Assert.AreEqual(192u, condition.Width);
		Assert.AreEqual(192u, condition.Height);
		Assert.IsTrue(condition.Bytes.Where((_, index) => index % 4 == 3).Any(alpha => alpha > 0));
		foreach (var hour in day.Hours)
		{
			StringAssert.StartsWith(hour.Image, "data:image/png;base64,");
		}
	}

	[TestMethod]
	public async Task ConditionImages_ClearDayAndNight_UseDistinctArtworkAndReuseCachedImage()
	{
		var day = await WeatherCardImages.ConditionAsync(0, false);
		var night = await WeatherCardImages.ConditionAsync(0, true);
		Assert.AreNotEqual(day, night);
		Assert.AreEqual(day, await WeatherCardImages.ConditionAsync(0, false));
		await Decode(night);
	}

	[TestMethod]
	[DataRow(0)]
	[DataRow(2)]
	[DataRow(3)]
	[DataRow(45)]
	[DataRow(51)]
	[DataRow(61)]
	[DataRow(71)]
	[DataRow(95)]
	[DataRow(-1)]
	public async Task ConditionImages_WeatherFamilies_ProduceTransparentDecodableArtwork(int weatherCode)
	{
		var image = await Decode(await WeatherCardImages.ConditionAsync(weatherCode, false));

		Assert.AreEqual(192u, image.Width);
		Assert.AreEqual(192u, image.Height);
		var alpha = image.Bytes.Where((_, index) => index % 4 == 3).ToArray();
		Assert.IsTrue(alpha.Any(value => value > 0));
		Assert.IsTrue(alpha.Any(value => value == 0));
	}

	[TestMethod]
	public async Task DaylightImage_AfterSunset_RemovesTheDaytimeMarker()
	{
		var sunrise = await Decode(await WeatherCardImages.DaylightAsync(0, DaylightPhase.Daytime));
		var before = await Decode(await WeatherCardImages.DaylightAsync(0, DaylightPhase.BeforeSunrise));
		var sunset = await Decode(await WeatherCardImages.DaylightAsync(1, DaylightPhase.Daytime));
		var after = await Decode(await WeatherCardImages.DaylightAsync(1, DaylightPhase.AfterSunset));

		Assert.IsFalse(sunrise.Bytes.SequenceEqual(before.Bytes));
		Assert.IsFalse(sunset.Bytes.SequenceEqual(after.Bytes));
	}

	[TestMethod]
	public async Task PopulateImages_UnavailableDaylight_DoesNotInventAnArc()
	{
		var data = WeatherCardPresentation.Empty("Birmingham", Resources.unavailable);
		await WeatherCardImages.PopulateAsync(data);
		Assert.AreEqual(string.Empty, data.Daylight.Image);
		Assert.AreEqual(string.Empty, data.CurrentImage);
	}

	[TestMethod]
	public void AllShippedLocales_HaveDaylightLabelsAndValidFormatPlaceholders()
	{
		var original = CultureInfo.CurrentUICulture;
		try
		{
			foreach (var locale in new[] { "en", "ar", "de", "es", "fr", "it", "ja", "ko", "nl", "pl", "pt-BR", "ru", "tr", "zh-Hans", "zh-Hant" })
			{
				CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(locale);
				var labels = new WeatherCardLabels();
				Assert.IsFalse(string.IsNullOrWhiteSpace(labels.Daylight), locale);
				Assert.IsFalse(string.IsNullOrWhiteSpace(labels.Sunrise), locale);
				Assert.IsFalse(string.IsNullOrWhiteSpace(labels.Sunset), locale);
				Assert.IsFalse(string.IsNullOrWhiteSpace(labels.LocalTime), locale);
				var data = Build(Noon);
				Assert.AreEqual(DaylightPhase.Daytime, data.Daylight.Phase);
				Assert.IsFalse(string.IsNullOrWhiteSpace(data.Daylight.AltText), locale);
				Assert.IsFalse(data.Daylight.AltText.Contains("{0}", StringComparison.Ordinal), locale);
			}
		}
		finally
		{
			CultureInfo.CurrentUICulture = original;
		}
	}

	private static DateTimeOffset Noon => new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);

	private static GeocodingResult Location() => new() { Name = "Birmingham", Timezone = "UTC" };

	private static WeatherData Weather(string? timezone = "UTC") => new()
	{
		Timezone = timezone,
		Current = new() { Temperature = 24, ApparentTemperature = 26, RelativeHumidity = 58, WindSpeed = 6.1, WeatherCode = 2 },
	};

	private static WeatherCardData Build(DateTimeOffset now, WeatherData? weather = null, ForecastData? forecast = null, HourlyForecastData? hourly = null)
	{
		var culture = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
			var location = Location();
			if (weather != null)
			{
				location.Timezone = weather.Timezone;
			}

			return WeatherCardPresentation.Build(location, weather ?? Weather(), forecast ?? Forecast(),
				hourly ?? Hourly(), "celsius", "kmh", true, now);
		}
		finally
		{
			CultureInfo.CurrentCulture = culture;
		}
	}

	private static ForecastData Forecast(string start = "2026-10-02")
	{
		var date = DateTime.ParseExact(start, "yyyy-MM-dd", CultureInfo.InvariantCulture);
		var dates = Enumerable.Range(0, 4).Select(i => date.AddDays(i).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).ToList();
		return new()
		{
			Daily = new()
			{
				Time = dates,
				Sunrise = dates.Select(d => d + "T06:00").ToList(),
				Sunset = dates.Select(d => d + "T18:00").ToList(),
				TemperatureMax = [25, 26, 27, 28],
				TemperatureMin = [15, 16, 17, 18],
				WeatherCode = [2, 61, 2, 0],
				PrecipitationProbabilityMax = [10, 50, 10, 0],
			},
		};
	}

	private static HourlyForecastData Hourly(string date = "2026-10-02") => new()
	{
		Hourly = new()
		{
			Time = [date + "T13:00", date + "T14:00", date + "T15:00", date + "T19:00"],
			Temperature = [25, 26, 27, 22],
			WeatherCode = [2, 3, 61, 0],
			PrecipitationProbability = [10, 30, 70, 0],
		},
	};

	private static IEnumerable<JsonElement> FindElements(JsonElement element, string type)
	{
		if (element.ValueKind == JsonValueKind.Object)
		{
			if (element.TryGetProperty("type", out var value) && value.GetString() == type)
			{
				yield return element;
			}

			foreach (var property in element.EnumerateObject())
			{
				foreach (var child in FindElements(property.Value, type))
				{
					yield return child;
				}
			}
		}
		else if (element.ValueKind == JsonValueKind.Array)
		{
			foreach (var item in element.EnumerateArray())
			{
				foreach (var child in FindElements(item, type))
				{
					yield return child;
				}
			}
		}
	}

	private static async Task<(uint Width, uint Height, byte[] Bytes)> Decode(string uri)
	{
		StringAssert.StartsWith(uri, "data:image/png;base64,");
		var bytes = Convert.FromBase64String(uri["data:image/png;base64,".Length..]);
		using var stream = new InMemoryRandomAccessStream();
		using (var writer = new DataWriter(stream.GetOutputStreamAt(0)))
		{
			writer.WriteBytes(bytes);
			await writer.StoreAsync();
		}

		stream.Seek(0);
		var decoder = await BitmapDecoder.CreateAsync(stream);
		var pixels = await decoder.GetPixelDataAsync();
		return (decoder.PixelWidth, decoder.PixelHeight, pixels.DetachPixelData());
	}
}
