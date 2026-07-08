// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text.RegularExpressions;
using BaldBeardedBuilder.WeatherExtension;

namespace Microsoft.CmdPal.Ext.Weather.UnitTests;

/// <summary>
/// Tests for <see cref="RollingFileLogger"/>.
///
/// The singleton writes to a writable per-user location under
/// <c>LocalApplicationData\Microsoft.CmdPal\WeatherLogs</c>. Tests read the
/// active directory from <see cref="RollingFileLogger.LogDirectory"/> rather
/// than assuming a path. Each test flushes the logger before and after to
/// close the file handle, ensuring reads and cleanups are not blocked by an
/// open <see cref="FileStream"/>.
/// </summary>
[TestClass]
public class RollingFileLoggerTests
{
	private string _logDirectory = string.Empty;

	[TestInitialize]
	public void Setup()
	{
		// Flush any writer the previous test may have left open.
		RollingFileLogger.Instance.Flush();
		_logDirectory = RollingFileLogger.Instance.LogDirectory;
	}

	[TestCleanup]
	public void Cleanup()
	{
		// Close the writer so the test runner can clean up the output directory.
		RollingFileLogger.Instance.Flush();
	}

	// ---------------------------------------------------------------
	// Log() — file creation
	// ---------------------------------------------------------------

	[TestMethod]
	public void Log_Info_CreatesFileInLogDirectory()
	{
		RollingFileLogger.Instance.Log(LogLevel.Info, "RollingFileLoggerTests: file creation check");
		RollingFileLogger.Instance.Flush();

		var files = Directory.GetFiles(_logDirectory, "weather-*.log");
		Assert.IsTrue(files.Length > 0, "Expected at least one log file in the log directory after writing.");
	}

	[TestMethod]
	public void Log_CreatesDirectoryIfMissing()
	{
		// The logger creates the directory on first write. Because the
		// singleton may have already created it, we verify it exists rather
		// than deleting it (which would affect shared singleton state).
		RollingFileLogger.Instance.Log(LogLevel.Info, "RollingFileLoggerTests: directory creation check");
		RollingFileLogger.Instance.Flush();

		Assert.IsTrue(Directory.Exists(_logDirectory),
			$"Log directory should exist after writing: {_logDirectory}");
	}

	// ---------------------------------------------------------------
	// Log() — entry format
	// ---------------------------------------------------------------

	[DataTestMethod]
	[DataRow(0, "DEBUG")]  // LogLevel.Debug
	[DataRow(1, "INFO")]   // LogLevel.Info
	[DataRow(2, "WARN")]   // LogLevel.Warning
	[DataRow(3, "ERROR")]  // LogLevel.Error
	public void Log_EntryFormat_ContainsTimestampLevelAndMessage(int levelInt, string expectedTag)
	{
		// LogLevel is internal; use int parameter to avoid CS0051 accessibility mismatch.
		var level = (LogLevel)levelInt;
		var marker = $"format-check-{Guid.NewGuid()}";

		RollingFileLogger.Instance.Log(level, marker);
		RollingFileLogger.Instance.Flush();

		var logFile = LatestLogFile();
		Assert.IsNotNull(logFile, "No log file found after writing.");

		var content = File.ReadAllText(logFile!);
		Assert.IsTrue(content.Contains(marker, StringComparison.Ordinal),
			$"Log entry must contain the original message. Content:\n{content}");
		Assert.IsTrue(content.Contains($"[{expectedTag}]", StringComparison.Ordinal),
			$"Log entry must contain [{expectedTag}] level tag. Content:\n{content}");

		// Verify ISO-8601 timestamp pattern: [yyyy-MM-ddTHH:mm:ss.fffZ]
		Assert.IsTrue(
			Regex.IsMatch(content, @"\[\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z\]"),
			$"Log entry must contain an ISO-8601 timestamp. Content:\n{content}");
	}

	[TestMethod]
	public void Log_WithException_AppendsSeparateExceptionLine()
	{
		var marker = $"exception-check-{Guid.NewGuid()}";
		var ex = new InvalidOperationException("test-exception-message");

		RollingFileLogger.Instance.Log(LogLevel.Error, marker, ex);
		RollingFileLogger.Instance.Flush();

		var logFile = LatestLogFile();
		Assert.IsNotNull(logFile, "No log file found after writing.");

		var content = File.ReadAllText(logFile!);
		Assert.IsTrue(content.Contains("test-exception-message", StringComparison.Ordinal),
			"Log file should contain the exception message.");
		Assert.IsTrue(content.Contains(nameof(InvalidOperationException), StringComparison.Ordinal),
			"Log file should contain the exception type name.");
	}

	// ---------------------------------------------------------------
	// Flush() — releases the file handle
	// ---------------------------------------------------------------

	[TestMethod]
	public void Flush_AllowsFileToBeReadWithoutLock()
	{
		RollingFileLogger.Instance.Log(LogLevel.Info, $"flush-test-{Guid.NewGuid()}");
		RollingFileLogger.Instance.Flush();

		var logFile = LatestLogFile();
		Assert.IsNotNull(logFile, "No log file found after writing.");

		// After Flush(), the writer is closed. We should be able to open
		// the file with exclusive access (simulating what ZipFile does).
		using var fs = new FileStream(logFile!, FileMode.Open, FileAccess.Read, FileShare.None);
		Assert.IsTrue(fs.Length > 0, "Log file should be non-empty after writing.");
	}

	[TestMethod]
	public void Log_AfterFlush_ReopensAndContinuesWriting()
	{
		var marker1 = $"before-flush-{Guid.NewGuid()}";
		var marker2 = $"after-flush-{Guid.NewGuid()}";

		RollingFileLogger.Instance.Log(LogLevel.Info, marker1);
		RollingFileLogger.Instance.Flush();

		// Log again after Flush() — the logger must reopen the file.
		RollingFileLogger.Instance.Log(LogLevel.Info, marker2);
		RollingFileLogger.Instance.Flush();

		var logFile = LatestLogFile();
		Assert.IsNotNull(logFile, "No log file found.");

		var content = File.ReadAllText(logFile!);
		Assert.IsTrue(content.Contains(marker1, StringComparison.Ordinal),
			"Entry written before Flush() must be present.");
		Assert.IsTrue(content.Contains(marker2, StringComparison.Ordinal),
			"Entry written after Flush() must also be present — logger must reopen the file.");
	}

	// ---------------------------------------------------------------
	// Concurrent writes — no exceptions or torn entries
	// ---------------------------------------------------------------

	[TestMethod]
	public void Log_ConcurrentWrites_DoNotThrow()
	{
		const int threadCount = 20;
		const int writesPerThread = 50;
		var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

		Parallel.For(0, threadCount, i =>
		{
			for (var j = 0; j < writesPerThread; j++)
			{
				try
				{
					RollingFileLogger.Instance.Log(LogLevel.Debug,
						$"concurrent-{i}-{j}-{Guid.NewGuid()}");
				}
				catch (Exception ex)
				{
					exceptions.Add(ex);
				}
			}
		});

		RollingFileLogger.Instance.Flush();

		Assert.AreEqual(0, exceptions.Count,
			$"No exceptions should be thrown during concurrent writes. Got: {string.Join(", ", exceptions.Select(e => e.Message))}");
	}

	[TestMethod]
	public void Log_ConcurrentWrites_AllEntriesPresent()
	{
		const int entryCount = 100;
		var markers = Enumerable.Range(0, entryCount)
			.Select(_ => Guid.NewGuid().ToString("N"))
			.ToArray();

		Parallel.ForEach(markers, marker =>
			RollingFileLogger.Instance.Log(LogLevel.Info, $"concurrent-marker-{marker}"));

		RollingFileLogger.Instance.Flush();

		var logFile = LatestLogFile();
		Assert.IsNotNull(logFile, "No log file found.");

		var content = File.ReadAllText(logFile!);
		var missing = markers.Where(m => !content.Contains(m, StringComparison.Ordinal)).ToList();

		Assert.AreEqual(0, missing.Count,
			$"All {entryCount} markers must be present in the log. Missing: {string.Join(", ", missing.Take(5))}");
	}

	// ---------------------------------------------------------------
	// LogDirectory property
	// ---------------------------------------------------------------

	[TestMethod]
	public void LogDirectory_EndsWithLogsSegment()
	{
		var dir = RollingFileLogger.Instance.LogDirectory;

		Assert.IsTrue(
			dir.EndsWith("Logs", StringComparison.OrdinalIgnoreCase) ||
			dir.EndsWith("Logs" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase),
			$"LogDirectory should end with 'Logs'. Got: {dir}");
	}

	// ---------------------------------------------------------------
	// Helpers
	// ---------------------------------------------------------------

	/// <summary>Returns the most-recently-modified weather log file, or null.</summary>
	private string? LatestLogFile()
	{
		if (!Directory.Exists(_logDirectory))
		{
			return null;
		}

		return Directory
			.GetFiles(_logDirectory, "weather-*.log")
			.OrderByDescending(File.GetLastWriteTimeUtc)
			.FirstOrDefault();
	}
}
