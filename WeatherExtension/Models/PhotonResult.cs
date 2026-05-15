// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Microsoft.CmdPal.Ext.Weather.Models;

internal sealed class PhotonResult
{
	[JsonPropertyName("features")]
	public List<PhotonFeature>? Features { get; set; }
}
