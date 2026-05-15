// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Microsoft.CmdPal.Ext.Weather.Models;
using Microsoft.CmdPal.Ext.Weather.Services;

namespace Microsoft.CmdPal.Ext.Weather.UnitTests;

[TestClass]
public class HourlyForecastDataTests
{
    [TestMethod]
    public void HourlyForecastData_DeserializeValidJson_AllFieldsPopulated()
    {
        var json = """
        {
            "latitude": 47.6,
            "longitude": -122.33,
            "timezone": "America/Los_Angeles",
            "hourly": {
                "time": ["2026-03-02T00:00", "2026-03-02T01:00", "2026-03-02T02:00"],
                "temperature_2m": [5.2, 4.8, 4.5],
                "apparent_temperature": [2.1, 1.8, 1.5],
                "weather_code": [0, 1, 3],
                "precipitation_probability": [0, 5, 10],
                "wind_speed_10m": [8.5, 7.2, 6.8],
                "relative_humidity_2m": [75, 78, 80]
            }
        }
        """;

        var hourlyData = JsonSerializer.Deserialize<HourlyForecastData>(json, WeatherJsonContext.Default.HourlyForecastData);

        Assert.IsNotNull(hourlyData);
        Assert.AreEqual(47.6, hourlyData.Latitude);
        Assert.AreEqual(-122.33, hourlyData.Longitude);
        Assert.AreEqual("America/Los_Angeles", hourlyData.Timezone);

        Assert.IsNotNull(hourlyData.Hourly);
        Assert.IsNotNull(hourlyData.Hourly.Time);
        Assert.AreEqual(3, hourlyData.Hourly.Time.Count);
        Assert.AreEqual("2026-03-02T00:00", hourlyData.Hourly.Time[0]);
        Assert.AreEqual("2026-03-02T01:00", hourlyData.Hourly.Time[1]);
        Assert.AreEqual("2026-03-02T02:00", hourlyData.Hourly.Time[2]);

        Assert.IsNotNull(hourlyData.Hourly.Temperature);
        Assert.AreEqual(3, hourlyData.Hourly.Temperature.Count);
        Assert.AreEqual(5.2, hourlyData.Hourly.Temperature[0]);
        Assert.AreEqual(4.8, hourlyData.Hourly.Temperature[1]);
        Assert.AreEqual(4.5, hourlyData.Hourly.Temperature[2]);

        Assert.IsNotNull(hourlyData.Hourly.ApparentTemperature);
        Assert.AreEqual(3, hourlyData.Hourly.ApparentTemperature.Count);
        Assert.AreEqual(2.1, hourlyData.Hourly.ApparentTemperature[0]);
        Assert.AreEqual(1.8, hourlyData.Hourly.ApparentTemperature[1]);
        Assert.AreEqual(1.5, hourlyData.Hourly.ApparentTemperature[2]);

        Assert.IsNotNull(hourlyData.Hourly.WeatherCode);
        Assert.AreEqual(3, hourlyData.Hourly.WeatherCode.Count);
        Assert.AreEqual(0, hourlyData.Hourly.WeatherCode[0]);
        Assert.AreEqual(1, hourlyData.Hourly.WeatherCode[1]);
        Assert.AreEqual(3, hourlyData.Hourly.WeatherCode[2]);

        Assert.IsNotNull(hourlyData.Hourly.PrecipitationProbability);
        Assert.AreEqual(3, hourlyData.Hourly.PrecipitationProbability.Count);
        Assert.AreEqual(0, hourlyData.Hourly.PrecipitationProbability[0]);
        Assert.AreEqual(5, hourlyData.Hourly.PrecipitationProbability[1]);
        Assert.AreEqual(10, hourlyData.Hourly.PrecipitationProbability[2]);

        Assert.IsNotNull(hourlyData.Hourly.WindSpeed);
        Assert.AreEqual(3, hourlyData.Hourly.WindSpeed.Count);
        Assert.AreEqual(8.5, hourlyData.Hourly.WindSpeed[0]);
        Assert.AreEqual(7.2, hourlyData.Hourly.WindSpeed[1]);
        Assert.AreEqual(6.8, hourlyData.Hourly.WindSpeed[2]);

        Assert.IsNotNull(hourlyData.Hourly.RelativeHumidity);
        Assert.AreEqual(3, hourlyData.Hourly.RelativeHumidity.Count);
        Assert.AreEqual(75, hourlyData.Hourly.RelativeHumidity[0]);
        Assert.AreEqual(78, hourlyData.Hourly.RelativeHumidity[1]);
        Assert.AreEqual(80, hourlyData.Hourly.RelativeHumidity[2]);
    }

    [TestMethod]
    public void HourlyForecastData_DeserializeWithMissingHourlyBlock_HourlyIsNull()
    {
        var json = """
        {
            "latitude": 47.6,
            "longitude": -122.33,
            "timezone": "America/Los_Angeles"
        }
        """;

        var hourlyData = JsonSerializer.Deserialize<HourlyForecastData>(json, WeatherJsonContext.Default.HourlyForecastData);

        Assert.IsNotNull(hourlyData);
        Assert.AreEqual(47.6, hourlyData.Latitude);
        Assert.AreEqual(-122.33, hourlyData.Longitude);
        Assert.AreEqual("America/Los_Angeles", hourlyData.Timezone);
        Assert.IsNull(hourlyData.Hourly);
    }

    [TestMethod]
    public void HourlyForecastData_DeserializeWithEmptyArrays_ListsAreEmpty()
    {
        var json = """
        {
            "latitude": 47.6,
            "longitude": -122.33,
            "timezone": "America/Los_Angeles",
            "hourly": {
                "time": [],
                "temperature_2m": [],
                "apparent_temperature": [],
                "weather_code": [],
                "precipitation_probability": [],
                "wind_speed_10m": [],
                "relative_humidity_2m": []
            }
        }
        """;

        var hourlyData = JsonSerializer.Deserialize<HourlyForecastData>(json, WeatherJsonContext.Default.HourlyForecastData);

        Assert.IsNotNull(hourlyData);
        Assert.IsNotNull(hourlyData.Hourly);
        Assert.IsNotNull(hourlyData.Hourly.Time);
        Assert.AreEqual(0, hourlyData.Hourly.Time.Count);
        Assert.IsNotNull(hourlyData.Hourly.Temperature);
        Assert.AreEqual(0, hourlyData.Hourly.Temperature.Count);
        Assert.IsNotNull(hourlyData.Hourly.ApparentTemperature);
        Assert.AreEqual(0, hourlyData.Hourly.ApparentTemperature.Count);
        Assert.IsNotNull(hourlyData.Hourly.WeatherCode);
        Assert.AreEqual(0, hourlyData.Hourly.WeatherCode.Count);
        Assert.IsNotNull(hourlyData.Hourly.PrecipitationProbability);
        Assert.AreEqual(0, hourlyData.Hourly.PrecipitationProbability.Count);
        Assert.IsNotNull(hourlyData.Hourly.WindSpeed);
        Assert.AreEqual(0, hourlyData.Hourly.WindSpeed.Count);
        Assert.IsNotNull(hourlyData.Hourly.RelativeHumidity);
        Assert.AreEqual(0, hourlyData.Hourly.RelativeHumidity.Count);
    }

    [TestMethod]
    public void HourlyForecastData_DeserializeWithPartialData_MissingArraysAreNull()
    {
        var json = """
        {
            "latitude": 47.6,
            "longitude": -122.33,
            "timezone": "America/Los_Angeles",
            "hourly": {
                "time": ["2026-03-02T00:00", "2026-03-02T01:00"],
                "temperature_2m": [5.2, 4.8]
            }
        }
        """;

        var hourlyData = JsonSerializer.Deserialize<HourlyForecastData>(json, WeatherJsonContext.Default.HourlyForecastData);

        Assert.IsNotNull(hourlyData);
        Assert.IsNotNull(hourlyData.Hourly);
        Assert.IsNotNull(hourlyData.Hourly.Time);
        Assert.AreEqual(2, hourlyData.Hourly.Time.Count);
        Assert.IsNotNull(hourlyData.Hourly.Temperature);
        Assert.AreEqual(2, hourlyData.Hourly.Temperature.Count);
        Assert.IsNull(hourlyData.Hourly.ApparentTemperature);
        Assert.IsNull(hourlyData.Hourly.WeatherCode);
        Assert.IsNull(hourlyData.Hourly.PrecipitationProbability);
        Assert.IsNull(hourlyData.Hourly.WindSpeed);
        Assert.IsNull(hourlyData.Hourly.RelativeHumidity);
    }
}
