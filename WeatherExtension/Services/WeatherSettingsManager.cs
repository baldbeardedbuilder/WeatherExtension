// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
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

    private static readonly HashSet<string> _validIntervals = ["60", "180", "360", "720"];

    public string TemperatureUnit => _temperatureUnit.Value ?? "celsius";

    public string WindSpeedUnit => _windSpeedUnit.Value ?? "kmh";

    public bool ShowForecast => _showForecast.Value;

    public int UpdateIntervalMinutes => _updateInterval.Value is string v && _validIntervals.Contains(v) ? int.Parse(v, System.Globalization.CultureInfo.InvariantCulture) : 60;

    public WeatherSettingsManager()
    {
        FilePath = SettingsJsonPath();

        Settings.Add(_temperatureUnit);
        Settings.Add(_windSpeedUnit);
        Settings.Add(_showForecast);
        Settings.Add(_updateInterval);

        LoadSettings();
        MigrateUpdateInterval();

        Settings.SettingsChanged += (_, _) => SaveSettings();
    }

    internal WeatherSettingsManager(string filePath)
    {
        FilePath = filePath;

        Settings.Add(_temperatureUnit);
        Settings.Add(_windSpeedUnit);
        Settings.Add(_showForecast);
        Settings.Add(_updateInterval);

        LoadSettings();
        MigrateUpdateInterval();

        Settings.SettingsChanged += (_, _) => SaveSettings();
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

    internal static string SettingsJsonPath()
    {
        // Standalone implementation - use LocalApplicationData for settings
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var directory = Path.Combine(localAppData, "Microsoft.CmdPal");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "settings.json");
    }

    /// <summary>
    /// Reads the settings file and returns the old DefaultLocation value if it was
    /// set by the user (i.e. differs from the default "98101"). Also removes the key
    /// from the file so the migration only runs once. Returns null when no migration
    /// is needed or the file cannot be read.
    /// </summary>
    internal static async Task<string?> MigrateDefaultLocationAsync(string settingsFilePath)
    {
        const string key = "weather.DefaultLocation";
        const string defaultValue = "98101";

        try
        {
            if (!File.Exists(settingsFilePath))
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(settingsFilePath).ConfigureAwait(false);
            var node = JsonNode.Parse(json);
            if (node is not JsonObject obj || !obj.ContainsKey(key))
            {
                return null;
            }

            var value = obj[key]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(value) || value == defaultValue)
            {
                return null;
            }

            // Remove the key so migration only runs once
            obj.Remove(key);

            var updated = obj.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(settingsFilePath, updated).ConfigureAwait(false);

            return value;
        }
        catch (Exception ex)
        {
            WeatherLogger.LogToHost(
                Microsoft.CommandPalette.Extensions.MessageState.Error,
                $"DefaultLocation migration error: {ex.Message}");
            return null;
        }
    }
}
