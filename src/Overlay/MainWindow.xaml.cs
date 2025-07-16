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
        private readonly DispatcherTimer updateTimer;
        private readonly DispatcherTimer fogGateUpdateTimer;

        private string? lastKnownArea;
        private DS3FogRandoOverlay.Services.Vector3? lastKnownPosition;

        private DateTime lastFogGateUpdate = DateTime.MinValue;
        private bool hasActiveFogGates = false;

        // Track expanded fog gates for click-to-show functionality
        private HashSet<string> expandedFogGates = new HashSet<string>();
        private bool isProcessingClick = false;

        public MainWindow()
        {
            InitializeComponent();

            configurationService = new ConfigurationService();
            memoryReader = new DS3MemoryReader();
            fogGateService = new FogGateService(configurationService);
            areaMapper = new AreaMapper();

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
            
            var currentSeed = fogGateService.GetCurrentSeed();
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Current seed: {currentSeed ?? "null"}");
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Has fog randomizer data: {fogGateService.HasFogRandomizerData()}");
            
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
            double forceUpdateInterval = 10.0; // Much longer force update interval
            if ((currentTime - lastFogGateUpdate).TotalSeconds > forceUpdateInterval)
            {
                shouldUpdate = true;
            }

            // Update fog gates if player moved significantly or if we need a periodic refresh
            if (shouldUpdate && !string.IsNullOrEmpty(lastKnownArea) && !isProcessingClick)
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

            if (!fogGateService.HasFogRandomizerData())
            {
                System.Diagnostics.Debug.WriteLine("[MainWindow] No fog randomizer data found, displaying error message");
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

            bool hasAnyGates = fogGatesInArea.Any() || warpsInArea.Any();

            if (!hasAnyGates)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] No fog gates found in area: {currentArea}");
                var noGatesText = new TextBlock
                {
                    Text = "No fog gates found in this area",
                    Style = (Style)FindResource("OverlayTextStyle")
                };
                FogGatesPanel.Children.Add(noGatesText);
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[MainWindow] Displaying fog gates for area: {currentArea}");

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
                    Style = (Style)FindResource("OverlayTextStyle"),
                    Foreground = fogGate.IsBoss ? System.Windows.Media.Brushes.Gold : System.Windows.Media.Brushes.White,
                    FontSize = 12,
                    Margin = new Thickness(0, 1, 0, 1),
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                // Add click-to-show destination functionality
                if (connection != null)
                {
                    var gateKey = $"{fogGate.Area}_{fogGate.Id}";
                    
                    System.Diagnostics.Debug.WriteLine($"[MainWindow] Added click handler for fog gate: {fogGate.Text ?? fogGate.Name} -> {connection.ToArea}");
                    
                    // Add click handler using MouseLeftButtonDown event
                    gateNameText.MouseLeftButtonDown += (s, e) => 
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainWindow] ===== CLICK EVENT FIRED =====");
                        System.Diagnostics.Debug.WriteLine($"[MainWindow] Fog gate clicked: {gateKey}");
                        System.Diagnostics.Debug.WriteLine($"[MainWindow] Sender: {s?.GetType().Name}");
                        System.Diagnostics.Debug.WriteLine($"[MainWindow] EventArgs: {e?.GetType().Name}");
                        System.Diagnostics.Debug.WriteLine($"[MainWindow] isProcessingClick: {isProcessingClick}");
                        ToggleFogGateExpansion(gateKey);
                        e.Handled = true; // Prevent event bubbling
                    };
                    
                    // Add hover effects
                    gateNameText.MouseEnter += (s, e) => 
                    {
                        gateNameText.Opacity = 0.8;
                        gateNameText.TextDecorations = TextDecorations.Underline;
                    };
                    
                    gateNameText.MouseLeave += (s, e) => 
                    {
                        gateNameText.Opacity = 1.0;
                        gateNameText.TextDecorations = null;
                    };
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[MainWindow] No connection found for fog gate: {fogGate.Text ?? fogGate.Name}");
                    gateNameText.Cursor = System.Windows.Input.Cursors.Arrow;
                }

                gateContainer.Children.Add(gateNameText);
                
                // Add destination info if gate is expanded
                if (connection != null)
                {
                    var gateKey = $"{fogGate.Area}_{fogGate.Id}";
                    if (expandedFogGates.Contains(gateKey))
                    {
                        var destinationText = new TextBlock
                        {
                            Text = $"   → Leads to: {connection.ToArea}",
                            Style = (Style)FindResource("DistanceTextStyle"),
                            Foreground = Brushes.LightGreen,
                            Margin = new Thickness(15, 2, 0, 2),
                            FontStyle = FontStyles.Italic
                        };
                        gateContainer.Children.Add(destinationText);
                        
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
                    Margin = new Thickness(0, 1, 0, 1),
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                // Add click-to-show destination functionality
                if (connection != null)
                {
                    var warpKey = $"{warp.Area}_{warp.Id}_warp";
                    
                    System.Diagnostics.Debug.WriteLine($"[MainWindow] Added click handler for warp: {warp.Text ?? $"Warp {warp.Id}"} -> {connection.ToArea}");
                    
                    // Add click handler using MouseLeftButtonDown event
                    warpNameText.MouseLeftButtonDown += (s, e) => 
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainWindow] ===== CLICK EVENT FIRED =====");
                        System.Diagnostics.Debug.WriteLine($"[MainWindow] Warp clicked: {warpKey}");
                        System.Diagnostics.Debug.WriteLine($"[MainWindow] Sender: {s?.GetType().Name}");
                        System.Diagnostics.Debug.WriteLine($"[MainWindow] EventArgs: {e?.GetType().Name}");
                        System.Diagnostics.Debug.WriteLine($"[MainWindow] isProcessingClick: {isProcessingClick}");
                        ToggleFogGateExpansion(warpKey);
                        e.Handled = true; // Prevent event bubbling
                    };
                    
                    // Add hover effects
                    warpNameText.MouseEnter += (s, e) => 
                    {
                        warpNameText.Opacity = 0.8;
                        warpNameText.TextDecorations = TextDecorations.Underline;
                    };
                    
                    warpNameText.MouseLeave += (s, e) => 
                    {
                        warpNameText.Opacity = 1.0;
                        warpNameText.TextDecorations = null;
                    };
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[MainWindow] No connection found for warp: {warp.Text ?? $"Warp {warp.Id}"}");
                    warpNameText.Cursor = System.Windows.Input.Cursors.Arrow;
                }

                warpContainer.Children.Add(warpNameText);
                
                // Add destination info if warp is expanded
                if (connection != null)
                {
                    var warpKey = $"{warp.Area}_{warp.Id}_warp";
                    if (expandedFogGates.Contains(warpKey))
                    {
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

        private void ToggleFogGateExpansion(string gateKey)
        {
            isProcessingClick = true;
            
            // Stop both timers during click processing
            updateTimer.Stop();
            fogGateUpdateTimer.Stop();
            
            if (expandedFogGates.Contains(gateKey))
            {
                expandedFogGates.Remove(gateKey);
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Collapsed fog gate: {gateKey}");
            }
            else
            {
                expandedFogGates.Add(gateKey);
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Expanded fog gate: {gateKey}");
            }
            
            // Refresh the display to show/hide the destination info
            UpdateFogGatesDisplayWithDistances(lastKnownArea);
            
            // Allow updates to resume after a longer delay
            var resumeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000) // Wait 1 second before resuming updates
            };
            resumeTimer.Tick += (s, e) =>
            {
                isProcessingClick = false;
                updateTimer.Start();
                fogGateUpdateTimer.Start();
                resumeTimer.Stop();
            };
            resumeTimer.Start();
        }
    }
}