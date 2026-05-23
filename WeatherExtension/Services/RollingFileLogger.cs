// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace BaldBeardedBuilder.WeatherExtension;

internal enum LogLevel
{
	Debug,
	Info,
	Warning,
	Error,
}

/// <summary>
/// Thread-safe rolling file logger. One file per day, 7-day retention,
/// 5 MB cap per file. Zero external dependencies — AOT-safe System.IO only.
/// </summary>
internal sealed class RollingFileLogger : IDisposable
{
	private const int MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
	private const int RetentionDays = 7;

	private static readonly Lazy<RollingFileLogger> _instance =
		new(() => new RollingFileLogger(), isThreadSafe: true);

	public static RollingFileLogger Instance => _instance.Value;

	private readonly Lock _sync = new();
	private readonly string _logDirectory;

	private StreamWriter? _writer;
	private string _currentLogPath = string.Empty;
	private DateOnly _currentDate;

	public string LogDirectory => _logDirectory;

	private RollingFileLogger()
	{
		_logDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
	}

	public void Log(LogLevel level, string message)
	{
		var entry = FormatEntry(level, message, exception: null);
		WriteEntry(entry);
	}

	public void Log(LogLevel level, string message, Exception ex)
	{
		var entry = FormatEntry(level, message, ex);
		WriteEntry(entry);
	}

	/// <summary>
	/// Flushes and closes the current file handle so callers can safely
	/// zip the log directory. The next <see cref="Log"/> call reopens the file.
	/// </summary>
	public void Flush()
	{
		lock (_sync)
		{
			CloseWriter();
		}
	}

	private static string FormatEntry(LogLevel level, string message, Exception? exception)
	{
		var levelTag = level switch
		{
			LogLevel.Debug => "DEBUG",
			LogLevel.Warning => "WARN",
			LogLevel.Error => "ERROR",
			_ => "INFO",
		};

		var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", System.Globalization.CultureInfo.InvariantCulture);
		var line = $"[{timestamp}] [{levelTag}] {message}";

		if (exception != null)
		{
			line = line + Environment.NewLine + "  " + exception.ToString().Replace(Environment.NewLine, Environment.NewLine + "  ", StringComparison.Ordinal);
		}

		return line;
	}

	private void WriteEntry(string entry)
	{
		try
		{
			lock (_sync)
			{
				var today = DateOnly.FromDateTime(DateTime.UtcNow);
				EnsureWriter(today);
				_writer?.WriteLine(entry);
				_writer?.Flush();
			}
		}
		catch
		{
			// Logger failures must never crash the extension.
		}
	}

	private void EnsureWriter(DateOnly today)
	{
		// Day rolled — close current writer and clean up old files.
		if (_writer != null && today != _currentDate)
		{
			CloseWriter();
			DeleteOldLogs(today);
		}

		if (_writer != null)
		{
			// Still on the same day — check size cap.
			if (ExceedsSizeCap(_currentLogPath))
			{
				CloseWriter();
			}
			else
			{
				return;
			}
		}

		// Writer is null — open (or reopen after Flush/size-cap rotation).
		Directory.CreateDirectory(_logDirectory);

		if (today != _currentDate)
		{
			// First write of a new day — clean stale logs first.
			DeleteOldLogs(today);
			_currentDate = today;
		}

		_currentLogPath = ResolveLogPath(today);
		_writer = new StreamWriter(
			new FileStream(_currentLogPath, FileMode.Append, FileAccess.Write, FileShare.Read),
			Encoding.UTF8,
			bufferSize: 4096,
			leaveOpen: false);
	}

	private string ResolveLogPath(DateOnly date)
	{
		var dateStr = date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
		var basePath = Path.Combine(_logDirectory, $"weather-{dateStr}.log");

		if (!File.Exists(basePath) || !ExceedsSizeCap(basePath))
		{
			return basePath;
		}

		// Size cap exceeded — find next available suffix.
		for (var suffix = 1; suffix < 100; suffix++)
		{
			var rotated = Path.Combine(_logDirectory, $"weather-{dateStr}.{suffix}.log");
			if (!File.Exists(rotated) || !ExceedsSizeCap(rotated))
			{
				return rotated;
			}
		}

		// Fallback: overwrite the last suffix slot.
		return Path.Combine(_logDirectory, $"weather-{dateStr}.99.log");
	}

	private static bool ExceedsSizeCap(string path)
	{
		try
		{
			return File.Exists(path) && new FileInfo(path).Length >= MaxFileSizeBytes;
		}
		catch
		{
			return false;
		}
	}

	private void DeleteOldLogs(DateOnly today)
	{
		try
		{
			if (!Directory.Exists(_logDirectory))
			{
				return;
			}

			var cutoff = today.AddDays(-RetentionDays);
			foreach (var file in Directory.EnumerateFiles(_logDirectory, "weather-*.log"))
			{
				var name = Path.GetFileNameWithoutExtension(file);
				// Parse the date from the filename prefix "weather-yyyy-MM-dd"
				var datePart = name.Length >= 18 ? name.Substring(8, 10) : null;
				if (datePart != null &&
					DateOnly.TryParseExact(datePart, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var fileDate) &&
					fileDate < cutoff)
				{
					File.Delete(file);
				}
			}
		}
		catch
		{
			// Retention cleanup failures are non-fatal.
		}
	}

	private void CloseWriter()
	{
		try
		{
			_writer?.Flush();
			_writer?.Dispose();
		}
		catch
		{
			// Ignore disposal errors.
		}
		finally
		{
			_writer = null;
		}
	}

	/// <summary>
	/// Flushes and releases the current log file handle.
	/// Because this is a singleton that lives for the process lifetime, Dispose
	/// is effectively called only when the host tears down the extension.
	/// </summary>
	public void Dispose()
	{
		lock (_sync)
		{
			CloseWriter();
		}
	}


}
