// Copyright (c) Bald Bearded Builder LLC
// Licensed under the MIT license. See LICENSE in the project root.

using System.Text.Json.Nodes;

namespace Microsoft.CmdPal.Ext.Weather.Services;

internal static class FlyoutCard
{
    // Keep these as JsonNode. Add<T> needs runtime serialization
    // that Native AOT can't provide.
    public static JsonNode Text(string text, string size = "default", bool subtle = false) => new JsonObject()
    {
        ["type"] = "TextBlock", ["text"] = text, ["wrap"] = true, ["size"] = size, ["isSubtle"] = subtle,
    };

    public static JsonNode Column(params JsonNode[] items) => new JsonObject()
    {
        ["type"] = "Column", ["width"] = "stretch", ["items"] = new JsonArray(items),
    };

    public static JsonNode Columns(params JsonNode[] columns) => new JsonObject()
    {
        ["type"] = "ColumnSet", ["columns"] = new JsonArray(columns),
    };

    public static JsonNode Image(string url, string alt) => new JsonObject()
    {
        ["type"] = "Image", ["url"] = url, ["altText"] = alt, ["size"] = "stretch",
    };

    public static string Document(JsonArray body, JsonArray? actions = null) => new JsonObject
    {
        ["type"] = "AdaptiveCard", ["$schema"] = "http://adaptivecards.io/schemas/adaptive-card.json",
        ["version"] = "1.5", ["body"] = body, ["actions"] = actions ?? new JsonArray(),
    }.ToJsonString();
}
