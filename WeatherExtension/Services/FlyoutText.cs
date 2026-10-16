// Copyright (c) Bald Bearded Builder LLC
// Licensed under the MIT license. See LICENSE in the project root.

using System.Globalization;
using System.Resources;
using System.Collections.Concurrent;
using System.Text;

namespace Microsoft.CmdPal.Ext.Weather.Services;

internal static class FlyoutText
{
    private static readonly ResourceManager Manager = new(
        "BaldBeardedBuilder.WeatherExtension.Properties.FlyoutResources", typeof(FlyoutText).Assembly);
    private static readonly ConcurrentDictionary<string, CompositeFormat> Formats = new();

    public static string Get(string key) =>
        Manager.GetString(key, CultureInfo.CurrentUICulture)
        ?? throw new MissingManifestResourceException($"Missing flyout resource: {key}");

    public static string Format(string key, params object[] args) =>
        FormatTemplate(Get(key), args);

    public static string FormatTemplate(string template, params object[] args) =>
        string.Format(CultureInfo.CurrentCulture, Formats.GetOrAdd(template, CompositeFormat.Parse), args);
}
