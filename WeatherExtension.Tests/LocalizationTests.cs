// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Reflection;
using BaldBeardedBuilder.WeatherExtension;

namespace Microsoft.CmdPal.Ext.Weather.UnitTests;

[TestClass]
public class LocalizationTests
{
	private static readonly string[] SupportedCultures =
	[
		"en", "tr", "de", "fr", "es", "it", "pt-BR",
		"ru", "ja", "zh-Hans", "zh-Hant", "ko", "pl", "nl", "ar",
	];

	[TestMethod]
	public void EveryShippedCulture_ResolvesAllSearchHintResourceKeys()
	{
		var originalCulture = CultureInfo.CurrentUICulture;
		var failures = new List<string>();
		var hintKeys = GetSearchHintResourceAccessors();

		Assert.AreEqual(4, hintKeys.Count,
			"Expected title, examples block, shortcut, and multiple-favorites hint keys.");

		try
		{
			foreach (var culture in SupportedCultures)
			{
				CultureInfo.CurrentUICulture = new CultureInfo(culture);
				foreach (var (key, accessor) in hintKeys)
				{
					var value = accessor();
					if (string.IsNullOrWhiteSpace(value))
					{
						failures.Add($"{culture}: {key} was empty");
					}
				}
			}
		}
		finally
		{
			CultureInfo.CurrentUICulture = originalCulture;
		}

		Assert.AreEqual(0, failures.Count,
			"Some cultures failed to resolve search_hint_* resources:\n" + string.Join('\n', failures));
	}

	[TestMethod]
	public void EveryShippedCulture_ResolvesAllWeatherResourceKeys()
	{
		var originalCulture = CultureInfo.CurrentUICulture;
		var failures = new List<string>();
		var weatherKeys = GetWeatherResourceAccessors();

		Assert.IsTrue(weatherKeys.Count >= 11,
			"Expected at least the WMO category keys plus weather_service_error.");

		try
		{
			foreach (var culture in SupportedCultures)
			{
				CultureInfo.CurrentUICulture = new CultureInfo(culture);
				foreach (var (key, accessor) in weatherKeys)
				{
					var value = accessor();
					if (string.IsNullOrWhiteSpace(value))
					{
						failures.Add($"{culture}: {key} was empty");
					}
				}
			}
		}
		finally
		{
			CultureInfo.CurrentUICulture = originalCulture;
		}

		Assert.AreEqual(0, failures.Count,
			"Some cultures failed to resolve weather_* resources:\n" + string.Join('\n', failures));
	}

	[TestMethod]
	public void EveryShippedCulture_ResolvesCorePluginStrings()
	{
		var originalCulture = CultureInfo.CurrentUICulture;
		var failures = new List<string>();

		// Sample one key from every category we ship per locale, so a missing
		// translation is caught here rather than at runtime.
		var requiredKeys = new (string Key, Func<string> Accessor)[]
		{
			(nameof(Resources.plugin_name), () => Resources.plugin_name),
			(nameof(Resources.card_section_current), () => Resources.card_section_current),
			(nameof(Resources.weather_thunderstorm), () => Resources.weather_thunderstorm),
			(nameof(Resources.feels_like_template), () => Resources.feels_like_template),
			(nameof(Resources.search_hint_examples_title), () => Resources.search_hint_examples_title),
			(nameof(Resources.search_hint_examples_block), () => Resources.search_hint_examples_block),
			(nameof(Resources.search_hint_favorite_shortcut), () => Resources.search_hint_favorite_shortcut),
			(nameof(Resources.search_hint_multiple_favorites), () => Resources.search_hint_multiple_favorites),
			(nameof(Resources.favorite_tag), () => Resources.favorite_tag),
		};

		try
		{
			foreach (var culture in SupportedCultures)
			{
				CultureInfo.CurrentUICulture = new CultureInfo(culture);
				foreach (var (key, accessor) in requiredKeys)
				{
					var value = accessor();
					if (string.IsNullOrWhiteSpace(value))
					{
						failures.Add($"{culture}: {key} was empty");
					}
				}
			}
		}
		finally
		{
			CultureInfo.CurrentUICulture = originalCulture;
		}

		Assert.AreEqual(0, failures.Count,
			"Some cultures failed to resolve resources:\n" + string.Join('\n', failures));
	}

	[TestMethod]
	public void TurkishCulture_ProvidesLocalizedPluginName()
	{
		var originalCulture = CultureInfo.CurrentUICulture;
		try
		{
			CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
			Assert.AreEqual("Hava Durumu", Resources.plugin_name);
			Assert.AreEqual("Favori", Resources.favorite_tag);
		}
		finally
		{
			CultureInfo.CurrentUICulture = originalCulture;
		}
	}

	[TestMethod]
	public void UnknownCulture_FallsBackToEnglish()
	{
		var originalCulture = CultureInfo.CurrentUICulture;
		try
		{
			// A culture we don't ship a satellite for should fall back to
			// English without throwing, courtesy of NeutralResourcesLanguageAttribute.
			CultureInfo.CurrentUICulture = new CultureInfo("eo"); // Esperanto
			Assert.AreEqual("Weather", Resources.plugin_name);
		}
		finally
		{
			CultureInfo.CurrentUICulture = originalCulture;
		}
	}

	private static IReadOnlyList<(string Key, Func<string> Accessor)> GetSearchHintResourceAccessors()
	{
		return typeof(Resources)
			.GetProperties(BindingFlags.Public | BindingFlags.Static)
			.Where(p => p.PropertyType == typeof(string)
				&& p.Name.StartsWith("search_hint_", StringComparison.Ordinal))
			.OrderBy(p => p.Name, StringComparer.Ordinal)
			.Select(p => (
				p.Name,
				(Func<string>)(() => (string)p.GetValue(null)!)))
			.ToArray();
	}

	private static IReadOnlyList<(string Key, Func<string> Accessor)> GetWeatherResourceAccessors()
	{
		return typeof(Resources)
			.GetProperties(BindingFlags.Public | BindingFlags.Static)
			.Where(p => p.PropertyType == typeof(string)
				&& p.Name.StartsWith("weather_", StringComparison.Ordinal))
			.OrderBy(p => p.Name, StringComparer.Ordinal)
			.Select(p => (
				p.Name,
				(Func<string>)(() => (string)p.GetValue(null)!)))
			.ToArray();
	}
}
