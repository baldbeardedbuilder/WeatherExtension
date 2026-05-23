// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection;
using BaldBeardedBuilder.WeatherExtension;
using Microsoft.CmdPal.Ext.Weather.Commands;
using Microsoft.CmdPal.Ext.Weather.Models;
using Microsoft.CmdPal.Ext.Weather.Pages;
using Microsoft.CmdPal.Ext.Weather.Services;
using Microsoft.CmdPal.Ext.Weather.UnitTests.TestDoubles;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Microsoft.CmdPal.Ext.Weather.UnitTests;

[TestClass]
public class WeatherListPageTests
{
	private static readonly BindingFlags InstanceAny =
		BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	private string _settingsPath = string.Empty;
	private string _favoritesPath = string.Empty;
	

	[TestInitialize]
	public void Setup()
	{
		_settingsPath = Path.Combine(Path.GetTempPath(), $"weather-list-settings-{Guid.NewGuid()}.json");
		_favoritesPath = Path.Combine(Path.GetTempPath(), $"weather-list-fav-{Guid.NewGuid()}.json");
	}

	[TestCleanup]
	public void Cleanup()
	{
		foreach (var path in new[] { _settingsPath, _favoritesPath })
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
	}

	[TestMethod]
	public async Task GetItems_WithFavorite_LoadsWeatherRow()
	{
		var favorites = new FavoritesManager(_favoritesPath);
		favorites.Favorite(new GeocodingResult
		{
			Name = "Istanbul",
			Latitude = 41.0082,
			Longitude = 28.9784,
			Admin1 = "Istanbul",
			Country = "Turkey",
		});

		using var page = CreatePage(favorites);

		var items = await AsyncTestHelper.WaitUntilAsync(
			() => page.GetItems(),
			items => items.Length == 1 && items[0].Title?.Contains("Istanbul", StringComparison.Ordinal) == true);

		Assert.IsFalse(string.IsNullOrWhiteSpace(items[0].Subtitle));
	}

	[TestMethod]
	public async Task UpdateSearchText_ValidQuery_ShowsSearchResults()
	{
		var geo = new StubGeocodingService();
		using var page = CreatePage(geocoding: geo);

		await WaitForInitialLoadAsync(page);

		page.UpdateSearchText(string.Empty, "istanbul");

		var items = await AsyncTestHelper.WaitUntilAsync(
			() => page.GetItems(),
			items => items.Length > 0 && items[0].Title?.Contains("Istanbul", StringComparison.Ordinal) == true,
			timeoutMs: 8000);

		Assert.IsTrue(geo.SearchCallCount >= 1);
	}

	[TestMethod]
	public async Task UpdateSearchText_ShortQuery_SetsMinCharsEmptyContent()
	{
		using var page = CreatePage();

		await WaitForInitialLoadAsync(page);

		page.UpdateSearchText(string.Empty, "ab");

		var empty = await AsyncTestHelper.WaitUntilAsync(
			() => GetEmptyContent(page),
			item => item != null && item.Title == Resources.search_min_chars,
			timeoutMs: 8000);

		StringAssert.Contains(empty!.Subtitle!, Resources.search_hint_examples_title);
		var firstExample = Resources.search_hint_examples_block
			.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0];
		StringAssert.Contains(empty.Subtitle!, firstExample);
	}

	[TestMethod]
	public async Task FavoriteFromSearch_ThenUnfavorite_UpdatesListState()
	{
		var favorites = new FavoritesManager(_favoritesPath);
		var geo = new StubGeocodingService();
		using var page = CreatePage(favorites, geo);

		await WaitForInitialLoadAsync(page);

		page.UpdateSearchText(string.Empty, "istanbul");

		await AsyncTestHelper.WaitUntilAsync(
			() => page.GetItems(),
			items => items.Length > 0,
			timeoutMs: 8000);

		var location = geo.Results[0];
		new FavoriteLocationCommand(location, favorites).Invoke();
		page.RefreshWeather();

		await AsyncTestHelper.WaitUntilAsync(
			() => page.GetItems(),
			items => items.Any(i => i.Tags?.Length > 0),
			timeoutMs: 8000);

		Assert.IsTrue(favorites.IsFavorite(location));

		new UnfavoriteLocationCommand(location, favorites).Invoke();
		page.RefreshWeather();

		await AsyncTestHelper.WaitUntilAsync(
			() => page.GetItems(),
			items => items.All(i => i.Tags == null || i.Tags.Length == 0),
			timeoutMs: 8000);

		Assert.IsFalse(favorites.IsFavorite(location));
	}

	private static async Task WaitForInitialLoadAsync(WeatherListPage page)
	{
		await AsyncTestHelper.WaitUntilAsync(
			() => page.GetItems(),
			_ => true,
			timeoutMs: 8000);
	}

	private static ListItem? GetEmptyContent(WeatherListPage page)
	{
		var property = typeof(DynamicListPage).GetProperty("EmptyContent", InstanceAny);
		return property?.GetValue(page) as ListItem;
	}

	private WeatherListPage CreatePage(
		FavoritesManager? favorites = null,
		StubGeocodingService? geocoding = null)
	{
		var settingsManager = new WeatherSettingsManager(_settingsPath);
		return new WeatherListPage(
			new StubWeatherService(),
			geocoding ?? new StubGeocodingService(),
			settingsManager,
			favorites ?? new FavoritesManager(_favoritesPath),
			new WeatherSettingsPage(settingsManager));
	}
}
