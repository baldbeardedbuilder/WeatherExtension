// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CmdPal.Ext.Weather.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CmdPal.Ext.Weather.UnitTests;

[TestClass]
public class ConnectivityProbeTests
{
	[TestMethod]
	public async Task GetCurrentWeatherAsync_ApiReturns500_TriggersProbe()
	{
		var callCount = 0;
		var handler = new CountingHttpHandler(request =>
		{
			callCount++;
			return new HttpResponseMessage(HttpStatusCode.InternalServerError);
		});

		using var service = new OpenMeteoService(handler);
		var result = await service.GetCurrentWeatherAsync(52.52, 13.41, ct: CancellationToken.None);

		Assert.IsNull(result);
		// 1 API call + 1 connectivity probe = 2
		Assert.AreEqual(2, callCount, "Expected API call + connectivity probe");
	}

	[TestMethod]
	public async Task GetCurrentWeatherAsync_ApiThrows_TriggersProbe()
	{
		var callCount = 0;
		var handler = new CountingHttpHandler(request =>
		{
			callCount++;
			throw new HttpRequestException("Connection refused");
		});

		using var service = new OpenMeteoService(handler);
		var result = await service.GetCurrentWeatherAsync(52.52, 13.41, ct: CancellationToken.None);

		Assert.IsNull(result);
		// 1 failed API call + 1 probe attempt (also fails since same handler throws) = 2
		Assert.AreEqual(2, callCount, "Expected API call + connectivity probe attempt");
	}

	[TestMethod]
	public async Task GetForecastAsync_ApiReturns500_TriggersProbe()
	{
		var callCount = 0;
		var handler = new CountingHttpHandler(request =>
		{
			callCount++;
			return new HttpResponseMessage(HttpStatusCode.InternalServerError);
		});

		using var service = new OpenMeteoService(handler);
		var result = await service.GetForecastAsync(52.52, 13.41, ct: CancellationToken.None);

		Assert.IsNull(result);
		Assert.AreEqual(2, callCount, "Expected API call + connectivity probe");
	}

	[TestMethod]
	public async Task GetHourlyForecastAsync_ApiReturns500_TriggersProbe()
	{
		var callCount = 0;
		var handler = new CountingHttpHandler(request =>
		{
			callCount++;
			return new HttpResponseMessage(HttpStatusCode.InternalServerError);
		});

		using var service = new OpenMeteoService(handler);
		var result = await service.GetHourlyForecastAsync(52.52, 13.41, ct: CancellationToken.None);

		Assert.IsNull(result);
		Assert.AreEqual(2, callCount, "Expected API call + connectivity probe");
	}

	[TestMethod]
	public async Task GetCurrentWeatherAsync_ApiSucceeds_NoProbe()
	{
		var callCount = 0;
		var handler = new CountingHttpHandler(request =>
		{
			callCount++;
			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("{}"),
			};
		});

		using var service = new OpenMeteoService(handler);
		_ = await service.GetCurrentWeatherAsync(52.52, 13.41, ct: CancellationToken.None);

		// Only the API call, no probe
		Assert.AreEqual(1, callCount, "Successful API call should not trigger probe");
	}

	[TestMethod]
	public async Task GetCurrentWeatherAsync_Cancelled_NoProbe()
	{
		var callCount = 0;
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		var handler = new CountingHttpHandler(request =>
		{
			callCount++;
			throw new OperationCanceledException();
		});

		using var service = new OpenMeteoService(handler);

		// HttpClient wraps OperationCanceledException as TaskCanceledException (a subtype).
		// Use try/catch to accept any OperationCanceledException subtype.
		Exception? thrown = null;
		try
		{
			await service.GetCurrentWeatherAsync(52.52, 13.41, ct: cts.Token);
			Assert.Fail("Expected OperationCanceledException to be thrown");
		}
		catch (OperationCanceledException ex)
		{
			thrown = ex;
		}

		Assert.IsNotNull(thrown, "Expected OperationCanceledException (or subtype)");
		// Cancelled — probe should not fire
		Assert.AreEqual(0, callCount, "Cancelled request should not trigger probe");
	}
}

