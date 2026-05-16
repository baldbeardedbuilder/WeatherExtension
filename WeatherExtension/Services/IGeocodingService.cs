// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CmdPal.Ext.Weather.Models;
using System.Threading;

namespace Microsoft.CmdPal.Ext.Weather.Services;

public interface IGeocodingService : IDisposable
{
    Task<List<GeocodingResult>> SearchLocationAsync(string query, CancellationToken ct = default);
}
