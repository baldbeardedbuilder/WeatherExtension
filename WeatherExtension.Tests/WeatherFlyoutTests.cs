// Copyright (c) Bald Bearded Builder LLC
// Licensed under the MIT license. See LICENSE in the project root.

using System.Buffers.Binary;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using BaldBeardedBuilder.WeatherExtension;
using Microsoft.CmdPal.Ext.Weather.DockBands;
using Microsoft.CmdPal.Ext.Weather.Models;
using Microsoft.CmdPal.Ext.Weather.Pages;
using Microsoft.CmdPal.Ext.Weather.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Moq;

namespace Microsoft.CmdPal.Ext.Weather.UnitTests;

[TestClass]
public class WeatherFlyoutTests
{
    private static readonly DateTimeOffset Instant = new(2026, 10, 15, 16, 0, 0, TimeSpan.Zero);
    private string _settingsPath = string.Empty;

    [TestInitialize]
    public void Initialize() => _settingsPath = Path.Combine(Path.GetTempPath(), $"weather-flyout-{Guid.NewGuid():N}.json");

    [TestCleanup]
    public void Cleanup()
    {
        if (File.Exists(_settingsPath))
        {
            File.Delete(_settingsPath);
        }
    }

    [TestMethod]
    public void ExistingSettingsKeepClassicUntilExplicitSelection()
    {
        File.WriteAllText(_settingsPath, """{"weather.TemperatureUnit":"fahrenheit","unrelated.setting":42}""");
        var settings = new WeatherSettingsManager(_settingsPath);
        Assert.AreEqual(WeatherFlyoutView.Classic, settings.FlyoutView);
        Assert.AreEqual("fahrenheit", settings.TemperatureUnit);
        var before = File.ReadAllText(_settingsPath);
        var gallery = new WeatherFlyoutGalleryPage(settings);
        foreach (var tile in gallery.GetItems())
        {
            Assert.IsInstanceOfType<WeatherFlyoutPreviewPage>(tile.Command);
            ((WeatherFlyoutPreviewPage)tile.Command).GetContent();
        }

        Assert.AreEqual(before, File.ReadAllText(_settingsPath), "Browsing must not save a default.");
        var form = new WeatherFlyoutPreviewPage.PreviewForm(settings, WeatherFlyoutView.Glance);
        form.SubmitForm("{}", """{"action":"cancel"}""");
        Assert.AreEqual(WeatherFlyoutView.Classic, settings.FlyoutView);
        Assert.AreEqual(before, File.ReadAllText(_settingsPath));
    }

    [TestMethod]
    public void ApplyingViewPersistsAndPreservesOtherSettings()
    {
        File.WriteAllText(_settingsPath, """{"weather.TemperatureUnit":"fahrenheit","unrelated.setting":42}""");
        var settings = new WeatherSettingsManager(_settingsPath);
        var notifications = 0;
        var weatherSettingsChanges = 0;
        settings.FlyoutViewChanged += (_, _) => notifications++;
        settings.Settings.SettingsChanged += (_, _) => weatherSettingsChanges++;
        var form = new WeatherFlyoutPreviewPage.PreviewForm(settings, WeatherFlyoutView.DaylightArc);
        form.SubmitForm("{}", """{"action":"apply"}""");

        var reloaded = new WeatherSettingsManager(_settingsPath);
        Assert.AreEqual(WeatherFlyoutView.DaylightArc, settings.FlyoutView);
        Assert.AreEqual(settings.FlyoutView, reloaded.FlyoutView);
        Assert.AreEqual("fahrenheit", reloaded.TemperatureUnit);
        Assert.AreEqual(42, JsonNode.Parse(File.ReadAllText(_settingsPath))!["unrelated.setting"]!.GetValue<int>());
        Assert.AreEqual(1, notifications);
        Assert.AreEqual(0, weatherSettingsChanges, "Presentation changes must not restart weather refreshes.");
        settings.SetFlyoutView(WeatherFlyoutView.DaylightArc);
        Assert.AreEqual(1, notifications, "Reapplying the same view must not rebuild bands.");
    }

    [TestMethod]
    public void FailedSaveDoesNotChangePreferenceOrNotify()
    {
        var settings = new WeatherSettingsManager(_settingsPath);
        settings.SetFlyoutView(WeatherFlyoutView.NextHours);
        var notifications = 0;
        settings.FlyoutViewChanged += (_, _) => notifications++;
        using (var locked = new FileStream(_settingsPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Assert.ThrowsException<IOException>(() => settings.SetFlyoutView(WeatherFlyoutView.Glance));
            new WeatherFlyoutPreviewPage.PreviewForm(settings, WeatherFlyoutView.Glance)
                .SubmitForm("{}", """{"action":"apply"}""");
            Assert.AreEqual(WeatherFlyoutView.NextHours, settings.FlyoutView);
        }

        Assert.AreEqual(0, notifications);
        Assert.AreEqual(WeatherFlyoutView.NextHours, new WeatherSettingsManager(_settingsPath).FlyoutView);
    }

    [TestMethod]
    public void CorruptSettingsAreNotOverwrittenByPicker()
    {
        File.WriteAllText(_settingsPath, "not JSON");
        var settings = new WeatherSettingsManager(_settingsPath);
        JsonException? failure = null;
        try
        {
            settings.SetFlyoutView(WeatherFlyoutView.Glance);
        }
        catch (JsonException ex)
        {
            failure = ex;
        }

        Assert.IsNotNull(failure);
        Assert.AreEqual("not JSON", File.ReadAllText(_settingsPath));
        Assert.AreEqual(WeatherFlyoutView.Classic, settings.FlyoutView);
    }

    [TestMethod]
    public void InvalidSavedViewFallsBackToClassic()
    {
        File.WriteAllText(_settingsPath, """{"weather.FlyoutView":"future-view"}""");
        Assert.AreEqual(WeatherFlyoutView.Classic, new WeatherSettingsManager(_settingsPath).FlyoutView);
    }

    [TestMethod]
    public void SettingsFormCanRestoreClassic()
    {
        var settings = new WeatherSettingsManager(_settingsPath);
        settings.SetFlyoutView(WeatherFlyoutView.DayBrowser);
        settings.Settings.Update("""{"weather.FlyoutView":"Classic"}""");
        settings.RaiseSettingsChanged();
        Assert.AreEqual(WeatherFlyoutView.Classic, settings.FlyoutView);
        Assert.AreEqual(WeatherFlyoutView.Classic, new WeatherSettingsManager(_settingsPath).FlyoutView);
    }

    [TestMethod]
    public void InvalidActionsNeverApplyAView()
    {
        var settings = new WeatherSettingsManager(_settingsPath);
        var form = new WeatherFlyoutPreviewPage.PreviewForm(settings, WeatherFlyoutView.Glance);
        foreach (var data in new[] { "", "[]", """{"action":false}""", """{"action":"unknown"}""" })
        {
            form.SubmitForm("{}", data);
            Assert.AreEqual(WeatherFlyoutView.Classic, settings.FlyoutView);
        }

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => settings.SetFlyoutView((WeatherFlyoutView)999));
    }

    [TestMethod]
    public void GalleryHasSixImageTitleSubtitleTiles()
    {
        var gallery = new WeatherFlyoutGalleryPage(new WeatherSettingsManager(_settingsPath));
        var layout = gallery.GridProperties as GalleryGridLayout;
        Assert.IsNotNull(layout);
        Assert.IsTrue(layout.ShowTitle);
        Assert.IsTrue(layout.ShowSubtitle);
        var tiles = gallery.GetItems();
        Assert.AreEqual(6, tiles.Length);
        CollectionAssert.AreEquivalent(WeatherFlyoutCatalog.Views.Select(WeatherFlyoutCatalog.Name).ToArray(),
            tiles.Select(item => item.Title).ToArray());
        foreach (var tile in tiles)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(tile.Subtitle));
            Assert.IsFalse(tile.Subtitle.Contains("Current default", StringComparison.Ordinal));
            Assert.IsTrue(tile.Icon.Dark.Icon.EndsWith(".png", StringComparison.Ordinal));
        }
    }

    [TestMethod]
    public void EveryGalleryPreviewIsPackagedAndSquare()
    {
        foreach (var view in WeatherFlyoutCatalog.Views)
        {
            foreach (var light in new[] { false, true })
            {
                var bytes = File.ReadAllBytes(WeatherFlyoutCatalog.PreviewPath(view, light));
                Assert.AreEqual(500, BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4)));
                Assert.AreEqual(500, BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4)));
            }
        }
    }

    [TestMethod]
    public void CompactCardsEscapeLocationAndHaveDistinctStructures()
    {
        var snapshot = Snapshot();
        snapshot.Location.Name = "A \"quoted\" place\\\nwith text";
        var counts = new HashSet<int>();
        foreach (var view in WeatherFlyoutCatalog.Views.Take(4))
        {
            var card = JsonNode.Parse(WeatherFlyoutCards.Build(view, snapshot, Instant))!;
            Assert.AreEqual(snapshot.Location.DisplayName, card["body"]![0]!["text"]!.GetValue<string>());
            Assert.AreEqual("AdaptiveCard", card["type"]!.GetValue<string>());
            counts.Add(card["body"]!.AsArray().Count);
        }

        Assert.IsTrue(counts.Count >= 3);
    }

    [TestMethod]
    public void ForecastViewsUseSixHoursAndFiveDays()
    {
        var snapshot = Snapshot();
        var hours = WeatherFlyoutForecasts.Hours(snapshot, Instant.DateTime, 6);
        var days = WeatherFlyoutForecasts.Days(snapshot, Instant.DateTime);
        Assert.AreEqual(6, hours.Length);
        Assert.AreEqual(5, days.Length);
        Assert.IsTrue(hours.All(hour => hour.Time > Instant.DateTime));
        Assert.IsTrue(hours.Last().IsNight);
        Assert.AreEqual(24, WeatherFlyoutForecasts.Hours(snapshot, Instant.DateTime, 24).Length);
    }

    [TestMethod]
    public void PartialArraysDisplayUnavailableInsteadOfInventedValues()
    {
        var snapshot = Snapshot();
        snapshot.Forecast!.Daily!.TemperatureMax = [];
        snapshot.Forecast.Daily.PrecipitationProbabilityMax = null;
        snapshot.Hourly!.Hourly!.Temperature = [];
        snapshot.Hourly.Hourly.WeatherCode = null;
        snapshot.Hourly.Hourly.WindSpeed = [];
        var day = WeatherFlyoutForecasts.Days(snapshot, Instant.DateTime)[0];
        var hour = WeatherFlyoutForecasts.Hours(snapshot, Instant.DateTime, 6)[0];
        Assert.IsNull(day.High);
        Assert.IsNull(day.Rain);
        Assert.IsNull(hour.Temperature);
        Assert.IsNull(hour.Code);
        Assert.IsNull(hour.Wind);
        foreach (var view in WeatherFlyoutCatalog.Views.Take(4))
        {
            Assert.IsNotNull(JsonNode.Parse(WeatherFlyoutCards.Build(view, snapshot, Instant)));
        }

        Assert.AreEqual("--", WeatherFlyoutCards.Percent(day.Rain));
        Assert.AreEqual("--", WeatherFlyoutCards.Temperature(double.NaN, snapshot));
    }

    [TestMethod]
    public void LocationClockHandlesIanaZoneAndDayBoundary()
    {
        var snapshot = Snapshot();
        snapshot.Weather!.Timezone = "Pacific/Auckland";
        Assert.AreEqual(new DateTime(2026, 10, 16, 5, 0, 0), snapshot.LocalTime(Instant));
        snapshot.Weather.Timezone = "America/New_York";
        Assert.AreEqual(new DateTime(2026, 3, 8, 1, 30, 0), snapshot.LocalTime(new DateTimeOffset(2026, 3, 8, 6, 30, 0, TimeSpan.Zero)));
        Assert.AreEqual(new DateTime(2026, 3, 8, 3, 30, 0), snapshot.LocalTime(new DateTimeOffset(2026, 3, 8, 7, 30, 0, TimeSpan.Zero)));
    }

    [TestMethod]
    public void InvalidZoneShowsExplicitUnavailableState()
    {
        var snapshot = Snapshot();
        snapshot.Weather!.Timezone = "not/a-time-zone";
        Assert.IsNull(snapshot.LocalTime(Instant));
        var card = WeatherFlyoutCards.Build(WeatherFlyoutView.DaylightArc, snapshot, Instant);
        Assert.IsTrue(JsonNode.Parse(card)!["body"]!.AsArray()
            .Any(node => node?["text"]?.GetValue<string>() == FlyoutText.Get("TimeZoneUnavailable")));
        Assert.IsFalse(card.Contains("data:image", StringComparison.Ordinal));
    }

    [TestMethod]
    public void DaylightProgressHandlesHorizonBoundaries()
    {
        var rise = Instant.Date.AddHours(6);
        var set = Instant.Date.AddHours(18);
        Assert.AreEqual(0, DaylightArcImage.Progress(rise.AddHours(-1), rise, set));
        Assert.AreEqual(0, DaylightArcImage.Progress(rise, rise, set));
        Assert.AreEqual(0.5, DaylightArcImage.Progress(rise.AddHours(6), rise, set));
        Assert.AreEqual(1, DaylightArcImage.Progress(set, rise, set));
        Assert.AreEqual(1, DaylightArcImage.Progress(set.AddHours(1), rise, set));
        Assert.ThrowsException<ArgumentException>(() => DaylightArcImage.Progress(rise, set, rise));
    }

    [TestMethod]
    public void DaylightImageIsPngWithTransparentBackground()
    {
        var data = DaylightArcImage.Create(Instant.DateTime, Instant.Date.AddHours(6), Instant.Date.AddHours(18));
        using var stream = new MemoryStream(Convert.FromBase64String(data["data:image/png;base64,".Length..]));
        using var image = new Bitmap(stream);
        Assert.AreEqual(900, image.Width);
        Assert.AreEqual(400, image.Height);
        Assert.AreEqual(0, (int)image.GetPixel(0, 0).A);
    }

    [TestMethod]
    public void DaylightLabelsHandleBeforeAndAfterDaylight()
    {
        var snapshot = Snapshot();
        var before = WeatherFlyoutCards.Build(WeatherFlyoutView.DaylightArc, snapshot, new DateTimeOffset(Instant.Date.AddHours(5), TimeSpan.Zero));
        var after = WeatherFlyoutCards.Build(WeatherFlyoutView.DaylightArc, snapshot, new DateTimeOffset(Instant.Date.AddHours(19), TimeSpan.Zero));
        Assert.IsTrue(JsonNode.Parse(before)!["body"]!.AsArray().Any(node =>
            node?["text"]?.GetValue<string>() == FlyoutText.Format("UntilSunrise", WeatherFlyoutCards.Duration(TimeSpan.FromHours(1)))));
        Assert.IsTrue(JsonNode.Parse(after)!["body"]!.AsArray().Any(node =>
            node?["text"]?.GetValue<string>() == FlyoutText.Get("AfterSunset")));
    }

    [TestMethod]
    public void CachedWeatherShowsObservationTimeNotFetchTime()
    {
        var snapshot = Snapshot();
        snapshot.Weather!.Current!.Time = "2026-10-14T12:34";
        var card = JsonNode.Parse(WeatherFlyoutCards.Build(WeatherFlyoutView.Glance, snapshot, Instant))!;
        Assert.AreEqual(FlyoutText.Format("Updated", new DateTime(2026, 10, 14, 12, 34, 0).ToString("g", CultureInfo.CurrentCulture)),
            card["body"]!.AsArray().Last()!["text"]!.GetValue<string>());
    }

    [TestMethod]
    public void MissingOrStaleSolarTimesDoNotInventDaylight()
    {
        var snapshot = Snapshot();
        snapshot.Forecast!.Daily!.Sunrise = [];
        var card = WeatherFlyoutCards.Build(WeatherFlyoutView.DaylightArc, snapshot, Instant);
        Assert.IsTrue(card.Contains(FlyoutText.Get("DaylightUnavailable"), StringComparison.Ordinal));
        Assert.IsFalse(card.Contains("data:image", StringComparison.Ordinal));
        snapshot.Forecast.Daily.Sunrise = ["2026-10-14T06:00"];
        card = WeatherFlyoutCards.Build(WeatherFlyoutView.DaylightArc, snapshot, Instant);
        Assert.IsFalse(card.Contains("data:image", StringComparison.Ordinal));
    }

    [TestMethod]
    public void SolarClockPreservesMinutesInBothFormats()
    {
        var time = Instant.Date.AddHours(18).AddMinutes(12);
        Assert.IsTrue(WeatherFlyoutCards.Clock(time, Snapshot()).Contains("6:12", StringComparison.Ordinal));
        Assert.AreEqual("18:12", WeatherFlyoutCards.Clock(time, Snapshot() with { Use24HourClock = true }));
    }

    [TestMethod]
    public void MissingCurrentWeatherShowsError()
    {
        var card = WeatherFlyoutCards.Build(WeatherFlyoutView.Glance, Snapshot() with { Weather = null }, Instant);
        Assert.IsTrue(card.Contains(Resources.weather_service_error, StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task ViewChangesReachEveryBandWithoutRecreatingTimersOrFetching()
    {
        var handler = new WeatherHandler();
        using var service = new OpenMeteoService(handler);
        var settings = new WeatherSettingsManager(_settingsPath);
        var geocoding = Mock.Of<IGeocodingService>();
        var first = new GeocodingResult { Name = "Huntsville", Latitude = 34.7, Longitude = -86.6 };
        var second = new GeocodingResult { Name = "Birmingham", Latitude = 33.5, Longitude = -86.8 };
        var firstCard = new WeatherBandCard(service, geocoding, settings, fixedLocation: first);
        var secondCard = new WeatherBandCard(service, geocoding, settings, fixedLocation: second);
        Assert.AreNotEqual(firstCard.Id, secondCard.Id);
        using var firstBand = new PinnedWeatherBand(first, service, settings, firstCard);
        using var secondBand = new PinnedWeatherBand(second, service, settings, secondCard);
        await firstCard.LoadWeatherDataAsync();
        await secondCard.LoadWeatherDataAsync();
        var requestCount = handler.RequestCount;
        var timerField = typeof(PinnedWeatherBand).GetField("_updateTimer", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var timer = timerField.GetValue(firstBand);
        Assert.AreSame(firstCard, firstBand.Command);
        Assert.IsTrue(firstBand.MoreCommands.Any(command => command is CommandContextItem));

        settings.SetFlyoutView(WeatherFlyoutView.DayBrowser);
        Assert.IsInstanceOfType<WeatherFlyoutListPage>(firstBand.Command);
        var firstPage = (WeatherFlyoutListPage)firstBand.Command!;
        var firstItems = firstPage.GetItems();
        Assert.IsInstanceOfType<WeatherFlyoutListPage>(secondBand.Command);
        var secondPage = (WeatherFlyoutListPage)secondBand.Command!;
        secondPage.GetItems();
        Assert.AreEqual(first.DisplayName, firstPage.Title);
        Assert.AreEqual(second.DisplayName, secondPage.Title);
        Assert.AreEqual(ContentSize.Small, ((Details)firstItems[0].Details).Size);
        settings.SetFlyoutView(WeatherFlyoutView.HourInspector);
        var hourItems = ((WeatherFlyoutListPage)firstBand.Command!).GetItems();
        Assert.AreEqual(ContentSize.Large, ((Details)hourItems[0].Details).Size);
        settings.SetFlyoutView(WeatherFlyoutView.DayBrowser);
        Assert.AreSame(firstPage, firstBand.Command);
        settings.SetFlyoutView(WeatherFlyoutView.Glance);
        Assert.AreSame(firstCard, firstBand.Command);
        Assert.AreSame(secondCard, secondBand.Command);
        Assert.AreSame(timer, timerField.GetValue(firstBand));
        Assert.AreEqual(requestCount, handler.RequestCount);
        firstBand.Dispose();
        settings.SetFlyoutView(WeatherFlyoutView.HourInspector);
        Assert.IsTrue(firstBand.IsDisposed);
    }

    private static WeatherFlyoutSnapshot Snapshot() => Snapshot(Instant);

    private static WeatherFlyoutSnapshot Snapshot(DateTimeOffset now)
    {
        var start = now.UtcDateTime.Date;
        var dates = Enumerable.Range(0, 7).Select(day => start.AddDays(day)).ToArray();
        var hours = Enumerable.Range(1, 30).Select(hour => now.UtcDateTime.AddHours(hour)).ToArray();
        return new WeatherFlyoutSnapshot(new GeocodingResult { Name = "Huntsville" },
            new WeatherData
            {
                Timezone = "UTC",
                Current = new CurrentWeather { Temperature = 23, ApparentTemperature = 22, IsDay = 1, WeatherCode = 2, RelativeHumidity = 48 },
            },
            new ForecastData
            {
                Timezone = "UTC",
                Daily = new DailyForecast
                {
                    Time = dates.Select(date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).ToList(),
                    Sunrise = dates.Select(date => date.AddHours(6).ToString("s", CultureInfo.InvariantCulture)).ToList(),
                    Sunset = dates.Select(date => date.AddHours(18).ToString("s", CultureInfo.InvariantCulture)).ToList(),
                    WeatherCode = dates.Select(_ => 2).ToList(),
                    TemperatureMax = dates.Select(_ => 25.0).ToList(),
                    TemperatureMin = dates.Select(_ => 13.0).ToList(),
                    PrecipitationProbabilityMax = dates.Select(_ => 5).ToList(),
                },
            },
            new HourlyForecastData
            {
                Timezone = "UTC",
                Hourly = new HourlyForecast
                {
                    Time = hours.Select(hour => hour.ToString("s", CultureInfo.InvariantCulture)).ToList(),
                    WeatherCode = hours.Select(_ => 0).ToList(),
                    Temperature = hours.Select(_ => 23.0).ToList(),
                    ApparentTemperature = hours.Select(_ => 22.0).ToList(),
                    PrecipitationProbability = hours.Select(_ => 5).ToList(),
                    RelativeHumidity = hours.Select(_ => 48).ToList(),
                    WindSpeed = hours.Select(_ => 6.0).ToList(),
                },
            }, "celsius", "kmh", false, now);
    }

    private sealed class WeatherHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            var snapshot = Snapshot(DateTimeOffset.UtcNow);
            var json = JsonSerializer.SerializeToNode(snapshot.Weather, WeatherJsonContext.Default.WeatherData)!.AsObject();
            json["daily"] = JsonSerializer.SerializeToNode(snapshot.Forecast, WeatherJsonContext.Default.ForecastData)!["daily"]!.DeepClone();
            json["hourly"] = JsonSerializer.SerializeToNode(snapshot.Hourly, WeatherJsonContext.Default.HourlyForecastData)!["hourly"]!.DeepClone();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json.ToJsonString()) });
        }
    }
}
