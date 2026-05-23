// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using BaldBeardedBuilder.WeatherExtension;
using Microsoft.CmdPal.Ext.Weather.Pages;

namespace Microsoft.CmdPal.Ext.Weather.UnitTests;

[TestClass]
public class SearchHintTests
{
	[TestMethod]
	public void BuildListHintEmptySubtitle_IncludesStackedExamples()
	{
		var subtitle = SearchHints.BuildListHintEmptySubtitle();

		Assert.IsTrue(subtitle.StartsWith(Resources.search_hint_examples_title, StringComparison.Ordinal));
		Assert.IsTrue(subtitle.Contains(Resources.search_hint_examples_block, StringComparison.Ordinal));
		Assert.IsTrue(subtitle.Contains('\n'),
			"Empty-state subtitle must use newlines so each example is visible.");
	}

	[TestMethod]
	public void BuildListHintBody_StacksSectionsWithBlankLines()
	{
		var body = SearchHints.BuildListHintBody();

		Assert.IsTrue(body.Contains("\n\n", StringComparison.Ordinal),
			"Hint body should separate sections with blank lines.");
		Assert.IsTrue(body.Contains(Resources.search_hint_examples_block, StringComparison.Ordinal));
		Assert.IsTrue(body.Contains(Resources.search_hint_favorite_shortcut, StringComparison.Ordinal));
		Assert.IsTrue(body.Contains(Resources.search_hint_multiple_favorites, StringComparison.Ordinal));
	}

	[TestMethod]
	public void BuildListHintBody_ExamplesBlockIsVertical()
	{
		var block = Resources.search_hint_examples_block;

		Assert.IsTrue(block.Contains('\n'),
			"Each example location should be on its own line.");
		Assert.IsFalse(block.Contains("   ", StringComparison.Ordinal),
			"Examples should not use inline spacing separators.");
	}

	[TestMethod]
	public void BuildListHintMarkdown_FormatsExamplesAsBulletList()
	{
		var original = CultureInfo.CurrentUICulture;
		try
		{
			CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
			var markdown = SearchHints.BuildListHintMarkdown();

			Assert.IsTrue(markdown.Contains("- ", StringComparison.Ordinal));
			StringAssert.Contains(markdown, "İstanbul");
		}
		finally
		{
			CultureInfo.CurrentUICulture = original;
		}
	}

	[TestMethod]
	public void TurkishCulture_ExamplesBlockUsesLocalSamples()
	{
		var original = CultureInfo.CurrentUICulture;
		try
		{
			CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
			var block = Resources.search_hint_examples_block;

			StringAssert.Contains(block, "İstanbul");
			StringAssert.Contains(Resources.search_hint_multiple_favorites, "eklenti ayarlarından");
		}
		finally
		{
			CultureInfo.CurrentUICulture = original;
		}
	}
}
