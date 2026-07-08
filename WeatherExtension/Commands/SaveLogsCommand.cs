// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO.Compression;
using BaldBeardedBuilder.WeatherExtension;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Microsoft.CmdPal.Ext.Weather.Commands;

internal sealed partial class SaveLogsCommand : InvokableCommand
{
    public SaveLogsCommand()
    {
        Name = Resources.bug_report_save_logs;
        Icon = new IconInfo("\uE74E"); // Save icon
    }

    public override ICommandResult Invoke()
    {
        try
        {
            RollingFileLogger.Instance.Flush();

            var logDir = RollingFileLogger.Instance.LogDirectory;

            // Make sure the directory exists even if nothing has been logged
            // yet, so we always produce a zip for the user to attach.
            Directory.CreateDirectory(logDir);

            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (string.IsNullOrEmpty(desktop))
            {
                // Desktop can be empty/redirected in some profiles — fall back
                // to the user profile so the zip still lands somewhere findable.
                desktop = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }

            var zipName = $"WeatherExtension-Logs-{DateTime.Now:yyyy-MM-dd}.zip";
            var zipPath = Path.Combine(desktop, zipName);

            // Remove any existing zip with the same name so CreateFromDirectory doesn't throw.
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            // Always create the archive — add only *.log files, skipping any
            // unrelated files that may be in the directory.
            var fileCount = 0;
            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                foreach (var logFile in Directory.EnumerateFiles(logDir, "*.log"))
                {
                    archive.CreateEntryFromFile(logFile, Path.GetFileName(logFile), CompressionLevel.Optimal);
                    fileCount++;
                }
            }

            WeatherLogger.LogToHost(MessageState.Info, $"Logs saved to: {zipPath} ({fileCount} file(s))");

            // Surface a visible confirmation — previously this command gave no
            // feedback, so users assumed nothing happened.
            return CommandResult.ShowToast(Resources.bug_report_logs_saved);
        }
        catch (Exception ex)
        {
            WeatherLogger.LogToHost(MessageState.Error, $"SaveLogsCommand failed: {ex.Message}");
            return CommandResult.ShowToast(Resources.bug_report_logs_save_failed);
        }
    }
}
