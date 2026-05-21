// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Microsoft.CmdPal.Ext.Weather.Services;

namespace Microsoft.CmdPal.Ext.Weather.UnitTests;

[TestClass]
public class WeatherFormatterTests
{
	[DataTestMethod]
	[DataRow("celsius", WeatherFormatter.CelsiusUnit)]
	[DataRow("CELSIUS", WeatherFormatter.CelsiusUnit)]
	[DataRow("fahrenheit", WeatherFormatter.FahrenheitUnit)]
	[DataRow("", WeatherFormatter.FahrenheitUnit)]
	[DataRow("kelvin", WeatherFormatter.FahrenheitUnit)]
	public void TemperatureUnit_ReturnsExpectedSuffix(string unit, string expected)
	{
		Assert.AreEqual(expected, WeatherFormatter.TemperatureUnit(unit));
	}

	[DataTestMethod]
	[DataRow("kmh", "km/h")]
	[DataRow("KMH", "km/h")]
	[DataRow("mph", "mph")]
	[DataRow("", "km/h")]
	[DataRow("knots", "km/h")]
	public void WindSpeedUnit_ReturnsExpectedLabel(string unit, string expected)
	{
		Assert.AreEqual(expected, WeatherFormatter.WindSpeedUnit(unit));
	}

	[TestMethod]
	public void Temperature_Celsius_UsesCurrentCultureNumberFormat()
	{
		var original = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = new CultureInfo("en-US");
			Assert.AreEqual("21°C", WeatherFormatter.Temperature(21, WeatherFormatter.CelsiusKey));
		}
		finally
		{
			CultureInfo.CurrentCulture = original;
		}
	}

	[TestMethod]
	public void HighLow_FormatsBothValuesWithUnit()
	{
		var original = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = new CultureInfo("en-US");
			Assert.AreEqual("30°F / 18°F", WeatherFormatter.HighLow(30, 18, "fahrenheit"));
		}
		finally
		{
			CultureInfo.CurrentCulture = original;
		}
	}

	[DataTestMethod]
	[DataRow(0, "N")]
	[DataRow(360, "N")]
	[DataRow(45, "NE")]
	[DataRow(90, "E")]
	[DataRow(135, "SE")]
	[DataRow(180, "S")]
	[DataRow(225, "SW")]
	[DataRow(270, "W")]
	[DataRow(315, "NW")]
	[DataRow(337, "NW")]
	[DataRow(338, "N")]
	[DataRow(22, "N")]
	[DataRow(23, "NE")]
	public void CompassDirection_MapsDegreesToEightPointRose(double degrees, string expected)
	{
		Assert.AreEqual(expected, WeatherFormatter.CompassDirection((int)degrees));
	}

	[TestMethod]
	public void Hour_Use24HourClock_UsesHourAndMinutePattern()
	{
		var original = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = new CultureInfo("en-US");
			var time = new DateTime(2024, 6, 15, 13, 30, 0);
			Assert.AreEqual("13:30", WeatherFormatter.Hour(time, use24Hour: true));
		}
		finally
		{
			CultureInfo.CurrentCulture = original;
		}
	}

	[TestMethod]
	public void Hour_Use12HourClock_UsesHourAndMeridiemPattern()
	{
		var original = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = new CultureInfo("en-US");
			var time = new DateTime(2024, 6, 15, 13, 30, 0);
			Assert.AreEqual("1 PM", WeatherFormatter.Hour(time, use24Hour: false));
		}
		finally
		{
			CultureInfo.CurrentCulture = original;
		}
	}

	[TestMethod]
	public void Humidity_WithValue_AppendsPercentSign()
	{
		var original = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = new CultureInfo("en-US");
			Assert.AreEqual("72%", WeatherFormatter.Humidity(72));
		}
		finally
		{
			CultureInfo.CurrentCulture = original;
		}
	}

	[TestMethod]
	public void Humidity_WithoutValue_ReturnsPlaceholder()
	{
		Assert.AreEqual("--", WeatherFormatter.Humidity(null));
	}
}
