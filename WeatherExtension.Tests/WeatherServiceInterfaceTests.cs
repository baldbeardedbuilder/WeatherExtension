// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.CmdPal.Ext.Weather.Models;
using Microsoft.CmdPal.Ext.Weather.Services;

namespace Microsoft.CmdPal.Ext.Weather.UnitTests;

[TestClass]
public class WeatherServiceInterfaceTests
{
    [TestMethod]
    public void IWeatherService_GetHourlyForecastAsyncMethod_Exists()
    {
        var interfaceType = typeof(IWeatherService);
        var method = interfaceType.GetMethod("GetHourlyForecastAsync");

        Assert.IsNotNull(method, "GetHourlyForecastAsync method should exist on IWeatherService");
        Assert.AreEqual(typeof(Task<HourlyForecastData?>), method.ReturnType, "Method should return Task<HourlyForecastData?>");

        var parameters = method.GetParameters();
        Assert.AreEqual(5, parameters.Length, "Method should have 5 parameters");
        Assert.AreEqual("latitude", parameters[0].Name);
        Assert.AreEqual(typeof(double), parameters[0].ParameterType);
        Assert.AreEqual("longitude", parameters[1].Name);
        Assert.AreEqual(typeof(double), parameters[1].ParameterType);
        Assert.AreEqual("temperatureUnit", parameters[2].Name);
        Assert.AreEqual(typeof(string), parameters[2].ParameterType);
        Assert.AreEqual("windSpeedUnit", parameters[3].Name);
        Assert.AreEqual(typeof(string), parameters[3].ParameterType);
        Assert.AreEqual("ct", parameters[4].Name);
        Assert.AreEqual(typeof(CancellationToken), parameters[4].ParameterType);
    }
}
