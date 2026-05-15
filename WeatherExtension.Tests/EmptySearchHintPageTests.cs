// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BaldBeardedBuilder.WeatherExtension;
using Microsoft.CmdPal.Ext.Weather.Pages;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Microsoft.CmdPal.Ext.Weather.UnitTests;

/// <summary>
/// Tests for <see cref="EmptySearchHintPage"/>, the ContentPage shown in
/// WeatherListPage.EmptyContent when a search returns no results.
/// Covers issue #57.
/// </summary>
[TestClass]
public class EmptySearchHintPageTests
{
    // ---------------------------------------------------------------
    // Metadata
    // ---------------------------------------------------------------

    [TestMethod]
    public void Name_IsNoLocationsFound()
    {
        var page = new EmptySearchHintPage();

        Assert.AreEqual(Resources.no_locations_found, page.Name);
    }

    [TestMethod]
    public void Title_IsNoLocationsFound()
    {
        var page = new EmptySearchHintPage();

        Assert.AreEqual(Resources.no_locations_found, page.Title);
    }

    [TestMethod]
    public void Icon_IsNotNull()
    {
        var page = new EmptySearchHintPage();

        Assert.IsNotNull(page.Icon);
    }

    // ---------------------------------------------------------------
    // GetContent() contract
    // ---------------------------------------------------------------

    [TestMethod]
    public void GetContent_ReturnsNonNullArray()
    {
        var page = new EmptySearchHintPage();

        var content = page.GetContent();

        Assert.IsNotNull(content);
    }

    [TestMethod]
    public void GetContent_ReturnsExactlyOneItem()
    {
        var page = new EmptySearchHintPage();

        var content = page.GetContent();

        Assert.AreEqual(1, content.Length,
            "EmptySearchHintPage should return exactly one IContent item");
    }

    [TestMethod]
    public void GetContent_FirstItemIsMarkdownContent()
    {
        var page = new EmptySearchHintPage();

        var content = page.GetContent();

        Assert.IsInstanceOfType<MarkdownContent>(content[0],
            "Content item must be MarkdownContent");
    }

    [TestMethod]
    public void GetContent_BodyContainsNoLocationsFoundText()
    {
        var page = new EmptySearchHintPage();
        var markdown = (MarkdownContent)page.GetContent()[0];

        StringAssert.Contains(markdown.Body, Resources.no_locations_found,
            "Markdown body must include the 'no locations found' heading");
    }

    [TestMethod]
    public void GetContent_BodyContainsSearchFormatHint()
    {
        var page = new EmptySearchHintPage();
        var markdown = (MarkdownContent)page.GetContent()[0];

        StringAssert.Contains(markdown.Body, Resources.search_format_hint,
            "Markdown body must include the search format hint");
    }

    [TestMethod]
    public void GetContent_BodyIsNotEmpty()
    {
        var page = new EmptySearchHintPage();
        var markdown = (MarkdownContent)page.GetContent()[0];

        Assert.IsFalse(string.IsNullOrWhiteSpace(markdown.Body),
            "Markdown body must not be empty");
    }

    // ---------------------------------------------------------------
    // Stability: calling GetContent() multiple times returns consistent results
    // ---------------------------------------------------------------

    [TestMethod]
    public void GetContent_CalledTwice_ReturnsSameBody()
    {
        var page = new EmptySearchHintPage();

        var body1 = ((MarkdownContent)page.GetContent()[0]).Body;
        var body2 = ((MarkdownContent)page.GetContent()[0]).Body;

        Assert.AreEqual(body1, body2,
            "GetContent() must be stable across multiple calls");
    }
}
