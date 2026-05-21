using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace BaldBeardedBuilder.WeatherExtension;

internal static class WeatherLogger
{
	private static readonly object _fileSync = new();
	private static readonly string _logPath = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
		"Microsoft.CmdPal",
		"weather-extension.log");

	internal static void LogToHost(MessageState state, string message)
	{
		try
		{
			ExtensionHost.LogMessage(new LogMessage
			{
				Message = message,
				State = state,
			});
		}
		catch
		{
			// Host may not be initialized yet; the file fallback below still gets the message.
		}

		// Mirror to a local file so developers and bug reporters can inspect what
		// happened without attaching a debugger to the host.
		try
		{
			lock (_fileSync)
			{
				Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
				File.AppendAllText(_logPath, $"{DateTime.Now:O} [{state}] {message}{Environment.NewLine}");
			}
		}
		catch
		{
			// Logging is best-effort — never let it crash the extension.
		}
	}
}
