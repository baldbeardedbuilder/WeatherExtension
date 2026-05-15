// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Microsoft.CmdPal.Ext.Weather.Models;
using Microsoft.CmdPal.Ext.Weather.Services;

namespace Microsoft.CmdPal.Ext.Weather.UnitTests;

[TestClass]
public class WeatherDataModelTests
{
    [TestMethod]
    public void WeatherData_DeserializeRealCurrentWeatherResponse_Success()
    {
        // Real Open-Meteo current weather JSON response
        var json = """
        {
            "latitude": 47.6062,
            "longitude": -122.3321,
            "timezone": "America/Los_Angeles",
            "current": {
                "time": "2024-01-15T12:00",
                "temperature_2m": 10.5,
                "relative_humidity_2m": 75,
                "apparent_temperature": 8.2,
                "weather_code": 3,
                "wind_speed_10m": 12.5,
                "wind_direction_10m": 180
            }
        }
        """;

        var weatherData = JsonSerializer.Deserialize<WeatherData>(json, WeatherJsonContext.Default.WeatherData);

        Assert.IsNotNull(weatherData);
        Assert.AreEqual(47.6062, weatherData.Latitude);
        Assert.AreEqual(-122.3321, weatherData.Longitude);
        Assert.AreEqual("America/Los_Angeles", weatherData.Timezone);

        Assert.IsNotNull(weatherData.Current);
        Assert.AreEqual("2024-01-15T12:00", weatherData.Current.Time);
        Assert.AreEqual(10.5, weatherData.Current.Temperature);
        Assert.AreEqual(75, weatherData.Current.RelativeHumidity);
        Assert.AreEqual(8.2, weatherData.Current.ApparentTemperature);
        Assert.AreEqual(3, weatherData.Current.WeatherCode);
        Assert.AreEqual(12.5, weatherData.Current.WindSpeed);
        Assert.AreEqual(180, weatherData.Current.WindDirection);
    }

    [TestMethod]
    public void WeatherData_DeserializeWithNullCurrent_HandledGracefully()
    {
        var json = """
        {
            "latitude": 47.6062,
            "longitude": -122.3321,
            "timezone": "America/Los_Angeles"
        }
        """;

        var weatherData = JsonSerializer.Deserialize<WeatherData>(json, WeatherJsonContext.Default.WeatherData);

        Assert.IsNotNull(weatherData);
        Assert.AreEqual(47.6062, weatherData.Latitude);
        Assert.AreEqual(-122.3321, weatherData.Longitude);
        Assert.AreEqual("America/Los_Angeles", weatherData.Timezone);
        Assert.IsNull(weatherData.Current);
    }

    [TestMethod]
    public void ForecastData_DeserializeForecastResponse_Success()
    {
        // Real Open-Meteo forecast JSON response
        var json = """
        {
            "latitude": 47.6062,
            "longitude": -122.3321,
            "timezone": "America/Los_Angeles",
            "daily": {
                "time": ["2024-01-15", "2024-01-16", "2024-01-17"],
                "weather_code": [3, 61, 95],
                "temperature_2m_max": [12.5, 10.2, 8.7],
                "temperature_2m_min": [5.1, 3.8, 2.5],
                "precipitation_probability_max": [20, 80, 90]
            }
        }
        """;

        var forecastData = JsonSerializer.Deserialize<ForecastData>(json, WeatherJsonContext.Default.ForecastData);

        Assert.IsNotNull(forecastData);
        Assert.AreEqual(47.6062, forecastData.Latitude);
        Assert.AreEqual(-122.3321, forecastData.Longitude);
        Assert.AreEqual("America/Los_Angeles", forecastData.Timezone);

        Assert.IsNotNull(forecastData.Daily);
        Assert.IsNotNull(forecastData.Daily.Time);
        Assert.AreEqual(3, forecastData.Daily.Time.Count);
        Assert.AreEqual("2024-01-15", forecastData.Daily.Time[0]);
        Assert.AreEqual("2024-01-16", forecastData.Daily.Time[1]);
        Assert.AreEqual("2024-01-17", forecastData.Daily.Time[2]);

        Assert.IsNotNull(forecastData.Daily.WeatherCode);
        Assert.AreEqual(3, forecastData.Daily.WeatherCode.Count);
        Assert.AreEqual(3, forecastData.Daily.WeatherCode[0]);
        Assert.AreEqual(61, forecastData.Daily.WeatherCode[1]);
        Assert.AreEqual(95, forecastData.Daily.WeatherCode[2]);

        Assert.IsNotNull(forecastData.Daily.TemperatureMax);
        Assert.AreEqual(3, forecastData.Daily.TemperatureMax.Count);
        Assert.AreEqual(12.5, forecastData.Daily.TemperatureMax[0]);

        Assert.IsNotNull(forecastData.Daily.TemperatureMin);
        Assert.AreEqual(3, forecastData.Daily.TemperatureMin.Count);
        Assert.AreEqual(5.1, forecastData.Daily.TemperatureMin[0]);

        Assert.IsNotNull(forecastData.Daily.PrecipitationProbabilityMax);
        Assert.AreEqual(3, forecastData.Daily.PrecipitationProbabilityMax.Count);
        Assert.AreEqual(20, forecastData.Daily.PrecipitationProbabilityMax[0]);
    }

    [TestMethod]
    public void ForecastData_DeserializeWithNullDaily_HandledGracefully()
    {
        var json = """
        {
            "latitude": 47.6062,
            "longitude": -122.3321,
            "timezone": "America/Los_Angeles"
        }
        """;

        var forecastData = JsonSerializer.Deserialize<ForecastData>(json, WeatherJsonContext.Default.ForecastData);

        Assert.IsNotNull(forecastData);
        Assert.AreEqual(47.6062, forecastData.Latitude);
        Assert.AreEqual(-122.3321, forecastData.Longitude);
        Assert.AreEqual("America/Los_Angeles", forecastData.Timezone);
        Assert.IsNull(forecastData.Daily);
    }

    [TestMethod]
    public void ConvertNominatimResults_WithStructuredAddress_MapsCorrectly()
    {
        var nominatimResults = new List<NominatimResult>
        {
            new()
            {
                Lat = 47.6062,
                Lon = -122.3321,
                Name = "Seattle",
                DisplayName = "Seattle, King County, Washington, United States",
                Address = new NominatimAddress
                {
                    City = "Seattle",
                    State = "Washington",
                    Country = "United States",
                    CountryCode = "us",
                },
            },
        };

        var results = GeocodingService.ConvertNominatimResults(nominatimResults);

        Assert.AreEqual(1, results.Count);
        var result = results[0];
        Assert.AreEqual(47.6062, result.Latitude);
        Assert.AreEqual(-122.3321, result.Longitude);
        Assert.AreEqual("Seattle", result.Name);
        Assert.AreEqual("Washington", result.Admin1);
        Assert.AreEqual("United States", result.Country);
        Assert.AreEqual("US", result.CountryCode);
    }

    [TestMethod]
    public void ConvertNominatimResults_WithTownFallback_UsesTownForName()
    {
        var nominatimResults = new List<NominatimResult>
        {
            new()
            {
                Lat = 51.75,
                Lon = -1.25,
                DisplayName = "Kidlington, Cherwell, Oxfordshire, England, United Kingdom",
                Address = new NominatimAddress
                {
                    Town = "Kidlington",
                    State = "England",
                    Country = "United Kingdom",
                    CountryCode = "gb",
                },
            },
        };

        var results = GeocodingService.ConvertNominatimResults(nominatimResults);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("Kidlington", results[0].Name);
    }

    [TestMethod]
    public void ConvertNominatimResults_WithNoAddress_FallsBackToDisplayName()
    {
        var nominatimResults = new List<NominatimResult>
        {
            new()
            {
                Lat = 33.5207,
                Lon = -86.8025,
                DisplayName = "Birmingham, Jefferson County, Alabama, United States",
            },
        };

        var results = GeocodingService.ConvertNominatimResults(nominatimResults);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("Birmingham", results[0].Name);
        Assert.AreEqual("United States", results[0].Country);
    }

    [TestMethod]
    public void ConvertNominatimResults_WithEmptyList_ReturnsEmptyList()
    {
        var results = GeocodingService.ConvertNominatimResults([]);

        Assert.AreEqual(0, results.Count);
    }

    [TestMethod]
    public void ConvertNominatimResults_CountryCodeUppercased()
    {
        var nominatimResults = new List<NominatimResult>
        {
            new()
            {
                Lat = 48.8566,
                Lon = 2.3522,
                Name = "Paris",
                Address = new NominatimAddress
                {
                    City = "Paris",
                    Country = "France",
                    CountryCode = "fr",
                },
            },
        };

        var results = GeocodingService.ConvertNominatimResults(nominatimResults);

        Assert.AreEqual("FR", results[0].CountryCode);
    }

    [TestMethod]
    public void ConvertNominatimResults_MapsPlaceIdToGeocodingResultId()
    {
        var nominatimResults = new List<NominatimResult>
        {
            new()
            {
                PlaceId = 123456789L,
                Lat = 47.6062,
                Lon = -122.3321,
                Name = "Seattle",
                Address = new NominatimAddress
                {
                    City = "Seattle",
                    State = "Washington",
                    Country = "United States",
                    CountryCode = "us",
                },
            },
        };

        var results = GeocodingService.ConvertNominatimResults(nominatimResults);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(123456789L, results[0].Id);
    }

    [TestMethod]
    public void ConvertNominatimResults_MultipleResults_HaveDistinctIds()
    {
        var nominatimResults = new List<NominatimResult>
        {
            new()
            {
                PlaceId = 111L,
                Lat = 47.6062,
                Lon = -122.3321,
                Name = "Seattle",
                Address = new NominatimAddress { City = "Seattle", Country = "United States" },
            },
            new()
            {
                PlaceId = 222L,
                Lat = 51.5074,
                Lon = -0.1278,
                Name = "London",
                Address = new NominatimAddress { City = "London", Country = "United Kingdom" },
            },
        };

        var results = GeocodingService.ConvertNominatimResults(nominatimResults);

        Assert.AreEqual(2, results.Count);
        Assert.AreNotEqual(results[0].Id, results[1].Id);
    }

    [TestMethod]
    public void ConvertNominatimResults_AcceptsLargePlaceId_BeyondIntRange()
    {
        var largePlaceId = (long)int.MaxValue + 1L;
        var nominatimResults = new List<NominatimResult>
        {
            new()
            {
                PlaceId = largePlaceId,
                Lat = 48.8566,
                Lon = 2.3522,
                Name = "Paris",
                Address = new NominatimAddress { City = "Paris", Country = "France" },
            },
        };

        var results = GeocodingService.ConvertNominatimResults(nominatimResults);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(largePlaceId, results[0].Id);
    }

    [TestMethod]
    public void PinnedLocation_ToGeocodingResult_DerivedIdIsNonZero()
    {
        var pinned = new PinnedLocation
        {
            Latitude = 47.6062,
            Longitude = -122.3321,
            Name = "Seattle",
        };

        var result = pinned.ToGeocodingResult();

        Assert.AreNotEqual(0L, result.Id);
    }

    [TestMethod]
    public void PinnedLocation_ToGeocodingResult_DerivedIdIsDeterministic()
    {
        var pinned = new PinnedLocation
        {
            Latitude = 47.6062,
            Longitude = -122.3321,
            Name = "Seattle",
        };

        var result1 = pinned.ToGeocodingResult();
        var result2 = pinned.ToGeocodingResult();

        Assert.AreEqual(result1.Id, result2.Id);
    }

    [TestMethod]
    public void PinnedLocation_ToGeocodingResult_DerivedIdIsNonNegative()
    {
        // Verify IDs are positive even when lat/lon are negative
        var pinned = new PinnedLocation
        {
            Latitude = -33.8688,
            Longitude = -70.6693,
            Name = "Santiago",
        };

        var result = pinned.ToGeocodingResult();

        Assert.IsTrue(result.Id >= 0, $"Expected non-negative ID but got {result.Id}");
    }

    [TestMethod]
    public void PinnedLocation_ToGeocodingResult_DifferentLocationsHaveDistinctIds()
    {
        var seattle = new PinnedLocation { Latitude = 47.6062, Longitude = -122.3321, Name = "Seattle" };
        var london = new PinnedLocation { Latitude = 51.5074, Longitude = -0.1278, Name = "London" };

        var seattleResult = seattle.ToGeocodingResult();
        var londonResult = london.ToGeocodingResult();

        Assert.AreNotEqual(seattleResult.Id, londonResult.Id);
    }

    [TestMethod]
    public void WeatherData_DeserializeWithMissingFields_HandledGracefully()
    {
        var json = """
        {
            "latitude": 47.6062,
            "longitude": -122.3321
        }
        """;

        var weatherData = JsonSerializer.Deserialize<WeatherData>(json, WeatherJsonContext.Default.WeatherData);

        Assert.IsNotNull(weatherData);
        Assert.AreEqual(47.6062, weatherData.Latitude);
        Assert.AreEqual(-122.3321, weatherData.Longitude);
        Assert.IsNull(weatherData.Timezone);
        Assert.IsNull(weatherData.Current);
    }

    [TestMethod]
    public void CurrentWeather_DeserializeWithMissingFields_HandledGracefully()
    {
        var json = """
        {
            "latitude": 47.6062,
            "longitude": -122.3321,
            "current": {
                "time": "2024-01-15T12:00",
                "temperature_2m": 10.5
            }
        }
        """;

        var weatherData = JsonSerializer.Deserialize<WeatherData>(json, WeatherJsonContext.Default.WeatherData);

        Assert.IsNotNull(weatherData);
        Assert.IsNotNull(weatherData.Current);
        Assert.AreEqual("2024-01-15T12:00", weatherData.Current.Time);
        Assert.AreEqual(10.5, weatherData.Current.Temperature);
        Assert.AreEqual(0, weatherData.Current.RelativeHumidity);
        Assert.AreEqual(0, weatherData.Current.ApparentTemperature);
        Assert.AreEqual(0, weatherData.Current.WeatherCode);
        Assert.AreEqual(0, weatherData.Current.WindSpeed);
        Assert.AreEqual(0, weatherData.Current.WindDirection);
    }

    [TestMethod]
    public void WeatherData_DeserializeInvalidJson_ThrowsJsonException()
    {
        var json = "this is not valid json {{{";

        Assert.ThrowsException<JsonException>(() =>
            JsonSerializer.Deserialize<WeatherData>(json, WeatherJsonContext.Default.WeatherData));
    }

    [TestMethod]
    public void WeatherData_DeserializeEmptyString_ThrowsJsonException()
    {
        var json = string.Empty;

        Assert.ThrowsException<JsonException>(() =>
            JsonSerializer.Deserialize<WeatherData>(json, WeatherJsonContext.Default.WeatherData));
    }

    [TestMethod]
    public void WeatherData_DeserializeCompletelyWrongStructure_HandledGracefully()
    {
        var json = """
        {
            "totally_wrong": "data",
            "not_weather": 123,
            "random_array": [1, 2, 3]
        }
        """;

        var weatherData = JsonSerializer.Deserialize<WeatherData>(json, WeatherJsonContext.Default.WeatherData);

        Assert.IsNotNull(weatherData);
        Assert.AreEqual(0, weatherData.Latitude);
        Assert.AreEqual(0, weatherData.Longitude);
        Assert.IsNull(weatherData.Timezone);
        Assert.IsNull(weatherData.Current);
    }

    [TestMethod]
    public void ForecastData_DeserializeInvalidJson_ThrowsJsonException()
    {
        var json = "invalid json structure ]]]";

        Assert.ThrowsException<JsonException>(() =>
            JsonSerializer.Deserialize<ForecastData>(json, WeatherJsonContext.Default.ForecastData));
    }
}
