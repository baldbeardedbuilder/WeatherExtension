// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CmdPal.Ext.Weather.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Microsoft.CmdPal.Ext.Weather.UnitTests;

[TestClass]
public class DefaultLocationMigrationTests
{
	private string? _tempFile;

	[TestCleanup]
	public void Cleanup()
	{
		if (_tempFile != null && File.Exists(_tempFile))
		{
			File.Delete(_tempFile);
		}
	}

	[TestMethod]
	public async Task MigrateDefaultLocationAsync_WithCustomLocation_ReturnsValueAndRemovesKey()
	{
		_tempFile = Path.GetTempFileName();
		await File.WriteAllTextAsync(_tempFile,
			"""{"weather.DefaultLocation": "Seattle, WA", "weather.TemperatureUnit": "celsius"}""");

		var result = await WeatherSettingsManager.MigrateDefaultLocationAsync(_tempFile);

		Assert.AreEqual("Seattle, WA", result, "Expected the custom location to be returned");

		var json = await File.ReadAllTextAsync(_tempFile);
		var obj = JsonNode.Parse(json)?.AsObject();
		Assert.IsNotNull(obj);
		Assert.IsFalse(obj.ContainsKey("weather.DefaultLocation"), "Key should have been removed after migration");
		Assert.IsTrue(obj.ContainsKey("weather.TemperatureUnit"), "Unrelated keys should be preserved");
	}

	[TestMethod]
	public async Task MigrateDefaultLocationAsync_WithDefaultValue_ReturnsNull()
	{
		_tempFile = Path.GetTempFileName();
		await File.WriteAllTextAsync(_tempFile,
			"""{"weather.DefaultLocation": "98101"}""");

		var result = await WeatherSettingsManager.MigrateDefaultLocationAsync(_tempFile);

		Assert.IsNull(result, "Default value '98101' should not trigger migration");

		var json = await File.ReadAllTextAsync(_tempFile);
		var obj = JsonNode.Parse(json)?.AsObject();
		Assert.IsNotNull(obj);
		Assert.IsTrue(obj.ContainsKey("weather.DefaultLocation"), "Key should be unchanged when value is the default");
	}

	[TestMethod]
	public async Task MigrateDefaultLocationAsync_NoKey_ReturnsNull()
	{
		_tempFile = Path.GetTempFileName();
		await File.WriteAllTextAsync(_tempFile,
			"""{"weather.TemperatureUnit": "fahrenheit"}""");

		var result = await WeatherSettingsManager.MigrateDefaultLocationAsync(_tempFile);

		Assert.IsNull(result, "Missing key should return null");
	}

	[TestMethod]
	public async Task MigrateDefaultLocationAsync_FileNotFound_ReturnsNull()
	{
		var nonExistentPath = Path.Combine(Path.GetTempPath(), "does-not-exist-" + Path.GetRandomFileName() + ".json");

		var result = await WeatherSettingsManager.MigrateDefaultLocationAsync(nonExistentPath);

		Assert.IsNull(result, "Non-existent file should return null without throwing");
	}

	[TestMethod]
	public async Task MigrateDefaultLocationAsync_Idempotent_SecondCallReturnsNull()
	{
		_tempFile = Path.GetTempFileName();
		await File.WriteAllTextAsync(_tempFile,
			"""{"weather.DefaultLocation": "Paris, France"}""");

		var firstResult = await WeatherSettingsManager.MigrateDefaultLocationAsync(_tempFile);
		var secondResult = await WeatherSettingsManager.MigrateDefaultLocationAsync(_tempFile);

		Assert.AreEqual("Paris, France", firstResult, "First call should return the location");
		Assert.IsNull(secondResult, "Second call should return null - key was removed on first run");
	}
}
