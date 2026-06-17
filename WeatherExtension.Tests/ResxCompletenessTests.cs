// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.CmdPal.Ext.Weather.UnitTests;

/// <summary>
/// Verifies that every key in the English baseline <c>Resources.resx</c>
/// is present in every satellite locale <c>Resources.XX.resx</c> file.
/// Missing keys silently fall back to English at runtime — these tests make
/// that visible before it ships.
/// </summary>
[TestClass]
public class ResxCompletenessTests
{
    private static readonly string PropertiesDir = FindPropertiesDirectory();
    private static readonly string BaselineFile = Path.Combine(PropertiesDir, "Resources.resx");

    /// <summary>
    /// Walks up from AppContext.BaseDirectory until it finds WeatherExtension/Properties.
    /// This is more robust than a fixed relative path depth.
    /// </summary>
    private static string FindPropertiesDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, "WeatherExtension", "Properties");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not find WeatherExtension/Properties directory by walking up from {AppContext.BaseDirectory}");
    }

    private static IReadOnlyList<string> ReadKeys(string resxPath)
    {
        var doc = XDocument.Load(resxPath);
        return doc.Root!
            .Elements("data")
            .Select(e => e.Attribute("name")!.Value)
            .ToList();
    }

    [TestMethod]
    public void AllLocaleFiles_ContainEveryBaselineKey()
    {
        Assert.IsTrue(File.Exists(BaselineFile),
            $"English baseline not found at: {BaselineFile}");

        var baselineKeys = ReadKeys(BaselineFile);
        Assert.IsTrue(baselineKeys.Count > 0, "Baseline resx has no data keys — check the file path.");

        var localeFiles = Directory.GetFiles(PropertiesDir, "Resources.*.resx")
                                   .OrderBy(f => f)
                                   .ToList();

        Assert.IsTrue(localeFiles.Count > 0,
            $"No locale resx files found in: {PropertiesDir}");

        var failures = new List<string>();

        foreach (var localeFile in localeFiles)
        {
            var localeName = Path.GetFileNameWithoutExtension(localeFile)
                                 .Substring("Resources.".Length); // e.g. "ar", "zh-Hans"
            var localeKeys = new HashSet<string>(ReadKeys(localeFile));

            var missing = baselineKeys.Where(k => !localeKeys.Contains(k)).ToList();
            if (missing.Count > 0)
            {
                failures.Add(
                    $"\n  [{localeName}] missing {missing.Count} key(s): {string.Join(", ", missing)}");
            }
        }

        Assert.AreEqual(0, failures.Count,
            $"The following locale files are missing keys from the English baseline:{string.Join(string.Empty, failures)}\n\n" +
            $"Add the missing translations or placeholder values to each affected .resx file.");
    }

    [TestMethod]
    public void BaselineFile_IsReachableFromTestOutput()
    {
        Assert.IsTrue(File.Exists(BaselineFile),
            $"Could not find Resources.resx at expected path: {BaselineFile}\n" +
            $"AppContext.BaseDirectory = {AppContext.BaseDirectory}");
    }

    [TestMethod]
    public void LocaleFiles_ArePresent()
    {
        var localeFiles = Directory.GetFiles(PropertiesDir, "Resources.*.resx");
        Assert.IsTrue(localeFiles.Length >= 13,
            $"Expected at least 13 locale files but found {localeFiles.Length} in {PropertiesDir}");
    }
}
