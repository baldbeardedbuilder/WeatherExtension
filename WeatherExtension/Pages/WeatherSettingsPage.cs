// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BaldBeardedBuilder.WeatherExtension;
using Microsoft.CmdPal.Ext.Weather.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Microsoft.CmdPal.Ext.Weather.Pages;

/// <summary>
/// A custom settings page that keeps the user inside the form on save.
/// The toolkit's built-in <see cref="Settings.SettingsPage"/> calls
/// <c>CommandResult.GoHome()</c> after every submit, which bounces the
/// user out of the settings sheet and back to the root command palette.
/// That makes adjusting more than one option a frustrating round trip,
/// so we own the page ourselves and return <c>KeepOpen()</c> instead.
/// </summary>
internal sealed partial class WeatherSettingsPage : ContentPage
{
	private readonly WeatherSettingsManager _settingsManager;
	private readonly WeatherSettingsForm _form;

	public WeatherSettingsPage(WeatherSettingsManager settingsManager)
	{
		ArgumentNullException.ThrowIfNull(settingsManager);

		_settingsManager = settingsManager;

		Name = Resources.settings_page_title;
		Icon = new IconInfo("\uE713");
		Title = Resources.plugin_name;
		Id = "com.baldbeardedbuilder.cmdpal.weather.settings";

		_form = new WeatherSettingsForm(_settingsManager);
	}

	public override IContent[] GetContent() => [_form];

	/// <summary>
	/// Forces the embedded form to re-render its template using the latest
	/// settings values. Useful when something outside the form (e.g. a
	/// pinned location being added) changes a dropdown's choice list.
	/// </summary>
	public void Refresh()
	{
		_form.Refresh();
		RaiseItemsChanged();
	}
}

internal sealed partial class WeatherSettingsForm : FormContent
{
	private readonly WeatherSettingsManager _settingsManager;

	public WeatherSettingsForm(WeatherSettingsManager settingsManager)
	{
		_settingsManager = settingsManager;
		Refresh();
	}

	public override ICommandResult SubmitForm(string inputs, string data)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(inputs))
			{
				return CommandResult.KeepOpen();
			}

			_settingsManager.Settings.Update(inputs);
			_settingsManager.RaiseSettingsChanged();

			// Re-render with the latest values so dropdowns and toggles
			// reflect what was just saved.
			Refresh();
		}
		catch (Exception ex)
		{
			WeatherLogger.LogToHost(
				MessageState.Error,
				$"Settings save failed: {ex.Message}");
		}

		return CommandResult.KeepOpen();
	}

	public void Refresh()
	{
		TemplateJson = _settingsManager.BuildSettingsFormJson();
		StateJson = _settingsManager.Settings.ToJson();
		DataJson = "{}";
	}
}
