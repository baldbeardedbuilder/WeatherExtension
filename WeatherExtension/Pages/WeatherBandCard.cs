// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BaldBeardedBuilder.WeatherExtension;
using Microsoft.CmdPal.Ext.Weather.Models;
using Microsoft.CmdPal.Ext.Weather.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System.Globalization;
using System.Text;

namespace Microsoft.CmdPal.Ext.Weather.Pages;

internal sealed partial class WeatherBandCard : ContentPage, IDisposable
{
	// Tweak these to change how much we cram into the card. Changes here
	// automatically resize the JSON payload, the AdaptiveCard template, and
	// every loading/error state — they're the single source of truth.
	private const int HourlyEntriesOnCard = 10;
	private const int DailyEntriesOnCard = 6;

	// Cached CompositeFormats for the localized "Next N hours" / "N-day forecast"
	// strings. CA1863 wants these reused rather than re-parsed each render.
	// Re-parsed when the UI culture changes so language switching at runtime
	// still works.
	private static System.Text.CompositeFormat? _nextHoursFormat;
	private static System.Text.CompositeFormat? _dayForecastFormat;
	private static string? _formatCulture;
	private static readonly Lock _formatSync = new();

	private static (System.Text.CompositeFormat NextHours, System.Text.CompositeFormat DayForecast) GetSectionFormats()
	{
		var current = CultureInfo.CurrentUICulture.Name;
		lock (_formatSync)
		{
			if (_nextHoursFormat == null || _dayForecastFormat == null || _formatCulture != current)
			{
				_nextHoursFormat = System.Text.CompositeFormat.Parse(Resources.card_section_next_hours);
				_dayForecastFormat = System.Text.CompositeFormat.Parse(Resources.card_section_day_forecast);
				_formatCulture = current;
			}

			return (_nextHoursFormat, _dayForecastFormat);
		}
	}

	private readonly OpenMeteoService _weatherService;
	private readonly IGeocodingService _geocodingService;
	private readonly WeatherSettingsManager _settings;
	private readonly FavoritesManager? _favoritesManager;
	private readonly FormContent _weatherForm = new();
	private readonly CancellationTokenSource _cts = new();
	private readonly GeocodingResult? _fixedLocation;

	public WeatherBandCard(
		OpenMeteoService weatherService,
		IGeocodingService geocodingService,
		WeatherSettingsManager settings,
		FavoritesManager? favoritesManager = null,
		GeocodingResult? fixedLocation = null)
	{
		_weatherService = weatherService ?? throw new ArgumentNullException(nameof(weatherService));
		_geocodingService = geocodingService ?? throw new ArgumentNullException(nameof(geocodingService));
		_settings = settings ?? throw new ArgumentNullException(nameof(settings));
		_favoritesManager = favoritesManager;
		_fixedLocation = fixedLocation;

		Id = "com.baldbeardedbuilder.cmdpal.weather.card";
		Name = Resources.plugin_name;
		Title = Resources.plugin_name;
		Icon = Icons.WeatherIcon;

		_weatherForm.TemplateJson = GetCardTemplate();
		_weatherForm.DataJson = GetLoadingData();

		_settings.Settings.SettingsChanged += OnSettingsChanged;
		if (_favoritesManager != null)
		{
			_favoritesManager.FavoritesChanged += OnFavoritesChanged;
		}

		_ = LoadWeatherDataAsync();
	}

	public override IContent[] GetContent() => [_weatherForm];

	internal async Task LoadWeatherDataAsync()
	{
		try
		{
			GeocodingResult location;

			if (_fixedLocation != null)
			{
				location = _fixedLocation;
			}
			else if (_favoritesManager != null)
			{
				var favorites = _favoritesManager.GetFavorites();
				if (favorites.Count == 0)
				{
					_weatherForm.DataJson = GetErrorData(
						Resources.no_favorites_hint);
					RaiseItemsChanged();
					return;
				}

				location = favorites[0].ToGeocodingResult();
			}
			else
			{
				_weatherForm.DataJson = GetErrorData(
					Resources.location_not_found);
				RaiseItemsChanged();
				return;
			}
			var weather = await _weatherService.GetCurrentWeatherAsync(
				location.Latitude,
				location.Longitude,
				_settings.TemperatureUnit,
				_settings.WindSpeedUnit,
				_cts.Token);

			var forecast = await _weatherService.GetForecastAsync(
				location.Latitude,
				location.Longitude,
				_settings.TemperatureUnit,
				_cts.Token);

			var hourly = await _weatherService.GetHourlyForecastAsync(
				location.Latitude,
				location.Longitude,
				_settings.TemperatureUnit,
				_settings.WindSpeedUnit,
				_cts.Token);

			_weatherForm.DataJson = BuildWeatherData(location, weather, forecast, hourly);
			RaiseItemsChanged();
		}
		catch (Exception ex)
		{
			WeatherLogger.LogToHost(
				MessageState.Error,
				$"Content page weather load error: {ex.Message}");

			_weatherForm.DataJson = GetErrorData(
				Resources.unavailable);
			RaiseItemsChanged();
		}
	}

	private string BuildWeatherData(
		GeocodingResult location,
		WeatherData? weather,
		ForecastData? forecast,
		HourlyForecastData? hourly)
	{
		var tempUnitSetting = _settings.TemperatureUnit;
		var windUnitSetting = _settings.WindSpeedUnit;

		var currentTemp = "--";
		var currentCondition = Resources.card_condition_unknown;
		var currentIcon = "\uD83C\uDF24\uFE0F";
		var feelsLike = "--";
		var humidity = "--";
		var wind = "--";
		var todayHighLow = "--";

		if (weather?.Current != null)
		{
			var c = weather.Current;
			currentTemp = WeatherFormatter.Temperature(c.Temperature, tempUnitSetting);
			currentCondition = Icons.GetWeatherDescription(c.WeatherCode);
			currentIcon = GetEmojiForWeatherCode(c.WeatherCode);
			feelsLike = WeatherFormatter.Temperature(c.ApparentTemperature, tempUnitSetting);
			humidity = WeatherFormatter.Humidity(c.RelativeHumidity);
			wind = WeatherFormatter.Wind(c.WindSpeed, windUnitSetting);
		}

		if (forecast?.Daily?.TemperatureMax?.Count > 0 &&
			forecast.Daily.TemperatureMin?.Count > 0)
		{
			var high = forecast.Daily.TemperatureMax[0];
			var low = forecast.Daily.TemperatureMin[0];
			todayHighLow = WeatherFormatter.HighLow(high, low, tempUnitSetting);
		}

		var hours = ExtractHourlySlots(hourly, currentIcon, tempUnitSetting, _settings.Use24HourClock);
		var days = ExtractDailySlots(forecast, currentIcon, tempUnitSetting);

		var sb = new StringBuilder(2048);
		sb.Append('{');
		AppendJsonString(sb, "locationName", location.DisplayName);
		AppendJsonString(sb, "currentIcon", currentIcon);
		AppendJsonString(sb, "currentTemp", currentTemp);
		AppendJsonString(sb, "currentCondition", currentCondition);
		AppendJsonString(sb, "feelsLike", feelsLike);
		AppendJsonString(sb, "todayHighLow", todayHighLow);
		AppendJsonString(sb, "humidity", humidity);
		AppendJsonString(sb, "wind", wind);

		for (var i = 0; i < HourlyEntriesOnCard; i++)
		{
			var s = i < hours.Count ? hours[i] : HourSlot.Empty(currentIcon);
			AppendJsonString(sb, $"hour{i + 1}Time", s.Time);
			AppendJsonString(sb, $"hour{i + 1}Icon", s.Icon);
			AppendJsonString(sb, $"hour{i + 1}Temp", s.Temp);
			AppendJsonString(sb, $"hour{i + 1}Precip", s.Precip);
		}

		for (var i = 0; i < DailyEntriesOnCard; i++)
		{
			var s = i < days.Count ? days[i] : DaySlot.Empty(currentIcon);
			AppendJsonString(sb, $"day{i + 1}Name", s.Name);
			AppendJsonString(sb, $"day{i + 1}Icon", s.Icon);
			AppendJsonString(sb, $"day{i + 1}Condition", s.Condition);
			AppendJsonString(sb, $"day{i + 1}HighLow", s.HighLow);
		}

		// Trim trailing comma left by AppendJsonString and close the object.
		if (sb[sb.Length - 1] == ',')
		{
			sb.Length--;
		}

		sb.Append('}');
		return sb.ToString();
	}

	private static List<HourSlot> ExtractHourlySlots(HourlyForecastData? hourly, string fallbackIcon, string tempUnit, bool use24Hour)
	{
		var result = new List<HourSlot>(HourlyEntriesOnCard);
		if (hourly?.Hourly?.Time == null)
		{
			return result;
		}

		var now = DateTime.Now;
		for (var i = 0; i < hourly.Hourly.Time.Count && result.Count < HourlyEntriesOnCard; i++)
		{
			var timeStr = hourly.Hourly.Time[i];
			if (timeStr == null || !DateTime.TryParse(timeStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var hourTime))
			{
				continue;
			}

			if (hourTime <= now)
			{
				continue;
			}

			var weatherCode = hourly.Hourly.WeatherCode?[i] ?? 0;
			result.Add(new HourSlot(
				Time: WeatherFormatter.Hour(hourTime, use24Hour),
				Icon: GetEmojiForWeatherCode(weatherCode),
				Temp: WeatherFormatter.Temperature(hourly.Hourly.Temperature?[i] ?? 0, tempUnit),
				Precip: WeatherFormatter.PrecipitationProbability(hourly.Hourly.PrecipitationProbability?[i])));
		}

		return result;
	}

	private static List<DaySlot> ExtractDailySlots(ForecastData? forecast, string fallbackIcon, string tempUnit)
	{
		var result = new List<DaySlot>(DailyEntriesOnCard);
		var daily = forecast?.Daily;
		if (daily?.Time == null)
		{
			return result;
		}

		// Skip i=0 because that's "today" — already shown as the current
		// section's high/low. Card focuses on the days ahead.
		for (var i = 1; i < daily.Time.Count && result.Count < DailyEntriesOnCard; i++)
		{
			var dateStr = daily.Time[i];
			if (!DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
			{
				continue;
			}

			var weatherCode = daily.WeatherCode?[i] ?? 0;
			var high = daily.TemperatureMax?[i] ?? 0;
			var low = daily.TemperatureMin?[i] ?? 0;

			result.Add(new DaySlot(
				Name: date.ToString("ddd", CultureInfo.CurrentCulture),
				Icon: GetEmojiForWeatherCode(weatherCode),
				Condition: Icons.GetWeatherDescription(weatherCode),
				HighLow: WeatherFormatter.HighLow(high, low, tempUnit)));
		}

		return result;
	}

	private readonly record struct HourSlot(string Time, string Icon, string Temp, string Precip)
	{
		public static HourSlot Empty(string fallbackIcon) => new("--", fallbackIcon, "--", "--");
	}

	private readonly record struct DaySlot(string Name, string Icon, string Condition, string HighLow)
	{
		public static DaySlot Empty(string fallbackIcon) => new("--", fallbackIcon, "--", "--");
	}

	private static void AppendJsonString(StringBuilder sb, string key, string? value)
	{
		sb.Append('"').Append(key).Append("\":\"").Append(JsonEscape(value)).Append("\",");
	}

	private static string GetEmojiForWeatherCode(int weatherCode)
	{
		return weatherCode switch
		{
			0 => "\u2600\uFE0F",
			1 or 2 => "\uD83C\uDF24\uFE0F",
			3 => "\u26C5",
			45 or 48 => "\uD83C\uDF2B\uFE0F",
			51 or 53 or 55 => "\uD83C\uDF26\uFE0F",
			56 or 57 => "\uD83C\uDF27\uFE0F",
			61 or 63 or 65 => "\uD83C\uDF27\uFE0F",
			66 or 67 => "\uD83C\uDF27\uFE0F",
			71 or 73 or 75 or 77 => "\u2744\uFE0F",
			80 or 81 or 82 => "\uD83C\uDF26\uFE0F",
			85 or 86 => "\uD83C\uDF28\uFE0F",
			95 => "\u26C8\uFE0F",
			96 or 99 => "\u26C8\uFE0F",
			_ => "\uD83C\uDF24\uFE0F",
		};
	}

	private static string GetLoadingData()
		=> BuildPlaceholderData(Resources.loading, Resources.loading_data, "🌤️");

	private static string GetErrorData(string errorMessage)
		=> BuildPlaceholderData(errorMessage, errorMessage, "⚠️");

	// Reused for "still loading" and "we hit an error" payloads. Keeps every
	// hour/day slot filled with placeholders so the AdaptiveCard data binding
	// always finds a value, no matter how many slots the template defines.
	private static string BuildPlaceholderData(string locationLabel, string statusLabel, string iconEmoji)
	{
		var sb = new StringBuilder(1024);
		sb.Append('{');
		AppendJsonString(sb, "locationName", locationLabel);
		AppendJsonString(sb, "currentIcon", iconEmoji);
		AppendJsonString(sb, "currentTemp", "--");
		AppendJsonString(sb, "currentCondition", statusLabel);
		AppendJsonString(sb, "feelsLike", "--");
		AppendJsonString(sb, "todayHighLow", "--");
		AppendJsonString(sb, "humidity", "--");
		AppendJsonString(sb, "wind", "--");

		for (var i = 0; i < HourlyEntriesOnCard; i++)
		{
			AppendJsonString(sb, $"hour{i + 1}Time", "--");
			AppendJsonString(sb, $"hour{i + 1}Icon", iconEmoji);
			AppendJsonString(sb, $"hour{i + 1}Temp", "--");
			AppendJsonString(sb, $"hour{i + 1}Precip", "--");
		}

		for (var i = 0; i < DailyEntriesOnCard; i++)
		{
			AppendJsonString(sb, $"day{i + 1}Name", "--");
			AppendJsonString(sb, $"day{i + 1}Icon", iconEmoji);
			AppendJsonString(sb, $"day{i + 1}Condition", "--");
			AppendJsonString(sb, $"day{i + 1}HighLow", "--");
		}

		if (sb[sb.Length - 1] == ',')
		{
			sb.Length--;
		}

		sb.Append('}');
		return sb.ToString();
	}

	private static string GetCardTemplate()
	{
		// Section/label texts come from Resources so the card respects the
		// system UI language (with English fallback).
		var sectionCurrent = JsonEscape(Resources.card_section_current);
		var (nextHoursFormat, dayForecastFormat) = GetSectionFormats();
		var sectionNext = JsonEscape(string.Format(
			CultureInfo.CurrentCulture,
			nextHoursFormat,
			HourlyEntriesOnCard));
		var sectionForecast = JsonEscape(string.Format(
			CultureInfo.CurrentCulture,
			dayForecastFormat,
			DailyEntriesOnCard));
		var labelFeelsLike = JsonEscape(Resources.feels_like);
		var labelHighLow = JsonEscape(Resources.card_label_high_low);
		var labelHumidity = JsonEscape(Resources.humidity);
		var labelWind = JsonEscape(Resources.wind_speed);

		var hourColumns = BuildSlotColumns(HourlyEntriesOnCard, "hour", includeCondition: false);
		var dayColumns = BuildSlotColumns(DailyEntriesOnCard, "day", includeCondition: true);

		// Lay the card out in three vertical sections so the renderer doesn't
		// have to balance one big ColumnSet that mixes wide and narrow columns.
		// Older versions tried to do "Current (width=3) | Hour1 | Hour2 | Hour3"
		// in a single row which collapsed the right-hand FactSet into a 1-char
		// column on some hosts. Rows now stretch independently.
		return $$"""
        {
            "type": "AdaptiveCard",
            "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
            "version": "1.5",
            "body": [
                {
                    "type": "TextBlock",
                    "text": "${locationName}",
                    "size": "large",
                    "weight": "bolder",
                    "wrap": true
                },
                {
                    "type": "TextBlock",
                    "text": "{{sectionCurrent}}",
                    "weight": "bolder",
                    "size": "medium",
                    "spacing": "medium"
                },
                {
                    "type": "ColumnSet",
                    "style": "emphasis",
                    "columns": [
                        {
                            "type": "Column",
                            "width": "auto",
                            "verticalContentAlignment": "center",
                            "items": [
                                { "type": "TextBlock", "text": "${currentIcon}", "size": "extraLarge", "horizontalAlignment": "center", "spacing": "none" },
                                { "type": "TextBlock", "text": "${currentTemp}", "size": "extraLarge", "weight": "bolder", "horizontalAlignment": "center", "spacing": "none" },
                                { "type": "TextBlock", "text": "${currentCondition}", "wrap": true, "horizontalAlignment": "center", "isSubtle": true, "spacing": "none" }
                            ]
                        },
                        {
                            "type": "Column",
                            "width": "stretch",
                            "spacing": "large",
                            "verticalContentAlignment": "center",
                            "items": [
                                {
                                    "type": "FactSet",
                                    "facts": [
                                        { "title": "{{labelFeelsLike}}", "value": "${feelsLike}" },
                                        { "title": "{{labelHighLow}}", "value": "${todayHighLow}" },
                                        { "title": "{{labelHumidity}}", "value": "${humidity}" },
                                        { "title": "{{labelWind}}", "value": "${wind}" }
                                    ]
                                }
                            ]
                        }
                    ]
                },
                {
                    "type": "TextBlock",
                    "text": "{{sectionNext}}",
                    "weight": "bolder",
                    "size": "medium",
                    "spacing": "large",
                    "separator": true
                },
                {
                    "type": "ColumnSet",
                    "columns": [{{hourColumns}}]
                },
                {
                    "type": "TextBlock",
                    "text": "{{sectionForecast}}",
                    "size": "medium",
                    "weight": "bolder",
                    "separator": true,
                    "spacing": "large"
                },
                {
                    "type": "ColumnSet",
                    "columns": [{{dayColumns}}]
                }
            ]
        }
        """;
	}

	// Builds the JSON for a row of N stretch-width Columns. Hour rows show
	// time/icon/temp/precip; day rows additionally display the localized
	// condition. Splitting by a flag avoids two near-identical templates.
	private static string BuildSlotColumns(int count, string slotPrefix, bool includeCondition)
	{
		var sb = new StringBuilder(512);
		for (var i = 1; i <= count; i++)
		{
			if (i > 1)
			{
				sb.Append(',');
			}

			sb.Append("{\"type\":\"Column\",\"width\":\"stretch\",\"style\":\"emphasis\",\"spacing\":\"")
			  .Append(i == 1 ? "default" : "small")
			  .Append("\",\"verticalContentAlignment\":\"")
			  .Append(includeCondition ? "top" : "center")
			  .Append("\",\"items\":[")
			  .Append("{\"type\":\"TextBlock\",\"text\":\"${")
			  .Append(slotPrefix).Append(i).Append(includeCondition ? "Name" : "Time")
			  .Append("}\",\"weight\":\"bolder\",\"horizontalAlignment\":\"center\",\"wrap\":true},")
			  .Append("{\"type\":\"TextBlock\",\"text\":\"${")
			  .Append(slotPrefix).Append(i).Append("Icon")
			  .Append("}\",\"horizontalAlignment\":\"center\",\"size\":\"large\",\"spacing\":\"small\"},");

			if (includeCondition)
			{
				sb.Append("{\"type\":\"TextBlock\",\"text\":\"${")
				  .Append(slotPrefix).Append(i).Append("Condition")
				  .Append("}\",\"horizontalAlignment\":\"center\",\"isSubtle\":true,\"wrap\":true,\"size\":\"small\",\"spacing\":\"small\"},")
				  .Append("{\"type\":\"TextBlock\",\"text\":\"${")
				  .Append(slotPrefix).Append(i).Append("HighLow")
				  .Append("}\",\"horizontalAlignment\":\"center\",\"spacing\":\"small\"}");
			}
			else
			{
				sb.Append("{\"type\":\"TextBlock\",\"text\":\"${")
				  .Append(slotPrefix).Append(i).Append("Temp")
				  .Append("}\",\"horizontalAlignment\":\"center\",\"spacing\":\"small\"},")
				  .Append("{\"type\":\"TextBlock\",\"text\":\"${")
				  .Append(slotPrefix).Append(i).Append("Precip")
				  .Append("}\",\"horizontalAlignment\":\"center\",\"isSubtle\":true,\"size\":\"small\",\"spacing\":\"none\"}");
			}

			sb.Append("]}");
		}

		return sb.ToString();
	}

	private static string JsonEscape(string? input)
		=> string.IsNullOrEmpty(input)
			? string.Empty
			: input.Replace("\\", "\\\\", StringComparison.Ordinal)
				   .Replace("\"", "\\\"", StringComparison.Ordinal);

	private async void OnFavoritesChanged(object? sender, EventArgs e)
	{
		if (_fixedLocation == null)
		{
			await LoadWeatherDataAsync();
		}
	}

	private async void OnSettingsChanged(object sender, Settings args)
	{
		await LoadWeatherDataAsync();
	}

	public void Dispose()
	{
		_settings.Settings.SettingsChanged -= OnSettingsChanged;
		if (_favoritesManager != null)
		{
			_favoritesManager.FavoritesChanged -= OnFavoritesChanged;
		}
		_cts?.Cancel();
		_cts?.Dispose();
	}
}
