// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BaldBeardedBuilder.WeatherExtension;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Microsoft.CmdPal.Ext.Weather.Pages;

/// <summary>
/// A content page shown in the EmptyContent slot of WeatherListPage when a
/// search returns no results. Displays supported query formats as markdown.
/// </summary>
internal sealed partial class EmptySearchHintPage : ContentPage
{
	private readonly MarkdownContent _content;

	public EmptySearchHintPage()
	{
		Name = Resources.no_locations_found;
		Title = Resources.no_locations_found;
		Icon = Icons.WeatherIcon;

		_content = new MarkdownContent
		{
			Body = $"## {Resources.no_locations_found}\n\n{Resources.search_format_hint}",
		};
	}

	public override IContent[] GetContent() => [_content];
}
