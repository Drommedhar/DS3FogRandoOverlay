using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DS3FogRandoOverlay.Services
{
    /// <summary>
    /// Service for resolving Dark Souls 3 game paths and automatically finding fog mod directories
    /// </summary>
    public class PathResolver
    {
        private readonly string _basePath;
        
        /// <summary>
        /// Initialize PathResolver with a base Dark Souls 3 directory
        /// </summary>
        /// <param name="basePath">Base path to Dark Souls 3 (e.g., "C:\Program Files (x86)\Steam\steamapps\common\DARK SOULS III")</param>
        public PathResolver(string basePath)
        {
            _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
        }

        /// <summary>
        /// Find the fog mod directory by searching for the "fog" folder in subdirectories
        /// </summary>
        /// <returns>Path to the fog directory, or null if not found</returns>
        public string? FindFogDirectory()
        {
            if (!Directory.Exists(_basePath))
                return null;

            try
            {
                // Common locations to check for fog directory
                var searchPaths = new List<string>
                {
                    _basePath,
                    Path.Combine(_basePath, "Game"),
                    Path.Combine(_basePath, "game")
                };

                // Search each potential location
                foreach (var searchPath in searchPaths.Where(Directory.Exists))
                {
                    var fogPath = Path.Combine(searchPath, "fog");
                    if (Directory.Exists(fogPath))
                    {
                        // Verify it's a fog mod directory by checking for key subdirectories
                        if (IsValidFogDirectory(fogPath))
                            return fogPath;
                    }
                }

                // If not found in common locations, search recursively (up to 2 levels deep)
                return SearchForFogDirectory(_basePath, maxDepth: 2);
            }
            catch (Exception ex)
            {
                File.AppendAllText("ds3_debug.log",
                    $"[PathResolver] {DateTime.Now:HH:mm:ss.fff} - Error finding fog directory: {ex.Message}\n");
                return null;
            }
        }

        /// <summary>
        /// Find the events.txt file within the fog directory
        /// </summary>
        /// <param name="fogDirectory">Path to the fog directory</param>
        /// <returns>Path to events.txt, or null if not found</returns>
        public string? FindEventsFile(string? fogDirectory = null)
        {
            fogDirectory ??= FindFogDirectory();
            if (string.IsNullOrEmpty(fogDirectory) || !Directory.Exists(fogDirectory))
                return null;

            try
            {
                // Common locations for events.txt
                var searchPaths = new[]
                {
                    Path.Combine(fogDirectory, "fogdist", "events.txt"),
                    Path.Combine(fogDirectory, "events.txt")
                };

                foreach (var path in searchPaths.Where(File.Exists))
                {
                    return path;
                }

                // Search recursively for events.txt in fog directory
                return SearchForFile(fogDirectory, "events.txt", maxDepth: 3);
            }
            catch (Exception ex)
            {
                File.AppendAllText("ds3_debug.log",
                    $"[PathResolver] {DateTime.Now:HH:mm:ss.fff} - Error finding events.txt: {ex.Message}\n");
                return null;
            }
        }

        /// <summary>
        /// Find the mapstudio directory within the fog directory
        /// </summary>
        /// <param name="fogDirectory">Path to the fog directory</param>
        /// <returns>Path to mapstudio directory, or null if not found</returns>
        public string? FindMapStudioDirectory(string? fogDirectory = null)
        {
            fogDirectory ??= FindFogDirectory();
            if (string.IsNullOrEmpty(fogDirectory) || !Directory.Exists(fogDirectory))
                return null;

            try
            {
                // Common locations for mapstudio
                var searchPaths = new[]
                {
                    Path.Combine(fogDirectory, "map", "mapstudio"),
                    Path.Combine(fogDirectory, "mapstudio")
                };

                foreach (var path in searchPaths.Where(Directory.Exists))
                {
                    return path;
                }

                // Search recursively for mapstudio directory
                return SearchForDirectory(fogDirectory, "mapstudio", maxDepth: 3);
            }
            catch (Exception ex)
            {
                File.AppendAllText("ds3_debug.log",
                    $"[PathResolver] {DateTime.Now:HH:mm:ss.fff} - Error finding mapstudio directory: {ex.Message}\n");
                return null;
            }
        }

        /// <summary>
        /// Get common Steam installation paths to search for Dark Souls 3
        /// </summary>
        /// <returns>List of potential Steam library paths</returns>
        public static List<string> GetCommonSteamPaths()
        {
            var paths = new List<string>();

            // Common Steam installation locations
            var steamBasePaths = new[]
            {
                @"C:\Program Files (x86)\Steam",
                @"C:\Program Files\Steam",
                @"D:\Steam",
                @"E:\Steam"
            };

            foreach (var steamPath in steamBasePaths)
            {
                var ds3Path = Path.Combine(steamPath, "steamapps", "common", "DARK SOULS III");
                if (Directory.Exists(ds3Path))
                {
                    paths.Add(ds3Path);
                }
            }

            return paths;
        }

        /// <summary>
        /// Try to auto-detect Dark Souls 3 installation path
        /// </summary>
        /// <returns>Path to Dark Souls 3, or null if not found</returns>
        public static string? AutoDetectDarkSouls3Path()
        {
            var commonPaths = GetCommonSteamPaths();
            return commonPaths.FirstOrDefault();
        }

        private bool IsValidFogDirectory(string path)
        {
            if (!Directory.Exists(path))
                return false;

            // Check for key subdirectories that indicate this is a fog mod directory
            var requiredSubdirs = new[] { "fogdist", "map" };
            var optionalSubdirs = new[] { "event", "msg", "script" };

            var existingSubdirs = Directory.GetDirectories(path)
                .Select(d => Path.GetFileName(d).ToLowerInvariant())
                .ToHashSet();

            // At least one required subdir must exist
            bool hasRequired = requiredSubdirs.Any(req => existingSubdirs.Contains(req.ToLowerInvariant()));
            
            // And at least one optional subdir should exist
            bool hasOptional = optionalSubdirs.Any(opt => existingSubdirs.Contains(opt.ToLowerInvariant()));

            return hasRequired || hasOptional;
        }

        private string? SearchForFogDirectory(string basePath, int maxDepth, int currentDepth = 0)
        {
            if (currentDepth >= maxDepth || !Directory.Exists(basePath))
                return null;

            try
            {
                // Check current directory for fog subdirectory
                var fogPath = Path.Combine(basePath, "fog");
                if (Directory.Exists(fogPath) && IsValidFogDirectory(fogPath))
                    return fogPath;

                // Search subdirectories
                foreach (var subDir in Directory.GetDirectories(basePath))
                {
                    var result = SearchForFogDirectory(subDir, maxDepth, currentDepth + 1);
                    if (result != null)
                        return result;
                }
            }
            catch
            {
                // Ignore access denied or other errors when searching
            }

            return null;
        }

        private string? SearchForDirectory(string basePath, string targetDirName, int maxDepth, int currentDepth = 0)
        {
            if (currentDepth >= maxDepth || !Directory.Exists(basePath))
                return null;

            try
            {
                // Check current directory for target subdirectory
                var targetPath = Path.Combine(basePath, targetDirName);
                if (Directory.Exists(targetPath))
                    return targetPath;

                // Search subdirectories
                foreach (var subDir in Directory.GetDirectories(basePath))
                {
                    if (Path.GetFileName(subDir).Equals(targetDirName, StringComparison.OrdinalIgnoreCase))
                        return subDir;

                    var result = SearchForDirectory(subDir, targetDirName, maxDepth, currentDepth + 1);
                    if (result != null)
                        return result;
                }
            }
            catch
            {
                // Ignore access denied or other errors when searching
            }

            return null;
        }

        private string? SearchForFile(string basePath, string targetFileName, int maxDepth, int currentDepth = 0)
        {
            if (currentDepth >= maxDepth || !Directory.Exists(basePath))
                return null;

            try
            {
                // Check current directory for target file
                var targetPath = Path.Combine(basePath, targetFileName);
                if (File.Exists(targetPath))
                    return targetPath;

                // Search subdirectories
                foreach (var subDir in Directory.GetDirectories(basePath))
                {
                    var result = SearchForFile(subDir, targetFileName, maxDepth, currentDepth + 1);
                    if (result != null)
                        return result;
                }
            }
            catch
            {
                // Ignore access denied or other errors when searching
            }

            return null;
        }
    }
}
