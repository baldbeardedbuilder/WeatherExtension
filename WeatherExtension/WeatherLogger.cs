using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.IO;
using System.Threading;

namespace BaldBeardedBuilder.WeatherExtension;

internal static class WeatherLogger
{
	private static readonly string _logPath;
	private static readonly Lock _fileLock = new();

	static WeatherLogger()
	{
		var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		var dir = Path.Combine(localAppData, "Microsoft.CmdPal");
		Directory.CreateDirectory(dir);
		_logPath = Path.Combine(dir, "weather-debug.log");

		// Truncate on startup so the file stays manageable
		try { File.WriteAllText(_logPath, $"=== Weather Extension Debug Log — {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n"); }
		catch { /* best effort */ }
	}

	internal static void LogToHost(MessageState state, string message)
	{
		ExtensionHost.LogMessage(new LogMessage
		{
			Message = message,
			State = state,
		});
	}

	/// <summary>
	/// Logs to both the host and a local file for dock band debugging.
	/// </summary>
	internal static void Debug(string message)
	{
		var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";

		// Host log
		try
		{
			ExtensionHost.LogMessage(new LogMessage
			{
				Message = line,
				State = MessageState.Info,
			});
		}
		catch { /* host may not be ready */ }

		// File log
		lock (_fileLock)
		{
			try { File.AppendAllText(_logPath, line + "\n"); }
			catch { /* best effort */ }
		}
	}

	internal static string LogFilePath => _logPath;
}
