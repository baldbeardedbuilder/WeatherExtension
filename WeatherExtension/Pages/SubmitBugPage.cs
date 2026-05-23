// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BaldBeardedBuilder.WeatherExtension;
using Microsoft.CmdPal.Ext.Weather.Commands;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Microsoft.CmdPal.Ext.Weather.Pages;

/// <summary>
/// Guides the user through submitting a bug report.
/// Displays two actions — save logs to Desktop and open GitHub Issues —
/// with step-by-step instructions in the Details panel.
/// </summary>
internal sealed partial class SubmitBugPage : ListPage
{
    private readonly IListItem[] _items;

    public SubmitBugPage()
    {
        Name = Resources.bug_report_title;
        Title = Resources.bug_report_title;
        Icon = new IconInfo("\uE730"); // Bug icon
        Id = "com.baldbeardedbuilder.cmdpal.weather.submitbug";
        ShowDetails = true;

        var instructionDetails = new Details
        {
            Title = Resources.bug_report_title,
            Body = Resources.bug_report_instructions,
        };

        _items =
        [
            new ListItem(new SaveLogsCommand())
            {
                Title = Resources.bug_report_save_logs,
                Subtitle = Resources.bug_report_title,
                Details = instructionDetails,
            },
            new ListItem(new OpenGitHubIssuesCommand())
            {
                Title = Resources.bug_report_open_github,
                Subtitle = Resources.bug_report_title,
                Details = instructionDetails,
            },
        ];
    }

    public override IListItem[] GetItems() => _items;
}
