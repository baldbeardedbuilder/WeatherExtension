// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Microsoft.CmdPal.Ext.Weather.Services;

namespace Microsoft.CmdPal.Ext.Weather.UnitTests;

[TestClass]
public class WeatherDescriptionsTests
{
	[TestMethod]
	public void GetLocalized_InvariantCulture_ReturnsNeutralCategoryStrings()
	{
		var original = CultureInfo.CurrentUICulture;
		try
		{
			CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
			Assert.AreEqual("Thunderstorm", WeatherDescriptions.GetLocalized(95));
			Assert.AreEqual("Overcast", WeatherDescriptions.GetLocalized(3));
			Assert.AreEqual("Rain showers", WeatherDescriptions.GetLocalized(80));
			Assert.AreEqual("Snow showers", WeatherDescriptions.GetLocalized(85));
		}
		finally
		{
			CultureInfo.CurrentUICulture = original;
		}
	}

	[TestMethod]
	public void GetLocalized_UnknownCode_ReturnsUnknown()
	{
		var original = CultureInfo.CurrentUICulture;
		try
		{
			CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
			Assert.AreEqual("Unknown", WeatherDescriptions.GetLocalized(12345));
		}
		finally
		{
			CultureInfo.CurrentUICulture = original;
		}
	}
}
