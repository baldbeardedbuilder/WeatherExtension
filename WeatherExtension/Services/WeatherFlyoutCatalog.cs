// Copyright (c) Bald Bearded Builder LLC
// Licensed under the MIT license. See LICENSE in the project root.

using System.IO;
using Microsoft.CmdPal.Ext.Weather.Models;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Microsoft.CmdPal.Ext.Weather.Services;

internal static class WeatherFlyoutCatalog
{
    public static IReadOnlyList<WeatherFlyoutView> Views { get; } =
    [
        WeatherFlyoutView.Glance, WeatherFlyoutView.NextHours, WeatherFlyoutView.DailyOutlook,
        WeatherFlyoutView.DaylightArc, WeatherFlyoutView.DayBrowser, WeatherFlyoutView.HourInspector,
    ];

    public static string Name(WeatherFlyoutView view) => FlyoutText.Get(view.ToString());

    public static string Description(WeatherFlyoutView view) => FlyoutText.Get($"{view}Description");

    public static string PreviewPath(WeatherFlyoutView view, bool light = false) =>
        Path.Combine(AppContext.BaseDirectory, "Assets", "Flyouts", $"{view}{(light ? "-light" : string.Empty)}.png");

    public static IconInfo PreviewIcon(WeatherFlyoutView view) =>
        new(new IconData(PreviewPath(view, light: true)), new IconData(PreviewPath(view)));
}
