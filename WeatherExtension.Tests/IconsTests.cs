// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace Microsoft.CmdPal.Ext.Weather.UnitTests;

[TestClass]
public class IconsTests
{
    // GetWeatherDescription used to return one string per WMO code (29 distinct
    // strings). That made every supported language ship 29 translations and
    // produced low-value variations like "Slight rain" / "Moderate rain" /
    // "Heavy rain" — the icon already conveys intensity. The descriptions are
    // now grouped into 11 broad categories. These tests pin every WMO code in
    // the open-meteo schema to the category we expect.
    //
    // Tests run with the invariant culture so we assert against the neutral
    // (English) resx without having to ship a fixed culture in test setup.

    private CultureInfo? _originalUiCulture;

    [TestInitialize]
    public void Setup()
    {
        _originalUiCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (_originalUiCulture != null)
        {
            CultureInfo.CurrentUICulture = _originalUiCulture;
        }
    }

    [DataTestMethod]
    [DataRow(0, "Clear sky")]
    [DataRow(1, "Mainly clear")]
    [DataRow(2, "Partly cloudy")]
    [DataRow(3, "Overcast")]
    [DataRow(45, "Fog")]
    [DataRow(48, "Fog")]
    [DataRow(51, "Drizzle")]
    [DataRow(53, "Drizzle")]
    [DataRow(55, "Drizzle")]
    [DataRow(56, "Drizzle")]
    [DataRow(57, "Drizzle")]
    [DataRow(61, "Rain")]
    [DataRow(63, "Rain")]
    [DataRow(65, "Rain")]
    [DataRow(66, "Rain")]
    [DataRow(67, "Rain")]
    [DataRow(71, "Snow")]
    [DataRow(73, "Snow")]
    [DataRow(75, "Snow")]
    [DataRow(77, "Snow")]
    [DataRow(80, "Rain showers")]
    [DataRow(81, "Rain showers")]
    [DataRow(82, "Rain showers")]
    [DataRow(85, "Snow showers")]
    [DataRow(86, "Snow showers")]
    [DataRow(95, "Thunderstorm")]
    [DataRow(96, "Thunderstorm")]
    [DataRow(99, "Thunderstorm")]
    public void GetWeatherDescription_KnownCode_ReturnsExpectedCategory(int weatherCode, string expected)
    {
        Assert.AreEqual(expected, Icons.GetWeatherDescription(weatherCode));
    }

    [DataTestMethod]
    [DataRow(999)]
    [DataRow(-1)]
    [DataRow(int.MaxValue)]
    public void GetWeatherDescription_UnknownCode_ReturnsUnknown(int weatherCode)
    {
        Assert.AreEqual("Unknown", Icons.GetWeatherDescription(weatherCode));
    }

    [TestMethod]
    public void GetIconForWeatherCode_ClearSky_ReturnsClearSkyIcon()
    {
        var result = Icons.GetIconForWeatherCode(0);
        Assert.IsNotNull(result);
        Assert.AreEqual(Icons.ClearSky, result);
    }

    [TestMethod]
    public void GetIconForWeatherCode_MainlyClear_GroupsCodes1And2()
    {
        Assert.AreEqual(Icons.MainlyClear, Icons.GetIconForWeatherCode(1));
        Assert.AreEqual(Icons.MainlyClear, Icons.GetIconForWeatherCode(2));
    }

    [TestMethod]
    public void GetIconForWeatherCode_PartlyCloudy_ReturnsPartlyCloudyIcon()
    {
        Assert.AreEqual(Icons.PartlyCloudy, Icons.GetIconForWeatherCode(3));
    }

    [TestMethod]
    public void GetIconForWeatherCode_Fog_GroupsCodes45And48()
    {
        Assert.AreEqual(Icons.Fog, Icons.GetIconForWeatherCode(45));
        Assert.AreEqual(Icons.Fog, Icons.GetIconForWeatherCode(48));
    }

    [TestMethod]
    public void GetIconForWeatherCode_Drizzle_GroupsCodes51To55()
    {
        Assert.AreEqual(Icons.Drizzle, Icons.GetIconForWeatherCode(51));
        Assert.AreEqual(Icons.Drizzle, Icons.GetIconForWeatherCode(53));
        Assert.AreEqual(Icons.Drizzle, Icons.GetIconForWeatherCode(55));
    }

    [TestMethod]
    public void GetIconForWeatherCode_FreezingDrizzle_GroupsCodes56And57()
    {
        Assert.AreEqual(Icons.DrizzleFreezing, Icons.GetIconForWeatherCode(56));
        Assert.AreEqual(Icons.DrizzleFreezing, Icons.GetIconForWeatherCode(57));
    }

    [TestMethod]
    public void GetIconForWeatherCode_Rain_GroupsCodes61To65()
    {
        Assert.AreEqual(Icons.Rain, Icons.GetIconForWeatherCode(61));
        Assert.AreEqual(Icons.Rain, Icons.GetIconForWeatherCode(63));
        Assert.AreEqual(Icons.Rain, Icons.GetIconForWeatherCode(65));
    }

    [TestMethod]
    public void GetIconForWeatherCode_FreezingRain_GroupsCodes66And67()
    {
        Assert.AreEqual(Icons.RainFreezing, Icons.GetIconForWeatherCode(66));
        Assert.AreEqual(Icons.RainFreezing, Icons.GetIconForWeatherCode(67));
    }

    [TestMethod]
    public void GetIconForWeatherCode_Snow_GroupsCodes71To77()
    {
        Assert.AreEqual(Icons.Snow, Icons.GetIconForWeatherCode(71));
        Assert.AreEqual(Icons.Snow, Icons.GetIconForWeatherCode(73));
        Assert.AreEqual(Icons.Snow, Icons.GetIconForWeatherCode(75));
        Assert.AreEqual(Icons.Snow, Icons.GetIconForWeatherCode(77));
    }

    [TestMethod]
    public void GetIconForWeatherCode_RainShowers_GroupsCodes80To82()
    {
        Assert.AreEqual(Icons.RainShowers, Icons.GetIconForWeatherCode(80));
        Assert.AreEqual(Icons.RainShowers, Icons.GetIconForWeatherCode(81));
        Assert.AreEqual(Icons.RainShowers, Icons.GetIconForWeatherCode(82));
    }

    [TestMethod]
    public void GetIconForWeatherCode_SnowShowers_GroupsCodes85And86()
    {
        Assert.AreEqual(Icons.SnowShowers, Icons.GetIconForWeatherCode(85));
        Assert.AreEqual(Icons.SnowShowers, Icons.GetIconForWeatherCode(86));
    }

    [TestMethod]
    public void GetIconForWeatherCode_Thunderstorm_ReturnsThunderstormIcon()
    {
        Assert.AreEqual(Icons.Thunderstorm, Icons.GetIconForWeatherCode(95));
    }

    [TestMethod]
    public void GetIconForWeatherCode_ThunderstormHail_GroupsCodes96And99()
    {
        Assert.AreEqual(Icons.ThunderstormHail, Icons.GetIconForWeatherCode(96));
        Assert.AreEqual(Icons.ThunderstormHail, Icons.GetIconForWeatherCode(99));
    }

    [TestMethod]
    public void GetIconForWeatherCode_UnknownCode_ReturnsDefaultWeatherIcon()
    {
        Assert.AreEqual(Icons.WeatherIcon, Icons.GetIconForWeatherCode(999));
        Assert.AreEqual(Icons.WeatherIcon, Icons.GetIconForWeatherCode(-1));
    }
}
