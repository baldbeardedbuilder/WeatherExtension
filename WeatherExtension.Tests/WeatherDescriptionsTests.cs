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
	public void GetLocalized_TurkishCulture_ReturnsTranslatedThunderstorm()
	{
		var original = CultureInfo.CurrentUICulture;
		try
		{
			CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
			Assert.AreEqual("Gök gürültülü", WeatherDescriptions.GetLocalized(95));
			Assert.AreEqual("Kapalı", WeatherDescriptions.GetLocalized(3));
			Assert.AreEqual("Sağanak", WeatherDescriptions.GetLocalized(80));
		}
		finally
		{
			CultureInfo.CurrentUICulture = original;
		}
	}

	[TestMethod]
	public void GetLocalized_EnglishCulture_FallsBackToNeutral()
	{
		var original = CultureInfo.CurrentUICulture;
		try
		{
			CultureInfo.CurrentUICulture = new CultureInfo("en-US");
			Assert.AreEqual("Thunderstorm", WeatherDescriptions.GetLocalized(95));
			Assert.AreEqual("Overcast", WeatherDescriptions.GetLocalized(3));
		}
		finally
		{
			CultureInfo.CurrentUICulture = original;
		}
	}

	[TestMethod]
	public void GetLocalized_UnknownCode_ReturnsLocalizedUnknown()
	{
		var original = CultureInfo.CurrentUICulture;
		try
		{
			CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
			Assert.AreEqual("Bilinmiyor", WeatherDescriptions.GetLocalized(12345));
		}
		finally
		{
			CultureInfo.CurrentUICulture = original;
		}
	}
}
