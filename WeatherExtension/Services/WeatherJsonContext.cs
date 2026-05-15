// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Microsoft.CmdPal.Ext.Weather.Models;

namespace Microsoft.CmdPal.Ext.Weather.Services;

[JsonSerializable(typeof(WeatherData))]
[JsonSerializable(typeof(ForecastData))]
[JsonSerializable(typeof(HourlyForecastData))]
[JsonSerializable(typeof(List<NominatimResult>))]
[JsonSerializable(typeof(NominatimAddress))]
[JsonSerializable(typeof(List<PinnedLocation>))]
[JsonSerializable(typeof(PhotonResult))]
[JsonSerializable(typeof(PhotonFeature))]
[JsonSerializable(typeof(List<PhotonFeature>))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
internal partial class WeatherJsonContext : JsonSerializerContext
{
}
