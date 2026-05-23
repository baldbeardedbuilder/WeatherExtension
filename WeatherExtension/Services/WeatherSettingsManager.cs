// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BaldBeardedBuilder.WeatherExtension;
using Microsoft.CmdPal.Ext.Weather.Models;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Microsoft.CmdPal.Ext.Weather.Services;

public sealed class WeatherSettingsManager : JsonSettingsManager
{
    private const string Namespace = "weather";

    private static string Namespaced(string propertyName) => $"{Namespace}.{propertyName}";

    private readonly ChoiceSetSetting _temperatureUnit = new(
        Namespaced(nameof(TemperatureUnit)),
        Resources.temperature_unit_title,
        Resources.temperature_unit_description,
        [
            new ChoiceSetSetting.Choice(Resources.celsius, "celsius"),
            new ChoiceSetSetting.Choice(Resources.fahrenheit, "fahrenheit"),
        ]);

    private readonly ChoiceSetSetting _windSpeedUnit = new(
        Namespaced(nameof(WindSpeedUnit)),
        Resources.wind_speed_unit_title,
        Resources.wind_speed_unit_description,
        [
            new ChoiceSetSetting.Choice(Resources.kmh, "kmh"),
            new ChoiceSetSetting.Choice(Resources.mph, "mph"),
        ]);

    private readonly ToggleSetting _showForecast = new(
        Namespaced(nameof(ShowForecast)),
        Resources.show_forecast_title,
        Resources.show_forecast_description,
        true);

    private readonly ChoiceSetSetting _updateInterval = new(
        Namespaced(nameof(UpdateIntervalMinutes)),
        Resources.update_interval_title,
        Resources.update_interval_description,
        [
            new ChoiceSetSetting.Choice(Resources.one_hour, "60"),
            new ChoiceSetSetting.Choice(Resources.three_hours, "180"),
            new ChoiceSetSetting.Choice(Resources.six_hours, "360"),
            new ChoiceSetSetting.Choice(Resources.twelve_hours, "720"),
        ]);

    private readonly ChoiceSetSetting _dockBandSubtitle = new(
        Namespaced(nameof(DockBandSubtitle)),
        Resources.dock_band_subtitle_title,
        Resources.dock_band_subtitle_description,
        [
            new ChoiceSetSetting.Choice(Resources.dock_band_subtitle_highlow, "highlow"),
            new ChoiceSetSetting.Choice(Resources.dock_band_subtitle_location, "location"),
        ]);

    private readonly ChoiceSetSetting _hourFormat = new(
        Namespaced(nameof(HourFormat)),
        Resources.hour_format_title,
        Resources.hour_format_description,
        [
            new ChoiceSetSetting.Choice(Resources.hour_format_12, "12"),
            new ChoiceSetSetting.Choice(Resources.hour_format_24, "24"),
        ]);

    private readonly ChoiceSetSetting _defaultLocation = new(
        Namespaced(nameof(DefaultLocationKey)),
        Resources.default_location_title,
        Resources.default_location_description,
        [
            new ChoiceSetSetting.Choice(Resources.default_location_auto, "auto"),
        ]);

    private static readonly HashSet<string> _validIntervals = ["60", "180", "360", "720"];

    public string TemperatureUnit => _temperatureUnit.Value ?? "celsius";

    public string WindSpeedUnit => _windSpeedUnit.Value ?? "kmh";

    public bool ShowForecast => _showForecast.Value;

    public int UpdateIntervalMinutes => _updateInterval.Value is string v && _validIntervals.Contains(v) ? int.Parse(v, CultureInfo.InvariantCulture) : 60;

    public string DockBandSubtitle => _dockBandSubtitle.Value ?? "highlow";

    /// <summary>
    /// Returns the user-selected clock display: "12" or "24". Defaults to
    /// "12" because that matches the previous hardcoded behavior.
    /// </summary>
    public string HourFormat => _hourFormat.Value ?? "12";

    public bool Use24HourClock => HourFormat == "24";

    public string DefaultLocationKey => _defaultLocation.Value ?? "auto";

    public WeatherSettingsManager()
    {
        FilePath = SettingsJsonPath();
        AddAll();

        LoadSettings();
        MigrateUpdateInterval();

        Settings.SettingsChanged += (_, _) => SaveSettings();
    }

    internal WeatherSettingsManager(string filePath)
    {
        FilePath = filePath;
        AddAll();

        LoadSettings();
        MigrateUpdateInterval();

        Settings.SettingsChanged += (_, _) => SaveSettings();
    }

    private void AddAll()
    {
        Settings.Add(_temperatureUnit);
        Settings.Add(_windSpeedUnit);
        Settings.Add(_showForecast);
        Settings.Add(_updateInterval);
        Settings.Add(_dockBandSubtitle);
        Settings.Add(_hourFormat);
        Settings.Add(_defaultLocation);
    }

    private void MigrateUpdateInterval()
    {
        var current = _updateInterval.Value;
        if (current != null && !_validIntervals.Contains(current))
        {
            _updateInterval.Value = "60";
            SaveSettings();
        }
    }

    public void RefreshDefaultLocationChoices(IReadOnlyList<PinnedLocation> favorites)
    {
        var choices = new List<ChoiceSetSetting.Choice>
        {
            new ChoiceSetSetting.Choice(Resources.default_location_auto, "auto")
        };

        foreach (var fav in favorites)
        {
            var key = string.Format(CultureInfo.InvariantCulture, "{0:F4}_{1:F4}", fav.Latitude, fav.Longitude);
            choices.Add(new ChoiceSetSetting.Choice(fav.DisplayName ?? "Unknown", key));
        }

        _defaultLocation.Choices = choices;

        if (DefaultLocationKey != "auto" && !choices.Any(c => c.Value == DefaultLocationKey))
        {
            _defaultLocation.Value = "auto";
            SaveSettings();
        }
    }

    private static string SettingsJsonPath()
    {
        // Standalone implementation - use LocalApplicationData for settings
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var directory = Path.Combine(localAppData, "Microsoft.CmdPal");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "settings.json");
    }

    /// <summary>
    /// Returns the AdaptiveCard form template the host uses to render the
    /// settings sheet. Internal in the toolkit, so we reach for it via
    /// reflection — keeps us off the (private) <c>Settings.SettingsContentPage</c>
    /// implementation which always responds to submit with
    /// <c>CommandResult.GoHome()</c>.
    /// </summary>
    public string BuildSettingsFormJson()
    {
        var method = typeof(Settings).GetMethod(
            "ToFormJson",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        if (method is null)
        {
            WeatherLogger.LogToHost(
                MessageState.Warning,
                "Toolkit reflection failed: Settings.ToFormJson was not found. "
                + "The settings form may render blank until the Command Palette SDK is updated.");
            return "{}";
        }

        var raw = method.Invoke(Settings, null) as string ?? "{}";

        // Toolkit hard-codes the Save button label as "Save" in the form
        // template. Substitute the localized label we ship instead so the
        // action button matches the surrounding UI language.
        var saveLabel = Resources.settings_save_button ?? "Save";
        if (!string.IsNullOrEmpty(saveLabel) && saveLabel != "Save")
        {
            raw = raw.Replace("\"title\": \"Save\"", $"\"title\": \"{EscapeJson(saveLabel)}\"", System.StringComparison.Ordinal);
        }

        return raw;
    }

    private static string EscapeJson(string input)
        => input
            .Replace("\\", "\\\\", System.StringComparison.Ordinal)
            .Replace("\"", "\\\"", System.StringComparison.Ordinal);

    /// <summary>
    /// Forces the public <see cref="Settings.SettingsChanged"/> event to fire
    /// — the toolkit's own raise method is internal. Used after
    /// <see cref="Settings.Update(string)"/> in our custom settings page.
    /// </summary>
    public void RaiseSettingsChanged()
    {
        var method = typeof(Settings).GetMethod(
            "RaiseSettingsChanged",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

        if (method is null)
        {
            WeatherLogger.LogToHost(
                MessageState.Warning,
                "Toolkit reflection failed: Settings.RaiseSettingsChanged was not found. "
                + "Saved settings may not propagate to pages and dock bands until the Command Palette SDK is updated.");
            return;
        }

        method.Invoke(Settings, null);
    }
}
