// Copyright (c) Bald Bearded Builder LLC
// Licensed under the MIT license. See LICENSE in the project root.

using System.Globalization;
using BaldBeardedBuilder.WeatherExtension;
using Microsoft.CmdPal.Ext.Weather.Models;
using Microsoft.CmdPal.Ext.Weather.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Microsoft.CmdPal.Ext.Weather.Pages;

internal sealed partial class WeatherFlyoutListPage : ListPage, IDisposable
{
    private readonly WeatherBandCard _source;
    private readonly WeatherFlyoutView _view;

    public WeatherFlyoutListPage(WeatherBandCard source, WeatherFlyoutView view, string locationKey)
    {
        if (view is not (WeatherFlyoutView.DayBrowser or WeatherFlyoutView.HourInspector))
        {
            throw new ArgumentOutOfRangeException(nameof(view));
        }

        _source = source;
        _view = view;
        Id = $"com.baldbeardedbuilder.cmdpal.weather.flyout.{view}.{locationKey}";
        Name = WeatherFlyoutCatalog.Name(view);
        Title = Name;
        Icon = Icons.WeatherIcon;
        ShowDetails = true;
        _source.SnapshotChanged += OnSnapshotChanged;
    }

    public override IListItem[] GetItems()
    {
        if (_source.Snapshot is not { } snapshot)
        {
            return [Status(_source.HasLoaded ? Resources.unavailable : Resources.dock_band_loading)];
        }

        Title = snapshot.Location.DisplayName;
        var localTime = snapshot.LocalTime(DateTimeOffset.UtcNow);
        if (localTime is null)
        {
            return [Status(FlyoutText.Get("TimeZoneUnavailable"))];
        }

        IListItem[] items = _view == WeatherFlyoutView.DayBrowser
            ? WeatherFlyoutForecasts.Days(snapshot, localTime.Value).Select(day => DayItem(day, snapshot)).ToArray()
            : WeatherFlyoutForecasts.Hours(snapshot, localTime.Value, 24).Select(hour => HourItem(hour, snapshot)).ToArray();
        return items.Length > 0 ? items : [Status(Resources.unavailable)];
    }

    private static ListItem DayItem(WeatherFlyoutForecasts.Day day, WeatherFlyoutSnapshot snapshot)
    {
        var title = day.Date.ToString("dddd, MMM d", CultureInfo.CurrentCulture);
        var condition = Condition(day.Code);
        var high = WeatherFlyoutCards.Temperature(day.High, snapshot);
        var low = WeatherFlyoutCards.Temperature(day.Low, snapshot);
        return new ListItem(new NoOpCommand())
        {
            Title = title,
            Subtitle = $"{condition} | {low} / {high}",
            Icon = day.Code.HasValue ? Icons.GetIconForWeatherCode(day.Code.Value) : Icons.WeatherIcon,
            Details = new Details
            {
                Size = ContentSize.Small,
                Title = title,
                Body = $"{WeatherFlyoutCards.Emoji(day.Code)} {condition}",
                Metadata =
                [
                    Fact(Resources.high, high), Fact(Resources.low, low),
                    Fact(Resources.precipitation, WeatherFlyoutCards.Percent(day.Rain)),
                    Fact(FlyoutText.Get("Sunrise"), WeatherFlyoutCards.Clock(day.Sunrise, snapshot)),
                    Fact(FlyoutText.Get("Sunset"), WeatherFlyoutCards.Clock(day.Sunset, snapshot)),
                ],
            },
        };
    }

    private static ListItem HourItem(WeatherFlyoutForecasts.Hour hour, WeatherFlyoutSnapshot snapshot)
    {
        var title = $"{hour.Time.ToString("ddd", CultureInfo.CurrentCulture)} {WeatherFormatter.Hour(hour.Time, snapshot.Use24HourClock)}";
        var condition = Condition(hour.Code);
        var temperature = WeatherFlyoutCards.Temperature(hour.Temperature, snapshot);
        return new ListItem(new NoOpCommand())
        {
            Title = title,
            Subtitle = $"{condition} | {temperature}",
            Icon = hour.Code.HasValue ? Icons.GetIconForWeatherCode(hour.Code.Value, hour.IsNight) : Icons.WeatherIcon,
            Details = new Details
            {
                Size = ContentSize.Large,
                Title = title,
                Body = $"{WeatherFlyoutCards.Emoji(hour.Code, hour.IsNight)} {condition}",
                Metadata =
                [
                    Fact(Resources.temperature, temperature),
                    Fact(Resources.feels_like, WeatherFlyoutCards.Temperature(hour.FeelsLike, snapshot)),
                    Fact(Resources.precipitation, WeatherFlyoutCards.Percent(hour.Rain)),
                    Fact(Resources.wind_speed, hour.Wind.HasValue ? WeatherFormatter.Wind(hour.Wind.Value, snapshot.WindUnit) : "--"),
                    Fact(Resources.humidity, WeatherFlyoutCards.Percent(hour.Humidity)),
                ],
            },
        };
    }

    private static string Condition(int? code) => code.HasValue ? Icons.GetWeatherDescription(code.Value) : Resources.unavailable;

    private static DetailsElement Fact(string key, string value) => new() { Key = key, Data = new DetailsLink(value) };

    private static ListItem Status(string text) => new(new NoOpCommand()) { Title = text, Icon = Icons.WeatherIcon };

    private void OnSnapshotChanged(object? sender, EventArgs args) => RaiseItemsChanged();

    public void Dispose() => _source.SnapshotChanged -= OnSnapshotChanged;
}
