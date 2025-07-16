using DS3FogRandoOverlay.Models;
using DS3FogRandoOverlay.Services;
using DS3Parser.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
        private readonly DispatcherTimer updateTimer;
        private readonly DispatcherTimer fogGateUpdateTimer;

        private string? lastKnownArea;
        private DS3FogRandoOverlay.Services.Vector3? lastKnownPosition;
        private bool isTooltipActive = false;

        private DateTime lastFogGateUpdate = DateTime.MinValue;
        private bool hasActiveFogGates = false;

        public MainWindow()
        {
            InitializeComponent();

            configurationService = new ConfigurationService();
            memoryReader = new DS3MemoryReader();
            fogGateService = new FogGateService(configurationService);
            areaMapper = new AreaMapper();

            updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100) // Update every 100ms for very fast responsiveness
            };
            updateTimer.Tick += UpdateTimer_Tick;

            fogGateUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200) // Update fog gates every 200ms initially
            };
            fogGateUpdateTimer.Tick += FogGateUpdateTimer_Tick;

            // Add event handlers for window state changes
            this.LocationChanged += (s, e) => SaveWindowSettings();
            this.SizeChanged += (s, e) => SaveWindowSettings();
            this.Closing += (s, e) => SaveWindowSettings();

            InitializeOverlay();
        }

        private void InitializeOverlay()
        {
            // Load window size and position from config
            LoadWindowSettings();

            // Load spoiler log data
            LoadSpoilerLogData();

            // Try to attach to DS3
            if (memoryReader.AttachToDS3())
            {
                StatusText.Text = "Connected to DS3";
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
                SetMinimalMode(true);
                updateTimer.Start();
                fogGateUpdateTimer.Start();
            }
            else
            {
                StatusText.Text = "DS3 not found";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
                SetMinimalMode(false);

                // Retry connection every 2 seconds
                var retryTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(2)
                };
                retryTimer.Tick += (s, e) =>
                {
                    if (memoryReader.AttachToDS3())
                    {
                        StatusText.Text = "Connected to DS3";
                        StatusText.Foreground = System.Windows.Media.Brushes.Green;
                        SetMinimalMode(true);
                        updateTimer.Start();
                        fogGateUpdateTimer.Start();
                        retryTimer.Stop();
                    }
                };
                retryTimer.Start();
            }
        }

        private void LoadSpoilerLogData()
        {
            System.Diagnostics.Debug.WriteLine("[MainWindow] Loading spoiler log data...");
            File.AppendAllText("ds3_debug.log", "[MainWindow] Loading spoiler log data...\n");
            
            var currentSeed = fogGateService.GetCurrentSeed();
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Current seed: {currentSeed ?? "null"}");
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Has fog randomizer data: {fogGateService.HasFogRandomizerData()}");
            File.AppendAllText("ds3_debug.log", $"[MainWindow] Current seed: {currentSeed ?? "null"}, Has data: {fogGateService.HasFogRandomizerData()}\n");
            
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
                return;
            }

            var position = memoryReader.GetPlayerPosition();
            var mapId = memoryReader.GetCurrentMapId();
            var currentArea = areaMapper.GetCurrentAreaName(mapId, position);

            // Update area display if changed
            if (currentArea != lastKnownArea)
            {
                lastKnownArea = currentArea;
                CurrentAreaText.Text = currentArea ?? "Unknown";
                // Force immediate fog gate update when area changes
                UpdateFogGatesDisplayWithDistances(currentArea);
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
                if (totalDistance > 2.0f && !string.IsNullOrEmpty(lastKnownArea))
                {
                    UpdateFogGatesDisplayWithDistances(lastKnownArea);
                    lastKnownPosition = position;
                    lastFogGateUpdate = DateTime.Now;
                }
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
            double forceUpdateInterval = hasActiveFogGates ? 1.0 : 3.0; // Very fast force updates
            if ((currentTime - lastFogGateUpdate).TotalSeconds > forceUpdateInterval)
            {
                shouldUpdate = true;
            }

            // Update fog gates if player moved significantly or if we need a periodic refresh
            if (shouldUpdate && !string.IsNullOrEmpty(lastKnownArea) && !isTooltipActive)
            {
                UpdateFogGatesDisplayWithDistances(lastKnownArea);
                lastKnownPosition = position;
                lastFogGateUpdate = currentTime;
                
                // Adjust update frequency for next cycle
                AdjustUpdateFrequency();
            }
        }



        private void UpdateFogGatesDisplayWithDistances(string? currentArea)
        {
            FogGatesPanel.Children.Clear();
            
            System.Diagnostics.Debug.WriteLine($"[MainWindow] UpdateFogGatesDisplayWithDistances called for area: {currentArea}");
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Has fog randomizer data: {fogGateService.HasFogRandomizerData()}");
            File.AppendAllText("ds3_debug.log", $"[MainWindow] UpdateFogGatesDisplayWithDistances called for area: {currentArea}, Has data: {fogGateService.HasFogRandomizerData()}\n");

            if (!fogGateService.HasFogRandomizerData())
            {
                System.Diagnostics.Debug.WriteLine("[MainWindow] No fog randomizer data found, displaying error message");
                File.AppendAllText("ds3_debug.log", "[MainWindow] No fog randomizer data found, displaying error message\n");
                var noDataText = new TextBlock
                {
                    Text = "No fog randomizer data found",
                    Style = (Style)FindResource("OverlayTextStyle")
                };
                FogGatesPanel.Children.Add(noDataText);
                return;
            }

            if (string.IsNullOrEmpty(currentArea))
            {
                System.Diagnostics.Debug.WriteLine("[MainWindow] Current area is null or empty");
                File.AppendAllText("ds3_debug.log", "[MainWindow] Current area is null or empty\n");
                var noAreaText = new TextBlock
                {
                    Text = "Current area unknown",
                    Style = (Style)FindResource("OverlayTextStyle")
                };
                FogGatesPanel.Children.Add(noAreaText);
                return;
            }

            // Get fog gates and warps in current area
            var fogGatesInArea = fogGateService.GetFogGatesInArea(currentArea);
            var warpsInArea = fogGateService.GetWarpsInArea(currentArea);
            var connectionsInArea = fogGateService.GetConnectionsInArea(currentArea);

            System.Diagnostics.Debug.WriteLine($"[MainWindow] Found {fogGatesInArea.Count} fog gates, {warpsInArea.Count} warps, {connectionsInArea.Count} connections in area: {currentArea}");
            File.AppendAllText("ds3_debug.log", $"[MainWindow] Found {fogGatesInArea.Count} fog gates, {warpsInArea.Count} warps, {connectionsInArea.Count} connections in area: {currentArea}\n");

            bool hasAnyGates = fogGatesInArea.Any() || warpsInArea.Any();

            if (!hasAnyGates)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] No fog gates found in area: {currentArea}");
                File.AppendAllText("ds3_debug.log", $"[MainWindow] No fog gates found in area: {currentArea}\n");
                var noGatesText = new TextBlock
                {
                    Text = "No fog gates found in this area",
                    Style = (Style)FindResource("OverlayTextStyle")
                };
                FogGatesPanel.Children.Add(noGatesText);
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[MainWindow] Displaying fog gates for area: {currentArea}");
            File.AppendAllText("ds3_debug.log", $"[MainWindow] Displaying fog gates for area: {currentArea}\n");

            // Display fog gates
            foreach (var fogGate in fogGatesInArea)
            {
                var connection = fogGateService.GetConnectionForFogGate(fogGate.Area, fogGate.Id);
                var displayText = BuildFogGateDisplayText(fogGate, connection);
                
                var gateContainer = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(10, 1, 0, 3)
                };

                var gateNameText = new TextBlock
                {
                    Text = displayText,
                    Style = (Style)FindResource("FogGateTextStyle"),
                    TextWrapping = TextWrapping.Wrap
                };

                // Highlight boss fog gates
                if (fogGate.IsBoss)
                {
                    gateNameText.Foreground = System.Windows.Media.Brushes.Gold;
                }

                gateContainer.Children.Add(gateNameText);
                
                // Add connection info if available
                if (connection != null)
                {
                    var connectionText = new TextBlock
                    {
                        Text = $"   → {connection.ToArea}",
                        Style = (Style)FindResource("DistanceTextStyle"),
                        Margin = new Thickness(0, 1, 0, 0)
                    };
                    gateContainer.Children.Add(connectionText);
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
                    Style = (Style)FindResource("FogGateTextStyle"),
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = System.Windows.Media.Brushes.LightBlue
                };

                warpContainer.Children.Add(warpNameText);
                
                // Add connection info if available
                if (connection != null)
                {
                    var connectionText = new TextBlock
                    {
                        Text = $"   → {connection.ToArea}",
                        Style = (Style)FindResource("DistanceTextStyle"),
                        Margin = new Thickness(0, 1, 0, 0)
                    };
                    warpContainer.Children.Add(connectionText);
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
                // Stop timers temporarily
                updateTimer.Stop();
                fogGateUpdateTimer.Stop();

                // Reload fog gate service data
                fogGateService.RefreshData();

                File.AppendAllText("ds3_debug.log",
                    $"[MainWindow] {DateTime.Now:HH:mm:ss.fff} - Services restarted with new paths\n");

                // Restart timers
                updateTimer.Start();
                fogGateUpdateTimer.Start();

                // Force a refresh
                LoadSpoilerLogData();
                UpdateFogGatesDisplayWithDistances(lastKnownArea);
            }
            catch (Exception ex)
            {
                File.AppendAllText("ds3_debug.log",
                    $"[MainWindow] {DateTime.Now:HH:mm:ss.fff} - Error restarting services: {ex.Message}\n");
                
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
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            updateTimer?.Stop();
            memoryReader?.Detach();
            base.OnClosed(e);
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Allow dragging the window when clicking anywhere on it
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
    }
}