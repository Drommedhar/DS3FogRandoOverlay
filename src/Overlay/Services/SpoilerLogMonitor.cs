using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DS3FogRandoOverlay.Services
{
    /// <summary>
    /// Service for monitoring changes to spoiler log files
    /// </summary>
    public class SpoilerLogMonitor : IDisposable
    {
        private readonly ConfigurationService configurationService;
        private readonly FogGateService fogGateService;
        private FileSystemWatcher? fileWatcher;
        private HashSet<string> knownSpoilerLogs = new HashSet<string>();
        private string? lastMonitoredDirectory;

        public event Action? SpoilerLogChanged;

        public SpoilerLogMonitor(ConfigurationService configurationService, FogGateService fogGateService)
        {
            this.configurationService = configurationService;
            this.fogGateService = fogGateService;
        }

        /// <summary>
        /// Start monitoring the spoiler logs directory for changes
        /// </summary>
        public void StartMonitoring()
        {
            try
            {
                var spoilerLogDirectory = GetSpoilerLogDirectory();
                if (string.IsNullOrEmpty(spoilerLogDirectory) || !Directory.Exists(spoilerLogDirectory))
                {
                    LogDebug($"Spoiler log directory not found or invalid: {spoilerLogDirectory}");
                    return;
                }

                // If we're already monitoring the same directory, no need to restart
                if (lastMonitoredDirectory == spoilerLogDirectory && fileWatcher != null)
                {
                    return;
                }

                // Stop existing monitoring
                StopMonitoring();

                // Initialize known spoiler logs
                RefreshKnownSpoilerLogs(spoilerLogDirectory);

                // Set up file system watcher
                fileWatcher = new FileSystemWatcher(spoilerLogDirectory, "*.txt")
                {
                    NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.FileName,
                    EnableRaisingEvents = true
                };

                fileWatcher.Created += OnSpoilerLogFileChanged;
                fileWatcher.Changed += OnSpoilerLogFileChanged;
                fileWatcher.Renamed += OnSpoilerLogFileRenamed;

                lastMonitoredDirectory = spoilerLogDirectory;
                LogDebug($"Started monitoring spoiler log directory: {spoilerLogDirectory}");
                LogDebug($"Known spoiler logs: {string.Join(", ", knownSpoilerLogs.Select(Path.GetFileName))}");
            }
            catch (Exception ex)
            {
                LogDebug($"Error starting spoiler log monitoring: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop monitoring the spoiler logs directory
        /// </summary>
        public void StopMonitoring()
        {
            if (fileWatcher != null)
            {
                fileWatcher.EnableRaisingEvents = false;
                fileWatcher.Created -= OnSpoilerLogFileChanged;
                fileWatcher.Changed -= OnSpoilerLogFileChanged;
                fileWatcher.Renamed -= OnSpoilerLogFileRenamed;
                fileWatcher.Dispose();
                fileWatcher = null;
                LogDebug("Stopped monitoring spoiler log directory");
            }
        }

        /// <summary>
        /// Check if there are new spoiler logs and trigger events if needed
        /// </summary>
        public void CheckForNewSpoilerLogs()
        {
            try
            {
                var spoilerLogDirectory = GetSpoilerLogDirectory();
                if (string.IsNullOrEmpty(spoilerLogDirectory) || !Directory.Exists(spoilerLogDirectory))
                    return;

                var currentSpoilerLogs = Directory.GetFiles(spoilerLogDirectory, "*.txt")
                    .ToHashSet();

                var newLogs = currentSpoilerLogs.Except(knownSpoilerLogs).ToList();
                
                if (newLogs.Any())
                {
                    LogDebug($"Detected {newLogs.Count} new spoiler log(s): {string.Join(", ", newLogs.Select(Path.GetFileName))}");
                    
                    // Update known logs
                    foreach (var newLog in newLogs)
                    {
                        knownSpoilerLogs.Add(newLog);
                    }

                    // Trigger event
                    SpoilerLogChanged?.Invoke();
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Error checking for new spoiler logs: {ex.Message}");
            }
        }

        private void OnSpoilerLogFileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                // Small delay to ensure file is fully written
                System.Threading.Thread.Sleep(100);

                if (!knownSpoilerLogs.Contains(e.FullPath))
                {
                    LogDebug($"New spoiler log detected: {Path.GetFileName(e.FullPath)}");
                    knownSpoilerLogs.Add(e.FullPath);
                    SpoilerLogChanged?.Invoke();
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Error handling spoiler log file change: {ex.Message}");
            }
        }

        private void OnSpoilerLogFileRenamed(object sender, RenamedEventArgs e)
        {
            try
            {
                // Remove old name, add new name
                knownSpoilerLogs.Remove(e.OldFullPath);
                
                if (!knownSpoilerLogs.Contains(e.FullPath))
                {
                    LogDebug($"Spoiler log renamed: {Path.GetFileName(e.OldName)} -> {Path.GetFileName(e.FullPath)}");
                    knownSpoilerLogs.Add(e.FullPath);
                    SpoilerLogChanged?.Invoke();
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Error handling spoiler log file rename: {ex.Message}");
            }
        }

        private void RefreshKnownSpoilerLogs(string spoilerLogDirectory)
        {
            try
            {
                knownSpoilerLogs.Clear();
                var existingLogs = Directory.GetFiles(spoilerLogDirectory, "*.txt");
                foreach (var log in existingLogs)
                {
                    knownSpoilerLogs.Add(log);
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Error refreshing known spoiler logs: {ex.Message}");
            }
        }

        private string? GetSpoilerLogDirectory()
        {
            try
            {
                var gameDirectory = configurationService.Config.DarkSouls3Path;
                if (string.IsNullOrEmpty(gameDirectory) || !Directory.Exists(gameDirectory))
                    return null;

                var parser = new DS3Parser.DS3Parser();
                var fogModDirectory = parser.FindFogRandomizerDirectory(gameDirectory);
                
                if (string.IsNullOrEmpty(fogModDirectory))
                    return null;

                return Path.Combine(fogModDirectory, "spoiler_logs");
            }
            catch (Exception ex)
            {
                LogDebug($"Error getting spoiler log directory: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Debug logging
        /// </summary>
        private void LogDebug(string message)
        {
            try
            {
                var logMessage = $"[SpoilerLogMonitor] {DateTime.Now:HH:mm:ss.fff} - {message}";
                File.AppendAllText("ds3_debug.log", logMessage + Environment.NewLine);
            }
            catch
            {
                // Ignore logging errors
            }
        }

        public void Dispose()
        {
            StopMonitoring();
        }
    }
}
