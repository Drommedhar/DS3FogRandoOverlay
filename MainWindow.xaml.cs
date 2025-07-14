using DS3FogRandoOverlay.Models;
using DS3FogRandoOverlay.Services;
using System;
using System.Collections.Generic;
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
        private readonly SpoilerLogParser spoilerLogParser;
        private readonly AreaMapper areaMapper;
        private readonly DispatcherTimer updateTimer;

        private SpoilerLogData? currentSpoilerData;
        private string? lastKnownArea;
        private bool isOverlayVisible = true;

        public MainWindow()
        {
            InitializeComponent();

            memoryReader = new DS3MemoryReader();
            spoilerLogParser = new SpoilerLogParser();
            areaMapper = new AreaMapper();

            updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500) // Update every 500ms
            };
            updateTimer.Tick += UpdateTimer_Tick;

            InitializeOverlay();
        }

        private void InitializeOverlay()
        {
            // Position overlay in top-right corner
            this.Left = SystemParameters.PrimaryScreenWidth - this.Width - 20;
            this.Top = 20;

            // Load spoiler log data
            LoadSpoilerLogData();

            // Try to attach to DS3
            if (memoryReader.AttachToDS3())
            {
                StatusText.Text = "Connected to DS3";
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
                updateTimer.Start();
            }
            else
            {
                StatusText.Text = "DS3 not found";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;

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
                        updateTimer.Start();
                        retryTimer.Stop();
                    }
                };
                retryTimer.Start();
            }
        }

        private void LoadSpoilerLogData()
        {
            currentSpoilerData = spoilerLogParser.ParseLatestSpoilerLog();
            if (currentSpoilerData != null)
            {
                HeaderText.Text = $"DS3 Fog Randomizer (Seed: {currentSpoilerData.Seed})";
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
                return;
            }

            var position = memoryReader.GetPlayerPosition();
            var mapId = memoryReader.GetCurrentMapId();
            var currentArea = areaMapper.GetCurrentAreaName(mapId, position);

            if (currentArea != lastKnownArea)
            {
                lastKnownArea = currentArea;
                CurrentAreaText.Text = currentArea ?? "Unknown";
                UpdateFogGatesDisplay(currentArea);
            }
        }

        private void UpdateFogGatesDisplay(string? currentArea)
        {
            FogGatesPanel.Children.Clear();

            if (currentSpoilerData == null)
            {
                var noDataText = new TextBlock
                {
                    Text = "No spoiler log data found",
                    Style = (Style)FindResource("OverlayTextStyle")
                };
                FogGatesPanel.Children.Add(noDataText);
                return;
            }

            if (string.IsNullOrEmpty(currentArea))
            {
                var noAreaText = new TextBlock
                {
                    Text = "Current area unknown",
                    Style = (Style)FindResource("OverlayTextStyle")
                };
                FogGatesPanel.Children.Add(noAreaText);
                return;
            }

            // Debug logging
            System.IO.File.AppendAllText("ds3_debug.log",
                $"[MainWindow] {DateTime.Now:HH:mm:ss.fff} - Searching fog gates for area: {currentArea}\n");

            // Get all possible area names for the current area
            var possibleAreaNames = areaMapper.GetPossibleSpoilerLogAreaNames(currentArea);

            // Debug logging
            System.IO.File.AppendAllText("ds3_debug.log",
                $"[MainWindow] {DateTime.Now:HH:mm:ss.fff} - Possible area names: {string.Join(", ", possibleAreaNames)}\n");

            // Find fog gates for current area
            var fogGatesInArea = new List<FogGate>();

            // Look for fog gates that start from any of the possible area names
            foreach (var area in currentSpoilerData.Areas)
            {
                foreach (var fogGate in area.FogGates)
                {
                    // Check if fog gate matches any of the possible area names
                    bool foundMatch = false;
                    foreach (var possibleName in possibleAreaNames)
                    {
                        if (fogGate.FromArea.Equals(possibleName, StringComparison.OrdinalIgnoreCase) ||
                            FuzzyAreaMatch(fogGate.FromArea, possibleName))
                        {
                            fogGatesInArea.Add(fogGate);
                            foundMatch = true;
                            System.IO.File.AppendAllText("ds3_debug.log",
                                $"[MainWindow] {DateTime.Now:HH:mm:ss.fff} - Found fog gate match: '{fogGate.FromArea}' -> '{fogGate.ToArea}' (matched with '{possibleName}')\n");
                            break;
                        }
                    }

                    // Also try direct match with current area name if not found yet
                    if (!foundMatch &&
                        (fogGate.FromArea.Equals(currentArea, StringComparison.OrdinalIgnoreCase) ||
                         FuzzyAreaMatch(fogGate.FromArea, currentArea)))
                    {
                        fogGatesInArea.Add(fogGate);
                        System.IO.File.AppendAllText("ds3_debug.log",
                            $"[MainWindow] {DateTime.Now:HH:mm:ss.fff} - Found fog gate direct match: '{fogGate.FromArea}' -> '{fogGate.ToArea}'\n");
                    }
                }
            }

            // Debug logging
            System.IO.File.AppendAllText("ds3_debug.log",
                $"[MainWindow] {DateTime.Now:HH:mm:ss.fff} - Total fog gates found: {fogGatesInArea.Count}\n");

            if (fogGatesInArea.Count == 0)
            {
                var noGatesText = new TextBlock
                {
                    Text = "No fog gates found in this area",
                    Style = (Style)FindResource("OverlayTextStyle")
                };
                FogGatesPanel.Children.Add(noGatesText);
                return;
            }

            foreach (var fogGate in fogGatesInArea.OrderBy(fg => fg.IsBoss ? 0 : 1).ThenBy(fg => fg.ToArea))
            {
                var fogGateText = new TextBlock
                {
                    Text = $"→ {fogGate.ToArea}",
                    Style = fogGate.IsBoss ?
                        (Style)FindResource("BossGateTextStyle") :
                        (Style)FindResource("FogGateTextStyle")
                };

                if (!string.IsNullOrEmpty(fogGate.Description))
                {
                    string tooltipText = $"{fogGate.Description}\n" +
                                       $"Scaling: {fogGate.ScalingPercent}%\n" +
                                       $"Type: {(fogGate.IsRandom ? "Random" : "Preexisting")}\n" +
                                       $"From: {fogGate.FromArea}";

                    if (fogGate.IsBoss)
                    {
                        tooltipText += "\n★ BOSS FOG GATE ★";
                    }

                    fogGateText.ToolTip = tooltipText;
                }

                FogGatesPanel.Children.Add(fogGateText);
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

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            isOverlayVisible = !isOverlayVisible;

            if (isOverlayVisible)
            {
                this.Show();
                ToggleButton.Content = "Hide";
            }
            else
            {
                this.Hide();
                ToggleButton.Content = "Show";
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadSpoilerLogData();
            UpdateFogGatesDisplay(lastKnownArea);
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
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
                case System.Windows.Input.Key.F1:
                    // Toggle overlay visibility
                    ToggleButton_Click(sender, new RoutedEventArgs());
                    break;
                case System.Windows.Input.Key.F5:
                    // Refresh data
                    RefreshButton_Click(sender, new RoutedEventArgs());
                    break;
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
    }
}