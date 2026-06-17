// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO.Pipes;
using System.Threading;
using BaldBeardedBuilder.WeatherExtension;
using Microsoft.CommandPalette.Extensions;

namespace WeatherExtension;

/// <summary>
/// Coordinates graceful shutdown of the extension via named pipes or named events.
/// Allows external processes (e.g., uninstaller) to signal shutdown without
/// relying on Command Palette's disposal chain.
/// </summary>
internal sealed class ShutdownCoordinator : IDisposable
{
	private readonly ManualResetEvent _shutdownSignal = new(false);
	private readonly CancellationTokenSource _listenerCts = new();
	private Thread? _listenerThread;

	/// <summary>
	/// Gets a WaitHandle that signals when external shutdown is requested.
	/// </summary>
	public WaitHandle ShutdownHandle => _shutdownSignal;

	/// <summary>
	/// Initializes the coordinator and starts listening for shutdown signals
	/// via named pipe and named event.
	/// </summary>
	public ShutdownCoordinator()
	{
		_listenerThread = new Thread(ListenForShutdownAsync)
		{
			Name = "WeatherExtension-ShutdownListener",
			IsBackground = true,
		};

		_listenerThread.Start();
	}

	private void ListenForShutdownAsync()
	{
		// Try the primary shutdown method: named pipe
		if (TryNamedPipeShutdown())
		{
			_shutdownSignal.Set();
			return;
		}

		// Fall back to named event if named pipe fails
		if (TryNamedEventShutdown())
		{
			_shutdownSignal.Set();
			return;
		}

		// If both fail, log the error but continue — graceful degradation
		// allows the extension to keep running if shutdown signaling is unavailable.
	}

	/// <summary>
	/// Attempts to listen on a named pipe for shutdown signals.
	/// Returns true if shutdown was signaled; false if the operation failed or was cancelled.
	/// </summary>
	private static bool TryNamedPipeShutdown()
	{
		try
		{
			string pipeName = "WeatherExtension-Shutdown";

			using var pipeServer = new NamedPipeServerStream(
				pipeName,
				PipeDirection.In,
				1,
				PipeTransmissionMode.Message,
				PipeOptions.None);

			pipeServer.WaitForConnection();

			// Any data on the pipe triggers shutdown
			byte[] buffer = new byte[1];
			int bytesRead = pipeServer.Read(buffer, 0, 1);

			return bytesRead > 0;
		}
		catch (OperationCanceledException)
		{
			// Graceful cancellation during listener shutdown
			return false;
		}
		catch (IOException ex)
		{
			// Pipe-related errors: already exists, access denied, etc.
			WeatherLogger.LogToHost(
				MessageState.Info,
				$"Named pipe shutdown listener failed: {ex.Message}");
			return false;
		}
		catch (UnauthorizedAccessException ex)
		{
			// Permissions issue
			WeatherLogger.LogToHost(
				MessageState.Info,
				$"Named pipe shutdown listener denied: {ex.Message}");
			return false;
		}
		catch (Exception ex)
		{
			// Unexpected error
			WeatherLogger.LogToHost(
				MessageState.Error,
				$"Unexpected error in named pipe shutdown listener: {ex.Message}");
			return false;
		}
	}

	/// <summary>
	/// Attempts to listen on a named event for shutdown signals.
	/// Returns true if shutdown was signaled; false if the operation failed or was cancelled.
	/// </summary>
	private bool TryNamedEventShutdown()
	{
		try
		{
			string eventName = "WeatherExtension-Shutdown";

			// Try to open an existing event (created by external process or previous instance)
			// If it doesn't exist, this creates a new one with initial state "not set"
			using var shutdownEvent = new EventWaitHandle(
				false,
				EventResetMode.AutoReset,
				eventName,
				out bool createdNew);

			// If we created this event, no one will signal it from outside.
			// Wait anyway in case another process signals it later.
			int index = WaitHandle.WaitAny(new[] { shutdownEvent, _listenerCts.Token.WaitHandle });
			return index == 0; // 0 means the event was signaled; 1 means cancellation
		}
		catch (UnauthorizedAccessException ex)
		{
			// Permissions issue (e.g., event exists but we can't access it)
			WeatherLogger.LogToHost(
				MessageState.Info,
				$"Named event shutdown listener denied: {ex.Message}");
			return false;
		}
		catch (Exception ex)
		{
			// Unexpected error
			WeatherLogger.LogToHost(
				MessageState.Error,
				$"Unexpected error in named event shutdown listener: {ex.Message}");
			return false;
		}
	}

	/// <summary>
	/// Stops the listener thread and releases resources.
	/// </summary>
	public void Dispose()
	{
		_listenerCts.Cancel();
		_listenerCts.Dispose();

		if (_listenerThread?.IsAlive == true)
		{
			_listenerThread.Join(timeout: TimeSpan.FromSeconds(2));
		}

		_shutdownSignal.Dispose();
	}
}
