// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection;
using BaldBeardedBuilder.WeatherExtension;
using Microsoft.CmdPal.Ext.Weather.Pages;
using Microsoft.CmdPal.Ext.Weather.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Microsoft.CmdPal.Ext.Weather.UnitTests;

[TestClass]
public class WeatherSettingsPageTests
{
	private static readonly BindingFlags InstanceAny =
		BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	private string _tempFilePath = string.Empty;

	[TestInitialize]
	public void Setup()
	{
		_tempFilePath = Path.Combine(Path.GetTempPath(), $"weather-settings-page-{Guid.NewGuid()}.json");
	}

	[TestCleanup]
	public void Cleanup()
	{
		if (File.Exists(_tempFilePath))
		{
			File.Delete(_tempFilePath);
		}
	}

	[TestMethod]
	public void GetContent_ReturnsSingleForm()
	{
		var page = new WeatherSettingsPage(new WeatherSettingsManager(_tempFilePath));

		var content = page.GetContent();

		Assert.AreEqual(1, content.Length);
		Assert.IsInstanceOfType<FormContent>(content[0]);
	}

	[TestMethod]
	public void SubmitForm_EmptyInputs_ReturnsKeepOpen()
	{
		var form = GetForm(new WeatherSettingsManager(_tempFilePath));

		var result = form.SubmitForm(string.Empty, "{}");

		Assert.IsNotNull(result);
		Assert.AreEqual(typeof(CommandResult).FullName, result.GetType().FullName);
	}

	[TestMethod]
	public void SubmitForm_ValidInputs_UpdatesTemperatureUnit()
	{
		var manager = new WeatherSettingsManager(_tempFilePath);
		var form = GetForm(manager);

		var inputs = """{"weather.TemperatureUnit":"fahrenheit"}""";
		form.SubmitForm(inputs, "{}");

		Assert.AreEqual("fahrenheit", manager.TemperatureUnit);
	}

	[TestMethod]
	public void Refresh_PopulatesTemplateAndStateJson()
	{
		var manager = new WeatherSettingsManager(_tempFilePath);
		var form = GetForm(manager);

		form.Refresh();

		Assert.IsFalse(string.IsNullOrWhiteSpace(form.TemplateJson));
		Assert.IsFalse(string.IsNullOrWhiteSpace(form.StateJson));
		Assert.AreEqual("{}", form.DataJson);
	}

	[TestMethod]
	public void BuildSettingsFormJson_ReturnsNonEmptyTemplate()
	{
		var manager = new WeatherSettingsManager(_tempFilePath);

		var json = manager.BuildSettingsFormJson();

		Assert.IsFalse(string.IsNullOrWhiteSpace(json));
		Assert.AreNotEqual("{}", json);
	}

	[TestMethod]
	public void SettingsPage_Refresh_RaisesItemsChangedWithoutThrowing()
	{
		var page = new WeatherSettingsPage(new WeatherSettingsManager(_tempFilePath));

		page.Refresh();
	}

	private static WeatherSettingsForm GetForm(WeatherSettingsManager manager)
	{
		var page = new WeatherSettingsPage(manager);
		return (WeatherSettingsForm)page.GetContent()[0];
	}
}
