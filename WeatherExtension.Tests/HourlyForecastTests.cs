// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CmdPal.Ext.Weather.Models;
using Microsoft.CmdPal.Ext.Weather.Pages;
using Microsoft.CmdPal.Ext.Weather.Services;

namespace Microsoft.CmdPal.Ext.Weather.UnitTests;

/// <summary>
/// Regression tests for issue #25: hourly forecasts stop at end of day.
/// The fix requests forecast_days=2 from the API and caps the UI at 24 hours.
/// </summary>
[TestClass]
public class HourlyForecastTests
{
    private static readonly BindingFlags AnyInstance =
        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

    private static readonly BindingFlags PrivateInstance =
        BindingFlags.NonPublic | BindingFlags.Instance;

    private string _tempFilePath = string.Empty;
    private WeatherSettingsManager? _settingsManager;

    [TestInitialize]
    public void Setup()
    {
        _tempFilePath = Path.Combine(
            Path.GetTempPath(),
            $"weather-test-hourly-{Guid.NewGuid()}.json");
        _settingsManager = new WeatherSettingsManager(_tempFilePath);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    // ---------------------------------------------------------------
    // Test 1: API requests 2 forecast days (not 1)
    // ---------------------------------------------------------------

    [TestMethod]
    public void GetHourlyForecastAsync_RequestsTwoForecastDays()
    {
        var stateMachineType = typeof(OpenMeteoService)
            .GetNestedTypes(BindingFlags.NonPublic)
            .FirstOrDefault(t =>
                typeof(IAsyncStateMachine).IsAssignableFrom(t) &&
                t.Name.Contains("<GetHourlyForecastAsync>"));

        Assert.IsNotNull(stateMachineType,
            "Could not find the async state machine for GetHourlyForecastAsync");

        var moveNext = stateMachineType!.GetMethod("MoveNext", AnyInstance);
        Assert.IsNotNull(moveNext);

        var il = moveNext!.GetMethodBody()?.GetILAsByteArray();
        Assert.IsNotNull(il);

        var foundDays2 = false;
        var foundDays1 = false;

        // Scan for ldstr (0x72) opcodes referencing string tokens
        for (var i = 0; i < il!.Length - 4; i++)
        {
            if (il[i] != 0x72)
            {
                continue;
            }

            var token = BitConverter.ToInt32(il, i + 1);

            try
            {
                var str = stateMachineType.Module.ResolveString(token);

                if (str.Contains("forecast_days=2"))
                {
                    foundDays2 = true;
                }

                if (str.Contains("forecast_days=1"))
                {
                    foundDays1 = true;
                }
            }
            catch
            {
            }
        }

        Assert.IsTrue(foundDays2,
            "GetHourlyForecastAsync must use forecast_days=2 so evening " +
            "hours span into the next day (issue #25)");
        Assert.IsFalse(foundDays1,
            "GetHourlyForecastAsync must NOT use forecast_days=1 (issue #25 fix)");
    }

    // ---------------------------------------------------------------
    // Test 2: UI caps hourly list at 24 future hours
    // ---------------------------------------------------------------

    [TestMethod]
    public void CreateHourlyItems_With48HoursOfData_ReturnsAtMost24()
    {
        var now = DateTime.Now;
        var hourlyData = BuildHourlyForecastData(now.AddHours(1), 48);

        var items = InvokeCreateHourlyItems(hourlyData);

        Assert.IsTrue(items.Count > 0, "Should return some hourly items");
        Assert.IsTrue(items.Count <= 24,
            $"Expected at most 24 items but got {items.Count}. " +
            "The 24-hour cap must be enforced (issue #25).");
    }

    // ---------------------------------------------------------------
    // Test 3: Past hours are excluded
    // ---------------------------------------------------------------

    [TestMethod]
    public void CreateHourlyItems_ExcludesPastHours()
    {
        var now = DateTime.Now;

        // Build data spanning 12 hours in the past + 12 hours in the future
        var hourlyData = BuildHourlyForecastData(now.AddHours(-12), 24);

        var items = InvokeCreateHourlyItems(hourlyData);

        Assert.IsTrue(items.Count > 0, "Should have future items");
        Assert.IsTrue(items.Count < 24,
            $"Past hours should be filtered out, but got {items.Count} of 24 items. " +
            "About half the data is in the past and should be excluded.");
    }

    // ---------------------------------------------------------------
    // Test 4: Late-evening scenario crosses midnight
    // ---------------------------------------------------------------

    [TestMethod]
    public void CreateHourlyItems_SpanningMidnight_ReturnsExactly24FutureHours()
    {
        var now = DateTime.Now;

        // 48 hours of data starting 1 hour from now always crosses midnight.
        // With the 24-hour cap, we should get exactly 24 items regardless
        // of what time it is — proving no day-boundary cutoff exists.
        var hourlyData = BuildHourlyForecastData(now.AddHours(1), 48);

        var items = InvokeCreateHourlyItems(hourlyData);

        Assert.AreEqual(24, items.Count,
            "Should get exactly 24 future-hour items spanning into tomorrow. " +
            "The list must not stop at midnight (issue #25).");
    }

    // ---------------------------------------------------------------
    // Test 5: Dock band card finds 3 future hours with 2-day data
    // ---------------------------------------------------------------

    [TestMethod]
    public void WeatherBandCard_BuildWeatherData_Finds3FutureHoursFromSpanningData()
    {
        var cardType = typeof(WeatherBandCard);
        var card = RuntimeHelpers.GetUninitializedObject(cardType);

        var settingsField = cardType.GetField("_settings", PrivateInstance);
        Assert.IsNotNull(settingsField, "WeatherBandCard must have _settings field");
        settingsField!.SetValue(card, _settingsManager);

        var location = new GeocodingResult
        {
            Name = "Seattle",
            Admin1 = "WA",
            Country = "US",
            Latitude = 47.6,
            Longitude = -122.33,
        };

        var now = DateTime.Now;
        var hourlyData = BuildHourlyForecastData(now.AddHours(1), 48);

        var buildMethod = cardType.GetMethod("BuildWeatherData", PrivateInstance);
        Assert.IsNotNull(buildMethod,
            "WeatherBandCard must have BuildWeatherData method");

        var json = (string)buildMethod!.Invoke(card, [location, null, null, hourlyData])!;
        Assert.IsNotNull(json);

        // All three hourly slots should have real values, not "--" placeholders.
        // With forecast_days=2 the band always finds ≥3 future hours.
        Assert.IsFalse(json.Contains("\"hour1Time\": \"--\""),
            "hour1Time should not be a placeholder when 2-day data is available");
        Assert.IsFalse(json.Contains("\"hour2Time\": \"--\""),
            "hour2Time should not be a placeholder when 2-day data is available");
        Assert.IsFalse(json.Contains("\"hour3Time\": \"--\""),
            "hour3Time should not be a placeholder when 2-day data is available");
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    private static HourlyForecastData BuildHourlyForecastData(
        DateTime startTime, int hours)
    {
        var times = new List<string>();
        var temps = new List<double>();
        var apparent = new List<double>();
        var weatherCodes = new List<int>();
        var precip = new List<int>();
        var wind = new List<double>();
        var humidity = new List<int>();

        for (var i = 0; i < hours; i++)
        {
            var t = startTime.AddHours(i);
            times.Add(t.ToString("yyyy-MM-ddTHH:00", CultureInfo.InvariantCulture));
            temps.Add(20.0 + i);
            apparent.Add(18.0 + i);
            weatherCodes.Add(0);
            precip.Add(10);
            wind.Add(5.0);
            humidity.Add(60);
        }

        return new HourlyForecastData
        {
            Hourly = new HourlyForecast
            {
                Time = times,
                Temperature = temps,
                ApparentTemperature = apparent,
                WeatherCode = weatherCodes,
                PrecipitationProbability = precip,
                WindSpeed = wind,
                RelativeHumidity = humidity,
            },
            Latitude = 47.6,
            Longitude = -122.33,
            Timezone = "America/Los_Angeles",
        };
    }

    private IList InvokeCreateHourlyItems(HourlyForecastData data)
    {
        var pageType = typeof(HourlyForecastPage);
        var page = RuntimeHelpers.GetUninitializedObject(pageType);

        var settingsField = pageType.GetField("_settingsManager", PrivateInstance);
        Assert.IsNotNull(settingsField,
            "HourlyForecastPage must have _settingsManager field");
        settingsField!.SetValue(page, _settingsManager);

        var method = pageType.GetMethod("CreateHourlyItems", PrivateInstance);
        Assert.IsNotNull(method,
            "HourlyForecastPage must have CreateHourlyItems method");

        var result = method!.Invoke(page, [data]);
        Assert.IsNotNull(result);

        return (IList)result!;
    }
}
