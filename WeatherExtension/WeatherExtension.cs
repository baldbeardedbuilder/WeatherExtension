// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Threading;
using BaldBeardedBuilder.WeatherExtension;
using Microsoft.CommandPalette.Extensions;

namespace WeatherExtension;

[Guid("151e3c6b-4e4d-488a-8228-7e58938bfc57")]
public sealed partial class WeatherExtension : IExtension, IDisposable
{
    private readonly ManualResetEvent _extensionDisposedEvent;
    private readonly WeatherCommandsProvider _provider = new();
    private bool _isDisposed;
    private readonly object _disposeLock = new();

    public WeatherExtension(ManualResetEvent extensionDisposedEvent)
    {
        this._extensionDisposedEvent = extensionDisposedEvent;
    }

    public object? GetProvider(ProviderType providerType)
    {
        return providerType switch
        {
            ProviderType.Commands => _provider,
            _ => null,
        };
    }

    /// <summary>
    /// Disposes the extension and signals completion.
    /// Thread-safe and idempotent: can be called multiple times from different threads.
    /// </summary>
    public void Dispose()
    {
        lock (_disposeLock)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
        }

        try
        {
            _provider?.Dispose();
        }
        catch (Exception ex)
        {
            WeatherLogger.LogToHost(
                MessageState.Error,
                $"Error during provider disposal: {ex.Message}");
        }

        try
        {
            this._extensionDisposedEvent.Set();
        }
        catch (Exception ex)
        {
            WeatherLogger.LogToHost(
                MessageState.Error,
                $"Error setting disposal event: {ex.Message}");
        }
    }
}
