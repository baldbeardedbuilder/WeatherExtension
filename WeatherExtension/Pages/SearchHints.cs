// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BaldBeardedBuilder.WeatherExtension;

namespace Microsoft.CmdPal.Ext.Weather.Pages;

/// <summary>
/// Composes stacked search / favorites hint text for list empty states.
/// </summary>
internal static class SearchHints
{
	/// <summary>
	/// Shown in the Command Palette empty-state panel (Title + Subtitle area).
	/// Uses newlines so each example city appears on its own line — Details.Body
	/// is not rendered in that layout, so the subtitle must carry the examples.
	/// </summary>
	public static string BuildListHintEmptySubtitle()
		=> string.Join(
			'\n',
			[
				Resources.search_hint_examples_title,
				Resources.search_hint_examples_block,
				string.Empty,
				Resources.search_hint_favorite_shortcut,
				string.Empty,
				Resources.search_hint_multiple_favorites,
			]);

	public static string BuildListHintBody()
		=> string.Join(
			"\n\n",
			[
				Resources.search_hint_examples_block,
				Resources.search_hint_favorite_shortcut,
				Resources.search_hint_multiple_favorites,
			]);

	public static string BuildListHintMarkdown(bool includeSearchFormatHint = false)
	{
		var sections = new List<string>
		{
			$"### {Resources.search_hint_examples_title}",
			FormatExamplesAsMarkdownList(),
			Resources.search_hint_favorite_shortcut,
			Resources.search_hint_multiple_favorites,
		};

		if (includeSearchFormatHint)
		{
			sections.Add(Resources.search_format_hint);
		}

		return string.Join("\n\n", sections);
	}

	private static string FormatExamplesAsMarkdownList()
	{
		var lines = Resources.search_hint_examples_block
			.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		return string.Join('\n', lines.Select(static line => $"- {line}"));
	}
}
