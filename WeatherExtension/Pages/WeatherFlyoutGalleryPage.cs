// Copyright (c) Bald Bearded Builder LLC
// Licensed under the MIT license. See LICENSE in the project root.

using Microsoft.CmdPal.Ext.Weather.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Microsoft.CmdPal.Ext.Weather.Pages;

internal sealed partial class WeatherFlyoutGalleryPage : ListPage
{
    private readonly WeatherSettingsManager _settings;

    public WeatherFlyoutGalleryPage(WeatherSettingsManager settings)
    {
        _settings = settings;
        Id = "com.baldbeardedbuilder.cmdpal.weather.flyout.gallery";
        Name = FlyoutText.Get("ChooseView");
        Title = Name;
        Icon = Icons.WeatherIcon;
        GridProperties = new GalleryGridLayout { ShowTitle = true, ShowSubtitle = true };
    }

    public override IListItem[] GetItems() => WeatherFlyoutCatalog.Views.Select(view =>
        new ListItem(new WeatherFlyoutPreviewPage(_settings, view))
        {
            Title = WeatherFlyoutCatalog.Name(view),
            Subtitle = WeatherFlyoutCatalog.Description(view),
            Icon = WeatherFlyoutCatalog.PreviewIcon(view),
        }).ToArray();
}
