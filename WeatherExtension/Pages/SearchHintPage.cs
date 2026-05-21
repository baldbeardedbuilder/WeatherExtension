// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BaldBeardedBuilder.WeatherExtension;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Microsoft.CmdPal.Ext.Weather.Pages;

/// <summary>
/// Markdown hint page for empty list / search states. Shown when the user
/// opens the empty-state row; the host empty panel itself reads
/// <see cref="SearchHints.BuildListHintEmptySubtitle"/> from the ListItem.
/// </summary>
internal sealed partial class SearchHintPage : ContentPage
{
	private readonly MarkdownContent _content;

	public SearchHintPage(string headline, bool includeSearchFormatHint = false)
	{
		Name = headline;
		Title = headline;
		Icon = Icons.WeatherIcon;

		var body = SearchHints.BuildListHintMarkdown(includeSearchFormatHint);
		_content = new MarkdownContent
		{
			Body = $"## {headline}\n\n{body}",
		};
	}

	public override IContent[] GetContent() => [_content];
}
