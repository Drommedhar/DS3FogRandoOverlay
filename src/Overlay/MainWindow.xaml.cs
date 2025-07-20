using DS3FogRandoOverlay.Models;
using DS3FogRandoOverlay.Services;
using DS3Parser.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DS3FogRandoOverlay
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DS3MemoryReader memoryReader;
        private readonly FogGateService fogGateService;
        private readonly AreaMapper areaMapper;
        private readonly ConfigurationService configurationService;
        private readonly FogGateTravelTracker travelTracker;
        private readonly SpoilerLogMonitor spoilerLogMonitor;
        private readonly DispatcherTimer updateTimer;
        private readonly DispatcherTimer fogGateUpdateTimer;
        private DispatcherTimer? retryConnectionTimer; // Timer for retrying DS3 connection

        private string? lastKnownArea;
        private string? lastKnownMapId;
        private DS3FogRandoOverlay.Services.Vector3? lastKnownPosition;

        private DateTime lastFogGateUpdate = DateTime.MinValue;
        private bool hasActiveFogGates = false;

        // Track whether spoiler information is shown
        private bool showSpoilerInfo = false;
        private bool isProcessingClick = false;

        private bool needsSettingsValidation = false;
        private bool isInitializing = false;

        public MainWindow()
        {
            InitializeComponent();

            configurationService = new ConfigurationService();
            memoryReader = new DS3MemoryReader();
            fogGateService = new FogGateService(configurationService);
            areaMapper = new AreaMapper();
            travelTracker = new FogGateTravelTracker(fogGateService, configurationService);
            spoilerLogMonitor = new SpoilerLogMonitor(configurationService, fogGateService);

            // Set up spoiler log monitoring event
            spoilerLogMonitor.SpoilerLogChanged += OnSpoilerLogChanged;

            updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000) // Update every 1 second instead of 100ms
            };
            updateTimer.Tick += UpdateTimer_Tick;

            fogGateUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(2000) // Update fog gates every 2 seconds instead of 200ms
            };
            fogGateUpdateTimer.Tick += FogGateUpdateTimer_Tick;

            // Add event handlers for window state changes
            this.LocationChanged += (s, e) => SaveWindowSettings();
            this.SizeChanged += (s, e) => SaveWindowSettings();
            this.Closing += (s, e) => SaveWindowSettings();

            // Check DS3 directory validity
            needsSettingsValidation = !IsDS3DirectoryValid();

            InitializeOverlay();
        }

        private void InitializeOverlay()
        {
            // Prevent recursive initialization
            if (isInitializing)
                return;
                
            isInitializing = true;
            
            try
            {
                // Load window size and position from config
                LoadWindowSettings();
                
                // Apply transparency setting
                ApplyTransparencySettings();

                // If we need settings validation, defer it until after the window is loaded
                if (needsSettingsValidation)
                {
                    this.Loaded += MainWindow_Loaded;
                    StatusText.Text = "Configuration required";
                    StatusText.Foreground = System.Windows.Media.Brushes.Orange;
                    HeaderText.Text = "DS3 Fog Randomizer (Configuration Required)";
                    SetMinimalMode(false);
                    return;
                }

                // Load spoiler log data
                LoadSpoilerLogData();

                // Start monitoring spoiler logs for changes
                spoilerLogMonitor.StartMonitoring();

                // Set up a timer to periodically check for spoiler log changes (backup method)
                var spoilerLogCheckTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(10) // Check every 10 seconds
                };
                spoilerLogCheckTimer.Tick += (s, e) => spoilerLogMonitor.CheckForNewSpoilerLogs();
                spoilerLogCheckTimer.Start();

                // Try to attach to DS3
                if (memoryReader.AttachToDS3())
                {
                    StatusText.Text = "Connected to DS3";
                    StatusText.Foreground = System.Windows.Media.Brushes.Green;
                    SetMinimalMode(true);
                    updateTimer.Start();
                    fogGateUpdateTimer.Start();
                    
                    // Force initial fog gate display
                    var mapId = memoryReader.GetCurrentMapId();
                    var position = memoryReader.GetPlayerPosition();
                    var currentArea = areaMapper.GetCurrentAreaName(mapId, position);
                    lastKnownArea = currentArea;
                    lastKnownMapId = mapId;
                    CurrentAreaText.Text = currentArea ?? "Unknown";
                    UpdateFogGatesDisplayWithDistances(mapId);
                }
                else
                {
                    StatusText.Text = "DS3 not found";
                    StatusText.Foreground = System.Windows.Media.Brushes.Red;
                    SetMinimalMode(false);

                    // Start retry connection attempt
                    StartRetryConnection();
                }
            }
            finally
            {
                isInitializing = false;
            }
        }

        private void LoadSpoilerLogData()
        {
            File.AppendAllText("ds3_debug.log", $"[MainWindow] {DateTime.Now:HH:mm:ss.fff} - Loading spoiler log data...\n");
            
            var currentSeed = fogGateService.GetCurrentSeed();
            File.AppendAllText("ds3_debug.log", $"[MainWindow] {DateTime.Now:HH:mm:ss.fff} - Current seed: {currentSeed ?? "null"}\n");
            File.AppendAllText("ds3_debug.log", $"[MainWindow] {DateTime.Now:HH:mm:ss.fff} - Has fog randomizer data: {fogGateService.HasFogRandomizerData()}\n");
            
            if (!string.IsNullOrEmpty(currentSeed))
            {
                HeaderText.Text = $"DS3 Fog Randomizer (Seed: {currentSeed})";
            }
            else
            {
                HeaderText.Text = "DS3 Fog Randomizer (No spoiler log found)";
            }
        }

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            if (!memoryReader.IsDS3Running())
            {
                StatusText.Text = "DS3 not running";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
                CurrentAreaText.Text = "Unknown";
                FogGatesPanel.Children.Clear();
                updateTimer.Stop();
                fogGateUpdateTimer.Stop(); // Stop fog gate updates too
                SetMinimalMode(false);
                
                // Start retry connection attempt
                StartRetryConnection();
                return;
            }

            // If we get here, DS3 is running, so stop any retry attempts
            StopRetryConnection();

            var position = memoryReader.GetPlayerPosition();
            var mapId = memoryReader.GetCurrentMapId();
            var currentArea = areaMapper.GetCurrentAreaName(mapId, position);
            
            File.AppendAllText("ds3_debug.log", $"[MainWindow] {DateTime.Now:HH:mm:ss.fff} - UpdateTimer_Tick - MapId: {mapId}, Position: {position}, CurrentArea: {currentArea}, LastKnownArea: {lastKnownArea}\n");

            // Update travel tracker with current state
            travelTracker.UpdatePlayerState(mapId, position, memoryReader);

            // Update area display if changed
            if (currentArea != lastKnownArea)
            {
                lastKnownArea = currentArea;
                lastKnownMapId = mapId;
                CurrentAreaText.Text = currentArea ?? "Unknown";
                // Force immediate fog gate update when area changes
                UpdateFogGatesDisplayWithDistances(mapId);
                lastKnownPosition = position; // Update last known position
                lastFogGateUpdate = DateTime.Now; // Reset the fog gate update timer
            }
            
            // Also trigger fog gate updates if we have moved significantly within the same area
            else if (position != null && lastKnownPosition != null)
            {
                float deltaX = Math.Abs(position.X - lastKnownPosition.X);
                float deltaY = Math.Abs(position.Y - lastKnownPosition.Y);
                float deltaZ = Math.Abs(position.Z - lastKnownPosition.Z);
                float totalDistance = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);

                // If moved more than 2 units, trigger immediate update from main timer too
                if (totalDistance > 2.0f && !string.IsNullOrEmpty(lastKnownMapId))
                {
                    UpdateFogGatesDisplayWithDistances(lastKnownMapId);
                    lastKnownPosition = position;
                    lastFogGateUpdate = DateTime.Now;
                }
            }
        }

        /// <summary>
        /// Start the retry timer to attempt reconnection to DS3
        /// </summary>
        private void StartRetryConnection()
        {
            // Stop any existing retry timer
            StopRetryConnection();
            
            // Create new retry timer
            retryConnectionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            retryConnectionTimer.Tick += (s, e) =>
            {
                if (memoryReader.AttachToDS3())
                {
                    StatusText.Text = "Connected to DS3";
                    StatusText.Foreground = System.Windows.Media.Brushes.Green;
                    SetMinimalMode(true);
                    updateTimer.Start();
                    fogGateUpdateTimer.Start();
                    StopRetryConnection();
                    
                    // Force initial fog gate display
                    var mapId = memoryReader.GetCurrentMapId();
                    var position = memoryReader.GetPlayerPosition();
                    var currentArea = areaMapper.GetCurrentAreaName(mapId, position);
                    lastKnownArea = currentArea;
                    lastKnownMapId = mapId;
                    CurrentAreaText.Text = currentArea ?? "Unknown";
                    UpdateFogGatesDisplayWithDistances(mapId);
                }
            };
            retryConnectionTimer.Start();
        }

        /// <summary>
        /// Stop the retry connection timer
        /// </summary>
        private void StopRetryConnection()
        {
            if (retryConnectionTimer != null)
            {
                retryConnectionTimer.Stop();
                retryConnectionTimer = null;
            }
        }

        private void FogGateUpdateTimer_Tick(object? sender, EventArgs e)
        {
            if (!memoryReader.IsDS3Running())
            {
                return; // Main timer will handle cleanup
            }

            var position = memoryReader.GetPlayerPosition();
            var currentTime = DateTime.Now;

            // Check if player has moved significantly (more than 1 unit)
            bool shouldUpdate = false;

            if (lastKnownPosition == null)
            {
                shouldUpdate = true;
            }
            else if (position != null)
            {
                float deltaX = Math.Abs(position.X - lastKnownPosition.X);
                float deltaY = Math.Abs(position.Y - lastKnownPosition.Y);
                float deltaZ = Math.Abs(position.Z - lastKnownPosition.Z);
                float totalDistance = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);

                if (totalDistance > 1.0f) // Player moved more than 1 unit (very sensitive)
                {
                    shouldUpdate = true;
                }
            }

            // Force update every 10 seconds even if player hasn't moved much
            // This ensures distances stay accurate and catches any missed updates
            double forceUpdateInterval = 10.0; // Much longer force update interval
            if ((currentTime - lastFogGateUpdate).TotalSeconds > forceUpdateInterval)
            {
                shouldUpdate = true;
            }

            // Update fog gates if player moved significantly or if we need a periodic refresh
            if (shouldUpdate && !string.IsNullOrEmpty(lastKnownMapId) && !isProcessingClick)
            {
                UpdateFogGatesDisplayWithDistances(lastKnownMapId);
                lastKnownPosition = position;
                lastFogGateUpdate = currentTime;
                
                // Adjust update frequency for next cycle
                AdjustUpdateFrequency();
            }
        }



        private void UpdateFogGatesDisplayWithDistances(string? mapId)
        {
            FogGatesPanel.Children.Clear();
            
            File.AppendAllText("ds3_debug.log", $"[MainWindow] {DateTime.Now:HH:mm:ss.fff} - UpdateFogGatesDisplayWithDistances called for mapId: {mapId}\n");
            File.AppendAllText("ds3_debug.log", $"[MainWindow] {DateTime.Now:HH:mm:ss.fff} - Has fog randomizer data: {fogGateService.HasFogRandomizerData()}\n");

            if (!fogGateService.HasFogRandomizerData())
            {
                File.AppendAllText("ds3_debug.log", $"[MainWindow] {DateTime.Now:HH:mm:ss.fff} - No fog randomizer data found, displaying error message\n");
                var noDataText = new TextBlock
                {
                    Text = "No fog randomizer data found",
                    Style = (Style)FindResource("OverlayTextStyle")
                };
                FogGatesPanel.Children.Add(noDataText);
                return;
            }

            if (string.IsNullOrEmpty(mapId))
            {
                File.AppendAllText("ds3_debug.log", $"[MainWindow] {DateTime.Now:HH:mm:ss.fff} - Current mapId is null or empty\n");
                var noAreaText = new TextBlock
                {
                    Text = "Current area unknown",
                    Style = (Style)FindResource("OverlayTextStyle")
                };
                FogGatesPanel.Children.Add(noAreaText);
                return;
            }

            // Get fog gates and warps in current area by map ID
            var fogGatesInArea = fogGateService.GetFogGatesInArea(mapId);
            var warpsInArea = fogGateService.GetWarpsInArea(mapId);

            File.AppendAllText("ds3_debug.log", $"[MainWindow] {DateTime.Now:HH:mm:ss.fff} - Found {fogGatesInArea.Count} fog gates, {warpsInArea.Count} warps in mapId: {mapId}\n");

            bool hasAnyGates = fogGatesInArea.Any() || warpsInArea.Any();

            if (!hasAnyGates)
            {
                File.AppendAllText("ds3_debug.log", $"[MainWindow] {DateTime.Now:HH:mm:ss.fff} - No fog gates found in mapId: {mapId}\n");
                var noGatesText = new TextBlock
                {
                    Text = "No fog gates found in this area",
                    Style = (Style)FindResource("OverlayTextStyle")
                };
                FogGatesPanel.Children.Add(noGatesText);
                return;
            }

            File.AppendAllText("ds3_debug.log", $"[MainWindow] {DateTime.Now:HH:mm:ss.fff} - Displaying fog gates for mapId: {mapId}\n");

            // Get player position for distance calculations
            var playerPosition = memoryReader.GetPlayerPosition();

            // Create a list of fog gates with their distances for sorting
            var fogGatesWithDistances = new List<(DS3FogGate fogGate, float? distance)>();
            
            foreach (var fogGate in fogGatesInArea)
            {
                float? distance = null;
                if (playerPosition != null)
                {
                    distance = fogGateService.GetDistanceToFogGate(fogGate, playerPosition);
                }
                fogGatesWithDistances.Add((fogGate, distance));
            }

            // Sort by distance (closest first), with gates without distance at the end
            var sortedFogGates = fogGatesWithDistances
                .OrderBy(item => item.distance.HasValue ? 0 : 1) // Put gates with distance first
                .ThenBy(item => item.distance ?? float.MaxValue) // Then sort by distance (closest first)
                .ToList();

            // Display fog gates in sorted order
            foreach (var (fogGate, distance) in sortedFogGates)
            {
                var connection = fogGateService.GetConnectionForFogGate(mapId, fogGate.Id);
                var displayText = BuildFogGateDisplayText(fogGate, connection);
                
                var gateContainer = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(10, 1, 0, 3)
                };

                var gateNameText = new TextBlock
                {
                    Text = displayText,
                    Style = (Style)FindResource("OverlayTextStyle"),
                    Foreground = fogGate.IsBoss ? System.Windows.Media.Brushes.Gold : System.Windows.Media.Brushes.White,
                    FontSize = 12,
                    Margin = new Thickness(0, 1, 0, 1)
                };

                gateContainer.Children.Add(gateNameText);
                
                // Add distance info for fog gates (not warps as per user request)
                if (playerPosition != null && distance.HasValue)
                {
                    var distanceText = new TextBlock
                    {
                        Text = $"   Distance: {distance.Value:F1} units",
                        Style = (Style)FindResource("DistanceTextStyle"),
                        Foreground = Brushes.Cyan,
                        Margin = new Thickness(15, 1, 0, 1),
                        FontSize = 10
                    };
                    gateContainer.Children.Add(distanceText);
                }
                
                // Add destination info if spoiler info is enabled
                if (connection != null && showSpoilerInfo)
                {
                    File.AppendAllText("ds3_debug.log", $"[MainWindow] {DateTime.Now:HH:mm:ss.fff} - Adding spoiler info for fog gate: {fogGate.Text ?? fogGate.Name} -> {connection.ToArea}\n");
                    
                    // Check if ToArea contains multiple destinations (with arrows)
                    var destinations = connection.ToArea.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    
                    if (destinations.Length > 1)
                    {
                        // Multiple destinations - show them in a grid-like format
                        var leadsToText = new TextBlock
                        {
                            Text = "   Leads to:",
                            Style = (Style)FindResource("DistanceTextStyle"),
                            Foreground = Brushes.LightGreen,
                            Margin = new Thickness(15, 2, 0, 2),
                            FontStyle = FontStyles.Italic
                        };
                        gateContainer.Children.Add(leadsToText);
                        
                        foreach (var destination in destinations)
                        {
                            var destinationText = new TextBlock
                            {
                                Text = $"     {destination.Trim()}",
                                Style = (Style)FindResource("DistanceTextStyle"),
                                Foreground = Brushes.LightGreen,
                                Margin = new Thickness(15, 0, 0, 1),
                                FontStyle = FontStyles.Italic
                            };
                            gateContainer.Children.Add(destinationText);
                        }
                    }
                    else
                    {
                        // Single destination - show as before
                        var destinationText = new TextBlock
                        {
                            Text = $"   → Leads to: {connection.ToArea}",
                            Style = (Style)FindResource("DistanceTextStyle"),
                            Foreground = Brushes.LightGreen,
                            Margin = new Thickness(15, 2, 0, 2),
                            FontStyle = FontStyles.Italic
                        };
                        gateContainer.Children.Add(destinationText);
                    }
                    
                    var connectionTypeText = new TextBlock
                    {
                        Text = $"   → {(connection.IsRandom ? "Randomized" : "Original")} connection",
                        Style = (Style)FindResource("DistanceTextStyle"),
                        Foreground = connection.IsRandom ? Brushes.Orange : Brushes.Gray,
                        Margin = new Thickness(15, 0, 0, 2),
                        FontStyle = FontStyles.Italic,
                        FontSize = 10
                    };
                    gateContainer.Children.Add(connectionTypeText);
                }
                else
                {
                    File.AppendAllText("ds3_debug.log", $"[MainWindow] {DateTime.Now:HH:mm:ss.fff} - Not adding spoiler info - connection: {connection != null}, showSpoilerInfo: {showSpoilerInfo}\n");
                }

                // Add traveled connection info (always shown regardless of spoiler setting)
                // Show travel info based on which side of the gate the player is currently on
                if (playerPosition != null)
                {
                    var (hasTravel, travelInfo, isSource) = travelTracker.GetRelevantTravelInfo(mapId, fogGate.Id, playerPosition);
                    
                    File.AppendAllText("ds3_debug.log", $"[MainWindow] {DateTime.Now:HH:mm:ss.fff} - Relevant travel for gate {fogGate.Name} (ID: {fogGate.Id}): hasTravel={hasTravel}, isSource={isSource}, info={travelInfo}\n");
                    
                    if (hasTravel && !string.IsNullOrEmpty(travelInfo))
                    {
                        var travelText = new TextBlock
                        {
                            Text = isSource ? $"   ✓ Traveled to: {travelInfo}" : $"   ← Arrived from: {travelInfo}",
                            Style = (Style)FindResource("DistanceTextStyle"),
                            Foreground = isSource ? Brushes.LightYellow : Brushes.LightBlue,
                            Margin = new Thickness(15, 1, 0, 2),
                            FontStyle = FontStyles.Italic,
                            FontWeight = FontWeights.Bold,
                            FontSize = 10
                        };
                        gateContainer.Children.Add(travelText);
                    }
                }

                FogGatesPanel.Children.Add(gateContainer);
            }

            // Display warps
            foreach (var warp in warpsInArea)
            {
                var connection = fogGateService.GetConnectionForFogGate(warp.Area, warp.Id);
                var displayText = BuildWarpDisplayText(warp, connection);
                
                var warpContainer = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(10, 1, 0, 3)
                };

                var warpNameText = new TextBlock
                {
                    Text = displayText,
                    Style = (Style)FindResource("OverlayTextStyle"),
                    Foreground = System.Windows.Media.Brushes.LightBlue,
                    FontSize = 12,
                    Margin = new Thickness(0, 1, 0, 1)
                };

                warpContainer.Children.Add(warpNameText);
                
                // Add destination info if spoiler info is enabled
                if (connection != null && showSpoilerInfo)
                {
                    File.AppendAllText("ds3_debug.log", $"[MainWindow] {DateTime.Now:HH:mm:ss.fff} - Adding spoiler info for warp: {warp.Text ?? $"Warp {warp.Id}"} -> {connection.ToArea}\n");
                    
                    var destinationText = new TextBlock
                    {
                        Text = $"   → Warps to: {connection.ToArea}",
                        Style = (Style)FindResource("DistanceTextStyle"),
                        Foreground = Brushes.LightCyan,
                        Margin = new Thickness(15, 2, 0, 2),
                        FontStyle = FontStyles.Italic
                    };
                    warpContainer.Children.Add(destinationText);
                    
                    var connectionTypeText = new TextBlock
                    {
                        Text = $"   → {(connection.IsRandom ? "Randomized" : "Original")} warp",
                        Style = (Style)FindResource("DistanceTextStyle"),
                        Foreground = connection.IsRandom ? Brushes.Orange : Brushes.Gray,
                        Margin = new Thickness(15, 0, 0, 2),
                        FontStyle = FontStyles.Italic,
                        FontSize = 10
                    };
                    warpContainer.Children.Add(connectionTypeText);
                }
                else
                {
                    File.AppendAllText("ds3_debug.log", $"[MainWindow] {DateTime.Now:HH:mm:ss.fff} - Not adding warp spoiler info - connection: {connection != null}, showSpoilerInfo: {showSpoilerInfo}\n");
                }

                FogGatesPanel.Children.Add(warpContainer);
            }
        }



        private void LoadWindowSettings()
        {
            var config = configurationService.Config;
            
            // Load window size
            if (config.WindowWidth > 0 && config.WindowHeight > 0)
            {
                this.Width = Math.Max(config.WindowWidth, this.MinWidth);
                this.Height = Math.Max(config.WindowHeight, this.MinHeight);
            }
            
            // Load window position
            if (config.WindowLeft >= 0 && config.WindowTop >= 0)
            {
                // Make sure the window is still visible on screen
                if (config.WindowLeft + this.Width <= SystemParameters.PrimaryScreenWidth &&
                    config.WindowTop + this.Height <= SystemParameters.PrimaryScreenHeight)
                {
                    this.Left = config.WindowLeft;
                    this.Top = config.WindowTop;
                }
                else
                {
                    // Position in top-right corner if saved position is off-screen
                    this.Left = SystemParameters.PrimaryScreenWidth - this.Width - 20;
                    this.Top = 20;
                }
            }
            else
            {
                // Default position in top-right corner
                this.Left = SystemParameters.PrimaryScreenWidth - this.Width - 20;
                this.Top = 20;
            }
        }

        private void SaveWindowSettings()
        {
            var config = configurationService.Config;
            config.WindowLeft = this.Left;
            config.WindowTop = this.Top;
            config.WindowWidth = this.Width;
            config.WindowHeight = this.Height;
            configurationService.SaveConfig();
        }

        private void SetMinimalMode(bool isMinimal)
        {
            if (isMinimal)
            {
                // Hide non-gameplay elements when connected to DS3
                StatusPanel.Visibility = Visibility.Collapsed;
                AreaPanel.Visibility = Visibility.Collapsed;
                HeaderText.Text = "Fog Gates";
                
                // Make window more compact
                this.MinHeight = 150;
                this.Height = Math.Max(this.Height - 100, 200);
            }
            else
            {
                // Show all elements when not connected
                StatusPanel.Visibility = Visibility.Visible;
                AreaPanel.Visibility = Visibility.Visible;
                HeaderText.Text = "DS3 Fog Randomizer";
                
                // Reset window size
                this.MinHeight = 200;
                this.Height = Math.Max(this.Height, 300);
            }
        }

        private bool FuzzyAreaMatch(string areaName1, string areaName2)
        {
            // Simple fuzzy matching for area names
            var name1 = areaName1.ToLower().Replace(" ", "");
            var name2 = areaName2.ToLower().Replace(" ", "");

            return name1.Contains(name2) || name2.Contains(name1) ||
                   name1.Split(' ').Any(word => name2.Contains(word)) ||
                   name2.Split(' ').Any(word => name1.Contains(word));
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settingsWindow = new SettingsWindow(configurationService)
                {
                    Owner = this
                };

                var result = settingsWindow.ShowDialog();
                
                if (result == true && settingsWindow.SettingsChanged)
                {
                    // Restart the services with new paths
                    RestartServicesWithNewPaths();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening settings: {ex.Message}", "Settings Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RestartServicesWithNewPaths()
        {
            try
            {
                // Check if DS3 directory is now valid
                if (IsDS3DirectoryValid())
                {
                    // Directory is now valid, we can proceed with full initialization
                    File.AppendAllText("ds3_debug.log", $"[MainWindow] DS3 directory now valid, performing full restart\n");
                    
                    needsSettingsValidation = false;
                    
                    // Stop any existing timers
                    updateTimer.Stop();
                    fogGateUpdateTimer.Stop();
                    
                    // Restart spoiler log monitoring
                    spoilerLogMonitor.StopMonitoring();
                    spoilerLogMonitor.StartMonitoring();

                    // Apply transparency settings
                    ApplyTransparencySettings();

                    // Reload fog gate service data
                    fogGateService.ReloadData();

                    // Load spoiler log data
                    LoadSpoilerLogData();

                    // Try to connect to DS3 and start normal operation
                    if (memoryReader.AttachToDS3())
                    {
                        StatusText.Text = "Connected to DS3";
                        StatusText.Foreground = System.Windows.Media.Brushes.Green;
                        SetMinimalMode(true);
                        updateTimer.Start();
                        fogGateUpdateTimer.Start();
                        
                        // Force initial fog gate display
                        var mapId = memoryReader.GetCurrentMapId();
                        var position = memoryReader.GetPlayerPosition();
                        var currentArea = areaMapper.GetCurrentAreaName(mapId, position);
                        lastKnownArea = currentArea;
                        lastKnownMapId = mapId;
                        CurrentAreaText.Text = currentArea ?? "Unknown";
                        UpdateFogGatesDisplayWithDistances(mapId);
                    }
                    else
                    {
                        StatusText.Text = "DS3 not found";
                        StatusText.Foreground = System.Windows.Media.Brushes.Red;
                        SetMinimalMode(false);
                        StartRetryConnection();
                    }
                }
                else
                {
                    // Directory is still invalid after settings change
                    File.AppendAllText("ds3_debug.log", $"[MainWindow] DS3 directory still invalid after settings change\n");
                    
                    // Stop timers temporarily
                    updateTimer.Stop();
                    fogGateUpdateTimer.Stop();

                    // Apply transparency settings (user might have changed this)
                    ApplyTransparencySettings();

                    // Show error state
                    StatusText.Text = "Invalid DS3 directory";
                    StatusText.Foreground = System.Windows.Media.Brushes.Red;
                    HeaderText.Text = "DS3 Fog Randomizer (Configuration Required)";
                    SetMinimalMode(false);
                }

                File.AppendAllText("ds3_debug.log", $"[MainWindow] {DateTime.Now:HH:mm:ss.fff} - Services restarted with new paths\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText("ds3_debug.log", $"[MainWindow] {DateTime.Now:HH:mm:ss.fff} - Error restarting services: {ex.Message}\n");
                
                MessageBox.Show($"Error restarting services with new paths: {ex.Message}\n\nPlease restart the application.",
                              "Service Restart Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        public void Detach()
        {
            updateTimer?.Stop();
            fogGateUpdateTimer?.Stop();
            memoryReader?.Detach();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            Detach();
            spoilerLogMonitor?.Dispose();
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            updateTimer?.Stop();
            fogGateUpdateTimer?.Stop();
            StopRetryConnection();
            memoryReader?.Detach();
            base.OnClosed(e);
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Only allow dragging the window when clicking on empty areas, not on fog gate entries
            // Check if the click originated from a Button or TextBlock (fog gate entry)
            if (e.OriginalSource is Button || e.OriginalSource is TextBlock)
            {
                // If it's a clickable TextBlock with a hand cursor, don't interfere
                if (e.OriginalSource is TextBlock textBlock && textBlock.Cursor == System.Windows.Input.Cursors.Hand)
                {
                    return; // Let the TextBlock handle the click
                }
                // For other TextBlocks (like headers), allow dragging
                if (e.OriginalSource is TextBlock)
                {
                    if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
                    {
                        this.DragMove();
                    }
                    return;
                }
                return; // Don't drag if clicking on a button
            }
            
            // Allow dragging the window when clicking anywhere else
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void ResetPosition_Click(object sender, RoutedEventArgs e)
        {
            // Reset overlay to top-right corner
            this.Left = SystemParameters.PrimaryScreenWidth - this.Width - 20;
            this.Top = 20;
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.F9:
                    // Reset position
                    ResetPosition_Click(sender, new RoutedEventArgs());
                    break;
                case System.Windows.Input.Key.Escape:
                    // Exit application
                    ExitButton_Click(sender, new RoutedEventArgs());
                    break;
            }
        }

        private void ScanAreaIds_Click(object sender, RoutedEventArgs e)
        {
            if (memoryReader.IsDS3Running())
            {
                StatusText.Text = "Scanning for Area IDs...";
                StatusText.Foreground = System.Windows.Media.Brushes.Yellow;
                memoryReader.ScanForAreaIds();
                StatusText.Text = "Area ID scan complete - check debug log";
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                MessageBox.Show("DS3 is not running!", "Debug Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ScanWorldChrMan_Click(object sender, RoutedEventArgs e)
        {
            if (memoryReader.IsDS3Running())
            {
                StatusText.Text = "Scanning for WorldChrMan...";
                StatusText.Foreground = System.Windows.Media.Brushes.Yellow;
                memoryReader.ScanForWorldChrMan();
                StatusText.Text = "WorldChrMan scan complete - check debug log";
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                MessageBox.Show("DS3 is not running!", "Debug Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void TestMemoryReading_Click(object sender, RoutedEventArgs e)
        {
            if (memoryReader.IsDS3Running())
            {
                var position = memoryReader.GetPlayerPosition();
                var mapId = memoryReader.GetCurrentMapId();
                var currentArea = areaMapper.GetCurrentAreaName(mapId, position);

                string message = $"Memory Reading Test Results:\n\n" +
                               $"Map ID: {mapId ?? "null"}\n" +
                               $"Position: {(position != null ? $"X={position.X:F2}, Y={position.Y:F2}, Z={position.Z:F2}" : "null")}\n" +
                               $"Current Area: {currentArea ?? "null"}\n\n" +
                               $"Check ds3_debug.log for detailed information.";

                MessageBox.Show(message, "Memory Reading Test", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("DS3 is not running!", "Debug Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AdjustUpdateFrequency()
        {
            // Check if we currently have fog gates displayed
            bool currentlyHasFogGates = FogGatesPanel.Children.Count > 1; // More than just header
            
            if (currentlyHasFogGates != hasActiveFogGates)
            {
                hasActiveFogGates = currentlyHasFogGates;
                
                if (hasActiveFogGates)
                {
                    // Very fast updates when we have fog gates to track
                    fogGateUpdateTimer.Interval = TimeSpan.FromMilliseconds(150); // Real-time updates
                }
                else
                {
                    // Still fairly fast when no fog gates are present
                    fogGateUpdateTimer.Interval = TimeSpan.FromMilliseconds(400);
                }
            }
        }

        private string BuildFogGateDisplayText(DS3FogGate fogGate, DS3Connection? connection)
        {
            var prefix = fogGate.IsBoss ? "👑" : "🚪";
            var text = $"{prefix} {fogGate.Text}";
            
            if (string.IsNullOrEmpty(fogGate.Text))
            {
                text = $"{prefix} {fogGate.Name}";
            }
            
            if (fogGate.IsBoss)
            {
                text += " (Boss)";
            }
            
            return text;
        }

        private string BuildWarpDisplayText(DS3Warp warp, DS3Connection? connection)
        {
            var prefix = "🌀";
            var text = $"{prefix} {warp.Text}";
            
            if (string.IsNullOrEmpty(warp.Text))
            {
                text = $"{prefix} Warp {warp.Id}";
            }
            
            text += " (Warp)";
            
            return text;
        }

        private void ToggleSpoilerInfo_Click(object sender, RoutedEventArgs e)
        {
            showSpoilerInfo = !showSpoilerInfo;
            
            // Update the menu item text using the named reference
            ToggleSpoilerMenuItem.Header = showSpoilerInfo ? "Hide Spoiler Information" : "Show Spoiler Information";
            
            // Force refresh the display to show/hide the spoiler info
            // Use the currently known mapId, or force update with null if no mapId is known
            UpdateFogGatesDisplayWithDistances(lastKnownMapId);
            
            File.AppendAllText("ds3_debug.log", $"[MainWindow] {DateTime.Now:HH:mm:ss.fff} - Spoiler info toggled: {(showSpoilerInfo ? "Shown" : "Hidden")}\n");
        }

        private void ApplyTransparencySettings()
        {
            if (configurationService.Config.TransparentBackground)
            {
                // Make the main background transparent
                MainBorder.Background = System.Windows.Media.Brushes.Transparent;
                
                // Remove the corner radius and padding from the main border
                MainBorder.CornerRadius = new CornerRadius(0);
                MainBorder.Padding = new Thickness(0);
            }
            else
            {
                // Restore the original background
                MainBorder.Background = new SolidColorBrush(Color.FromArgb(0x66, 0x00, 0x00, 0x00));
                MainBorder.CornerRadius = new CornerRadius(5);
                MainBorder.Padding = new Thickness(8);
            }
        }

        /// <summary>
        /// Check if the DS3 directory is valid and contains fog randomizer data
        /// </summary>
        private bool IsDS3DirectoryValid()
        {
            try
            {
                var ds3Path = configurationService.Config.DarkSouls3Path;
                if (string.IsNullOrEmpty(ds3Path) || !Directory.Exists(ds3Path))
                {
                    File.AppendAllText("ds3_debug.log", $"[MainWindow] DS3 directory invalid or missing: {ds3Path}\n");
                    return false;
                }

                var parser = new DS3Parser.DS3Parser();
                var gameDirectory = System.IO.Path.Combine(ds3Path, "Game");
                
                var hasRandomizerData = parser.HasFogRandomizerData(gameDirectory);
                File.AppendAllText("ds3_debug.log", $"[MainWindow] DS3 directory check - gameDirectory: {gameDirectory}, hasRandomizerData: {hasRandomizerData}\n");
                
                return hasRandomizerData;
            }
            catch (Exception ex)
            {
                File.AppendAllText("ds3_debug.log", $"[MainWindow] Error checking DS3 directory validity: {ex.Message}\n");
                return false;
            }
        }

        /// <summary>
        /// Open the settings window safely (reuses existing Settings_Click logic)
        /// </summary>
        private void OpenSettingsWindow()
        {
            // Simply call the existing Settings_Click method which already has proper error handling
            Settings_Click(this, new RoutedEventArgs());
        }

        /// <summary>
        /// Handle when spoiler log files change (new log detected)
        /// </summary>
        private void OnSpoilerLogChanged()
        {
            try
            {
                File.AppendAllText("ds3_debug.log", $"[MainWindow] Spoiler log changed detected - resetting travel tracker\n");
                
                // Reset the travel tracker (clears all travel data and state)
                travelTracker.ResetTracker();
                
                // Reload spoiler log data
                LoadSpoilerLogData();
                
                // Refresh the display
                Dispatcher.Invoke(() =>
                {
                    UpdateFogGatesDisplayWithDistances(lastKnownMapId);
                });
                
                File.AppendAllText("ds3_debug.log", $"[MainWindow] Travel tracker reset and display refreshed due to spoiler log change\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText("ds3_debug.log", $"[MainWindow] Error handling spoiler log change: {ex.Message}\n");
            }
        }

        /// <summary>
        /// Handle when the main window is fully loaded (used for deferred settings validation)
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= MainWindow_Loaded; // Remove the event handler
            
            if (needsSettingsValidation)
            {
                // Show a message and ask if the user wants to open settings
                var result = MessageBox.Show(this,
                    "The Dark Souls 3 directory is not set correctly or does not contain fog randomizer data.\n\n" +
                    "The overlay requires a valid DS3 installation with the fog randomizer mod installed.\n\n" +
                    "Would you like to open the settings to configure the correct directory?",
                    "Invalid Dark Souls 3 Directory",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // Try to open settings
                    try
                    {
                        OpenSettingsWindow();
                    }
                    catch (Exception ex)
                    {
                        File.AppendAllText("ds3_debug.log", $"[MainWindow] Error opening settings from MainWindow_Loaded: {ex.Message}\n");
                        
                        MessageBox.Show(this,
                            "Unable to open settings window. You can access settings through the right-click menu.",
                            "Settings Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                            
                        // Show help text
                        StatusText.Text = "Right-click for settings";
                        StatusText.Foreground = System.Windows.Media.Brushes.Yellow;
                        HeaderText.Text = "DS3 Fog Randomizer (Configuration Required)";
                        SetMinimalMode(false);
                    }
                }
                else
                {
                    // User chose not to configure, show helpful message
                    StatusText.Text = "Right-click for settings";
                    StatusText.Foreground = System.Windows.Media.Brushes.Yellow;
                    HeaderText.Text = "DS3 Fog Randomizer (Configuration Required)";
                    SetMinimalMode(false);
                }
            }
        }
    }
}