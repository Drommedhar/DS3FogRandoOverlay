using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace DS3FogRandoOverlay.Services
{
    /// <summary>
    /// Service for parsing events.txt file to extract fog gate names and descriptions
    /// </summary>
    public class EventsParser
    {
        private readonly Dictionary<string, string> _fogGateNames = new(); // Key: fog gate ID, Value: description
        private readonly Dictionary<int, string> _entityIdToFogGateNames = new(); // Key: entity ID, Value: description
        private readonly string? _eventsFilePath;
        private readonly string _darkSouls3Path;
        private bool _isLoaded = false;

        public EventsParser()
        {
            // Try to auto-detect Dark Souls 3 path
            _darkSouls3Path = PathResolver.AutoDetectDarkSouls3Path() ?? @"C:\Program Files (x86)\Steam\steamapps\common\DARK SOULS III";
            _eventsFilePath = ResolveEventsPath();
        }

        public EventsParser(string darkSouls3Path)
        {
            _darkSouls3Path = darkSouls3Path ?? throw new ArgumentNullException(nameof(darkSouls3Path));
            _eventsFilePath = ResolveEventsPath();
        }

        public EventsParser(string darkSouls3Path, string eventsFilePath)
        {
            _darkSouls3Path = darkSouls3Path ?? throw new ArgumentNullException(nameof(darkSouls3Path));
            _eventsFilePath = eventsFilePath ?? throw new ArgumentNullException(nameof(eventsFilePath));
        }

        /// <summary>
        /// Get the resolved path to the events.txt file
        /// </summary>
        public string? EventsFilePath => _eventsFilePath;

        /// <summary>
        /// Resolve the path to events.txt using PathResolver
        /// </summary>
        private string? ResolveEventsPath()
        {
            try
            {
                var pathResolver = new PathResolver(_darkSouls3Path);
                var eventsPath = pathResolver.FindEventsFile();
                
                if (!string.IsNullOrEmpty(eventsPath))
                {
                    File.AppendAllText("ds3_debug.log",
                        $"[EventsParser] {DateTime.Now:HH:mm:ss.fff} - Resolved events.txt path: {eventsPath}\n");
                    return eventsPath;
                }
                else
                {
                    File.AppendAllText("ds3_debug.log",
                        $"[EventsParser] {DateTime.Now:HH:mm:ss.fff} - Could not resolve events.txt path for DS3 path: {_darkSouls3Path}\n");
                    return null;
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("ds3_debug.log",
                    $"[EventsParser] {DateTime.Now:HH:mm:ss.fff} - Error resolving events.txt path: {ex.Message}\n");
                return null;
            }
        }

        /// <summary>
        /// Get the display name for a fog gate ID. Returns the ID if no name is found.
        /// </summary>
        /// <param name="fogGateId">The fog gate ID (e.g., "o000401_4000")</param>
        /// <returns>The descriptive name of the fog gate or the original ID if not found</returns>
        public string GetFogGateName(string fogGateId)
        {
            if (!_isLoaded)
            {
                LoadFogGateNames();
            }

            return _fogGateNames.GetValueOrDefault(fogGateId, fogGateId);
        }

        /// <summary>
        /// Get the display name for a fog gate using entity ID. Returns the fog gate ID if no name is found.
        /// </summary>
        /// <param name="entityId">The entity ID from MSB (e.g., 3001890)</param>
        /// <param name="fogGateId">The fog gate ID as fallback (e.g., "o000401_4000")</param>
        /// <returns>The descriptive name of the fog gate or the original fog gate ID if not found</returns>
        public string GetFogGateNameByEntityId(int entityId, string fogGateId)
        {
            if (!_isLoaded)
            {
                LoadFogGateNames();
            }

            // First try to get name by entity ID (most specific)
            if (_entityIdToFogGateNames.TryGetValue(entityId, out var nameByEntityId))
            {
                return nameByEntityId;
            }

            // Fallback to fog gate ID lookup
            return _fogGateNames.GetValueOrDefault(fogGateId, fogGateId);
        }

        /// <summary>
        /// Load fog gate names from the events.txt file
        /// </summary>
        private void LoadFogGateNames()
        {
            _fogGateNames.Clear();
            _entityIdToFogGateNames.Clear();

            try
            {
                if (string.IsNullOrEmpty(_eventsFilePath) || !File.Exists(_eventsFilePath))
                {
                    System.IO.File.AppendAllText("ds3_debug.log",
                        $"[EventsParser] {DateTime.Now:HH:mm:ss.fff} - Events file not found: {_eventsFilePath ?? "null"}\n");
                    _isLoaded = true;
                    return;
                }

                var lines = File.ReadAllLines(_eventsFilePath);
                
                // Regex pattern to match lines like:
                // - 3001890 - highwall object o000401_4000 - fog gate between Emma's Room and High Wall
                var pattern = @"^\s*-\s+(\d+)\s+-\s+\w+\s+object\s+(o\d+_\d+)\s+-\s+fog gate\s+(.+)$";
                var regex = new Regex(pattern, RegexOptions.IgnoreCase);

                int matchCount = 0;
                foreach (var line in lines)
                {
                    var match = regex.Match(line);
                    if (match.Success)
                    {
                        var entityIdStr = match.Groups[1].Value;
                        var fogGateId = match.Groups[2].Value;
                        var description = match.Groups[3].Value.Trim();
                        
                        if (int.TryParse(entityIdStr, out var entityId))
                        {
                            // Store both mappings
                            _entityIdToFogGateNames[entityId] = description;
                            
                            // Also store by fog gate ID for fallback (but this might be overwritten by duplicates)
                            _fogGateNames[fogGateId] = description;
                            
                            matchCount++;
                        }
                    }
                }

                System.IO.File.AppendAllText("ds3_debug.log",
                    $"[EventsParser] {DateTime.Now:HH:mm:ss.fff} - Loaded {matchCount} fog gate names from events.txt\n");
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText("ds3_debug.log",
                    $"[EventsParser] {DateTime.Now:HH:mm:ss.fff} - Error loading events.txt: {ex.Message}\n");
            }

            _isLoaded = true;
        }

        /// <summary>
        /// Reload fog gate names from the events.txt file
        /// </summary>
        public void Reload()
        {
            _isLoaded = false;
            LoadFogGateNames();
        }

        /// <summary>
        /// Get all loaded fog gate mappings by fog gate ID
        /// </summary>
        public Dictionary<string, string> GetAllFogGateMappings()
        {
            if (!_isLoaded)
            {
                LoadFogGateNames();
            }

            return new Dictionary<string, string>(_fogGateNames);
        }

        /// <summary>
        /// Get all loaded fog gate mappings by entity ID
        /// </summary>
        public Dictionary<int, string> GetAllEntityIdMappings()
        {
            if (!_isLoaded)
            {
                LoadFogGateNames();
            }

            return new Dictionary<int, string>(_entityIdToFogGateNames);
        }
    }
}
