// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace Microsoft.CmdPal.Ext.Weather.UnitTests;

[TestClass]
public class IconsTests
{
    [TestMethod]
    public void GetWeatherDescription_ClearSky_ReturnsCorrectDescription()
    {
        var result = Icons.GetWeatherDescription(0);
        Assert.AreEqual("Clear sky", result);
    }

    [TestMethod]
    public void GetWeatherDescription_MainlyClear_ReturnsCorrectDescription()
    {
        var result = Icons.GetWeatherDescription(1);
        Assert.AreEqual("Mainly clear", result);
    }

    [TestMethod]
    public void GetWeatherDescription_PartlyCloudy_ReturnsCorrectDescription()
    {
        var result = Icons.GetWeatherDescription(2);
        Assert.AreEqual("Partly cloudy", result);
    }

    [TestMethod]
    public void GetWeatherDescription_Overcast_ReturnsCorrectDescription()
    {
        var result = Icons.GetWeatherDescription(3);
        Assert.AreEqual("Overcast", result);
    }

    [TestMethod]
    public void GetWeatherDescription_Fog_ReturnsCorrectDescription()
    {
        var result = Icons.GetWeatherDescription(45);
        Assert.AreEqual("Fog", result);
    }

    [TestMethod]
    public void GetWeatherDescription_DepositingRimeFog_ReturnsCorrectDescription()
    {
        var result = Icons.GetWeatherDescription(48);
        Assert.AreEqual("Depositing rime fog", result);
    }

    [TestMethod]
    public void GetWeatherDescription_LightDrizzle_ReturnsCorrectDescription()
    {
        var result = Icons.GetWeatherDescription(51);
        Assert.AreEqual("Light drizzle", result);
    }

    [TestMethod]
    public void GetWeatherDescription_ModerateDrizzle_ReturnsCorrectDescription()
    {
        var result = Icons.GetWeatherDescription(53);
        Assert.AreEqual("Moderate drizzle", result);
    }

    [TestMethod]
    public void GetWeatherDescription_DenseDrizzle_ReturnsCorrectDescription()
    {
        var result = Icons.GetWeatherDescription(55);
        Assert.AreEqual("Dense drizzle", result);
    }

    [TestMethod]
    public void GetWeatherDescription_LightFreezingDrizzle_ReturnsCorrectDescription()
    {
        var result = Icons.GetWeatherDescription(56);
        Assert.AreEqual("Light freezing drizzle", result);
    }

    [TestMethod]
    public void GetWeatherDescription_DenseFreezingDrizzle_ReturnsCorrectDescription()
    {
        var result = Icons.GetWeatherDescription(57);
        Assert.AreEqual("Dense freezing drizzle", result);
    }

    [TestMethod]
    public void GetWeatherDescription_SlightRain_ReturnsCorrectDescription()
    {
        var result = Icons.GetWeatherDescription(61);
        Assert.AreEqual("Slight rain", result);
    }

    [TestMethod]
    public void GetWeatherDescription_ModerateRain_ReturnsCorrectDescription()
    {
        var result = Icons.GetWeatherDescription(63);
        Assert.AreEqual("Moderate rain", result);
    }

    [TestMethod]
    public void GetWeatherDescription_HeavyRain_ReturnsCorrectDescription()
    {
        var result = Icons.GetWeatherDescription(65);
        Assert.AreEqual("Heavy rain", result);
    }

    [TestMethod]
    public void GetWeatherDescription_LightFreezingRain_ReturnsCorrectDescription()
    {
        var result = Icons.GetWeatherDescription(66);
        Assert.AreEqual("Light freezing rain", result);
    }

    [TestMethod]
    public void GetWeatherDescription_HeavyFreezingRain_ReturnsCorrectDescription()
    {
        var result = Icons.GetWeatherDescription(67);
        Assert.AreEqual("Heavy freezing rain", result);
    }

    [TestMethod]
    public void GetWeatherDescription_SlightSnowFall_ReturnsCorrectDescription()
    {
        var result = Icons.GetWeatherDescription(71);
        Assert.AreEqual("Slight snow fall", result);
    }

    [TestMethod]
    public void GetWeatherDescription_ModerateSnowFall_ReturnsCorrectDescription()
    {
        var result = Icons.GetWeatherDescription(73);
        Assert.AreEqual("Moderate snow fall", result);
    }

    [TestMethod]
    public void GetWeatherDescription_HeavySnowFall_ReturnsCorrectDescription()
    {
        var result = Icons.GetWeatherDescription(75);
        Assert.AreEqual("Heavy snow fall", result);
    }

    [TestMethod]
    public void GetWeatherDescription_SnowGrains_ReturnsCorrectDescription()
    {
        var result = Icons.GetWeatherDescription(77);
        Assert.AreEqual("Snow grains", result);
    }

    [TestMethod]
    public void GetWeatherDescription_SlightRainShowers_ReturnsCorrectDescription()
    {
        var result = Icons.GetWeatherDescription(80);
        Assert.AreEqual("Slight rain showers", result);
    }

    [TestMethod]
    public void GetWeatherDescription_ModerateRainShowers_ReturnsCorrectDescription()
    {
        var result = Icons.GetWeatherDescription(81);
        Assert.AreEqual("Moderate rain showers", result);
    }

    [TestMethod]
    public void GetWeatherDescription_ViolentRainShowers_ReturnsCorrectDescription()
    {
        var result = Icons.GetWeatherDescription(82);
        Assert.AreEqual("Violent rain showers", result);
    }

    [TestMethod]
    public void GetWeatherDescription_SlightSnowShowers_ReturnsCorrectDescription()
    {
        var result = Icons.GetWeatherDescription(85);
        Assert.AreEqual("Slight snow showers", result);
    }

    [TestMethod]
    public void GetWeatherDescription_HeavySnowShowers_ReturnsCorrectDescription()
    {
        var result = Icons.GetWeatherDescription(86);
        Assert.AreEqual("Heavy snow showers", result);
    }

    [TestMethod]
    public void GetWeatherDescription_Thunderstorm_ReturnsCorrectDescription()
    {
        var result = Icons.GetWeatherDescription(95);
        Assert.AreEqual("Thunderstorm", result);
    }

    [TestMethod]
    public void GetWeatherDescription_ThunderstormWithSlightHail_ReturnsCorrectDescription()
    {
        var result = Icons.GetWeatherDescription(96);
        Assert.AreEqual("Thunderstorm with slight hail", result);
    }

    [TestMethod]
    public void GetWeatherDescription_ThunderstormWithHeavyHail_ReturnsCorrectDescription()
    {
        var result = Icons.GetWeatherDescription(99);
        Assert.AreEqual("Thunderstorm with heavy hail", result);
    }

    [TestMethod]
    public void GetWeatherDescription_UnknownCode_ReturnsUnknown()
    {
        var result = Icons.GetWeatherDescription(999);
        Assert.AreEqual("Unknown", result);
    }

    [TestMethod]
    public void GetWeatherDescription_NegativeCode_ReturnsUnknown()
    {
        var result = Icons.GetWeatherDescription(-1);
        Assert.AreEqual("Unknown", result);
    }

    [TestMethod]
    public void GetIconForWeatherCode_ClearSky_ReturnsNotNull()
    {
        var result = Icons.GetIconForWeatherCode(0);
        Assert.IsNotNull(result);
        Assert.AreEqual(Icons.ClearSky, result);
    }

    [TestMethod]
    public void GetIconForWeatherCode_MainlyClear_ReturnsNotNull()
    {
        var result1 = Icons.GetIconForWeatherCode(1);
        var result2 = Icons.GetIconForWeatherCode(2);
        Assert.IsNotNull(result1);
        Assert.IsNotNull(result2);
        Assert.AreEqual(Icons.MainlyClear, result1);
        Assert.AreEqual(Icons.MainlyClear, result2);
    }

    [TestMethod]
    public void GetIconForWeatherCode_PartlyCloudy_ReturnsNotNull()
    {
        var result = Icons.GetIconForWeatherCode(3);
        Assert.IsNotNull(result);
        Assert.AreEqual(Icons.PartlyCloudy, result);
    }

    [TestMethod]
    public void GetIconForWeatherCode_Fog_ReturnsNotNull()
    {
        var result45 = Icons.GetIconForWeatherCode(45);
        var result48 = Icons.GetIconForWeatherCode(48);
        Assert.IsNotNull(result45);
        Assert.IsNotNull(result48);
        Assert.AreEqual(Icons.Fog, result45);
        Assert.AreEqual(Icons.Fog, result48);
    }

    [TestMethod]
    public void GetIconForWeatherCode_Drizzle_ReturnsNotNull()
    {
        var result51 = Icons.GetIconForWeatherCode(51);
        var result53 = Icons.GetIconForWeatherCode(53);
        var result55 = Icons.GetIconForWeatherCode(55);
        Assert.IsNotNull(result51);
        Assert.IsNotNull(result53);
        Assert.IsNotNull(result55);
        Assert.AreEqual(Icons.Drizzle, result51);
    }

    [TestMethod]
    public void GetIconForWeatherCode_FreezingDrizzle_ReturnsNotNull()
    {
        var result56 = Icons.GetIconForWeatherCode(56);
        var result57 = Icons.GetIconForWeatherCode(57);
        Assert.IsNotNull(result56);
        Assert.IsNotNull(result57);
        Assert.AreEqual(Icons.DrizzleFreezing, result56);
    }

    [TestMethod]
    public void GetIconForWeatherCode_Rain_ReturnsNotNull()
    {
        var result61 = Icons.GetIconForWeatherCode(61);
        var result63 = Icons.GetIconForWeatherCode(63);
        var result65 = Icons.GetIconForWeatherCode(65);
        Assert.IsNotNull(result61);
        Assert.IsNotNull(result63);
        Assert.IsNotNull(result65);
        Assert.AreEqual(Icons.Rain, result61);
    }

    [TestMethod]
    public void GetIconForWeatherCode_FreezingRain_ReturnsNotNull()
    {
        var result66 = Icons.GetIconForWeatherCode(66);
        var result67 = Icons.GetIconForWeatherCode(67);
        Assert.IsNotNull(result66);
        Assert.IsNotNull(result67);
        Assert.AreEqual(Icons.RainFreezing, result66);
    }

    [TestMethod]
    public void GetIconForWeatherCode_Snow_ReturnsNotNull()
    {
        var result71 = Icons.GetIconForWeatherCode(71);
        var result73 = Icons.GetIconForWeatherCode(73);
        var result75 = Icons.GetIconForWeatherCode(75);
        var result77 = Icons.GetIconForWeatherCode(77);
        Assert.IsNotNull(result71);
        Assert.IsNotNull(result73);
        Assert.IsNotNull(result75);
        Assert.IsNotNull(result77);
        Assert.AreEqual(Icons.Snow, result71);
    }

    [TestMethod]
    public void GetIconForWeatherCode_RainShowers_ReturnsNotNull()
    {
        var result80 = Icons.GetIconForWeatherCode(80);
        var result81 = Icons.GetIconForWeatherCode(81);
        var result82 = Icons.GetIconForWeatherCode(82);
        Assert.IsNotNull(result80);
        Assert.IsNotNull(result81);
        Assert.IsNotNull(result82);
        Assert.AreEqual(Icons.RainShowers, result80);
    }

    [TestMethod]
    public void GetIconForWeatherCode_SnowShowers_ReturnsNotNull()
    {
        var result85 = Icons.GetIconForWeatherCode(85);
        var result86 = Icons.GetIconForWeatherCode(86);
        Assert.IsNotNull(result85);
        Assert.IsNotNull(result86);
        Assert.AreEqual(Icons.SnowShowers, result85);
    }

    [TestMethod]
    public void GetIconForWeatherCode_Thunderstorm_ReturnsNotNull()
    {
        var result = Icons.GetIconForWeatherCode(95);
        Assert.IsNotNull(result);
        Assert.AreEqual(Icons.Thunderstorm, result);
    }

    [TestMethod]
    public void GetIconForWeatherCode_ThunderstormHail_ReturnsNotNull()
    {
        var result96 = Icons.GetIconForWeatherCode(96);
        var result99 = Icons.GetIconForWeatherCode(99);
        Assert.IsNotNull(result96);
        Assert.IsNotNull(result99);
        Assert.AreEqual(Icons.ThunderstormHail, result96);
        Assert.AreEqual(Icons.ThunderstormHail, result99);
    }

    [TestMethod]
    public void GetIconForWeatherCode_UnknownCode_ReturnsDefaultWeatherIcon()
    {
        var result = Icons.GetIconForWeatherCode(999);
        Assert.IsNotNull(result);
        Assert.AreEqual(Icons.WeatherIcon, result);
    }

    [TestMethod]
    public void GetIconForWeatherCode_NegativeCode_ReturnsDefaultWeatherIcon()
    {
        var result = Icons.GetIconForWeatherCode(-1);
        Assert.IsNotNull(result);
        Assert.AreEqual(Icons.WeatherIcon, result);
    }
}
