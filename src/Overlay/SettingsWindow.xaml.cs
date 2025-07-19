using DS3FogRandoOverlay.Services;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DS3FogRandoOverlay
{
    public partial class SettingsWindow : Window
    {
        private readonly ConfigurationService configurationService;
        private string originalPath;
        private bool originalTransparentBackground;

        public bool SettingsChanged { get; private set; } = false;

        public SettingsWindow(ConfigurationService configurationService)
        {
            InitializeComponent();
            this.configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            
            originalPath = configurationService.Config.DarkSouls3Path;
            originalTransparentBackground = configurationService.Config.TransparentBackground;
            LoadSettings();
            UpdatePathStatus();
        }

        private void LoadSettings()
        {
            DarkSouls3PathTextBox.Text = configurationService.Config.DarkSouls3Path;
            TransparentBackgroundCheckBox.IsChecked = configurationService.Config.TransparentBackground;
        }

        private void UpdatePathStatus()
        {
            var ds3Path = DarkSouls3PathTextBox.Text;
            
            if (string.IsNullOrWhiteSpace(ds3Path))
            {
                StatusTextBlock.Text = "Please select a Dark Souls 3 path";
                StatusTextBlock.Foreground = Brushes.Orange;
                FogPathTextBlock.Text = "N/A";
                EventsPathTextBlock.Text = "N/A";
                return;
            }

            if (!Directory.Exists(ds3Path))
            {
                StatusTextBlock.Text = "Path does not exist";
                StatusTextBlock.Foreground = Brushes.Red;
                FogPathTextBlock.Text = "N/A";
                EventsPathTextBlock.Text = "N/A";
                return;
            }

            // Check if this looks like a Dark Souls 3 directory
            var gameExe = Path.Combine(ds3Path, "Game", "DarkSoulsIII.exe");
            if (!File.Exists(gameExe))
            {
                StatusTextBlock.Text = "This doesn't appear to be a Dark Souls 3 directory";
                StatusTextBlock.Foreground = Brushes.Orange;
                FogPathTextBlock.Text = "N/A";
                EventsPathTextBlock.Text = "N/A";
                return;
            }

            try
            {
                // Use the new dynamic detection system
                var parser = new DS3Parser.DS3Parser();
                var gameDirectory = Path.Combine(ds3Path, "Game");
                
                if (parser.HasFogRandomizerData(gameDirectory))
                {
                    StatusTextBlock.Text = "Valid Dark Souls 3 path with fog randomizer detected";
                    StatusTextBlock.Foreground = Brushes.Green;
                    
                    var fogModDirectory = parser.FindFogRandomizerDirectory(gameDirectory);
                    if (!string.IsNullOrEmpty(fogModDirectory))
                    {
                        FogPathTextBlock.Text = fogModDirectory;
                        FogPathTextBlock.Foreground = Brushes.Green;
                        
                        var eventsPath = Path.Combine(fogModDirectory, "fogdist", "events.txt");
                        if (File.Exists(eventsPath))
                        {
                            EventsPathTextBlock.Text = eventsPath;
                            EventsPathTextBlock.Foreground = Brushes.Green;
                        }
                        else
                        {
                            EventsPathTextBlock.Text = "Not found";
                            EventsPathTextBlock.Foreground = Brushes.Orange;
                        }
                    }
                    else
                    {
                        FogPathTextBlock.Text = "Detection error";
                        FogPathTextBlock.Foreground = Brushes.Red;
                        EventsPathTextBlock.Text = "N/A";
                        EventsPathTextBlock.Foreground = Brushes.Red;
                    }
                }
                else
                {
                    StatusTextBlock.Text = "Valid Dark Souls 3 path, but no fog randomizer detected";
                    StatusTextBlock.Foreground = Brushes.Orange;
                    FogPathTextBlock.Text = "Not found";
                    FogPathTextBlock.Foreground = Brushes.Orange;
                    EventsPathTextBlock.Text = "Not found";
                    EventsPathTextBlock.Foreground = Brushes.Orange;
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error checking path: {ex.Message}";
                StatusTextBlock.Foreground = Brushes.Red;
                FogPathTextBlock.Text = "Error";
                EventsPathTextBlock.Text = "Error";
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Dark Souls 3 Installation Directory",
                InitialDirectory = DarkSouls3PathTextBox.Text
            };

            if (dialog.ShowDialog() == true)
            {
                DarkSouls3PathTextBox.Text = dialog.FolderName;
                UpdatePathStatus();
            }
        }

        private void AutoDetectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var detectedPath = PathResolver.AutoDetectDarkSouls3Path();
                
                if (!string.IsNullOrEmpty(detectedPath))
                {
                    DarkSouls3PathTextBox.Text = detectedPath;
                    UpdatePathStatus();
                    MessageBox.Show(this, $"Auto-detected Dark Souls 3 at:\n{detectedPath}", "Auto-Detection Successful", 
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(this, "Could not auto-detect Dark Souls 3 installation.\n\nPlease browse for the installation directory manually.", 
                                  "Auto-Detection Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error during auto-detection:\n{ex.Message}", "Auto-Detection Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DiagnosticsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ds3Path = DarkSouls3PathTextBox.Text?.Trim();
                
                if (string.IsNullOrEmpty(ds3Path) || !Directory.Exists(ds3Path))
                {
                    MessageBox.Show(this, "Please select a valid Dark Souls 3 path first.", "Invalid Path", 
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var parser = new DS3Parser.DS3Parser();
                var gameDirectory = Path.Combine(ds3Path, "Game");
                var diagnostics = parser.GetFogRandomizerDiagnostics(gameDirectory);

                // Create a new window to show the diagnostics
                var diagnosticsWindow = new Window
                {
                    Title = "Fog Randomizer Diagnostics",
                    Width = 700,
                    Height = 500,
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Content = new ScrollViewer
                    {
                        Content = new TextBlock
                        {
                            Text = diagnostics,
                            FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                            Margin = new Thickness(10),
                            TextWrapping = TextWrapping.Wrap
                        }
                    }
                };

                diagnosticsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error generating diagnostics:\n{ex.Message}", "Diagnostics Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var newPath = DarkSouls3PathTextBox.Text?.Trim();
            
            if (string.IsNullOrEmpty(newPath))
            {
                MessageBox.Show(this, "Please select a Dark Souls 3 path.", "Invalid Path", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Directory.Exists(newPath))
            {
                MessageBox.Show(this, "The selected path does not exist.", "Invalid Path", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                configurationService.Config.DarkSouls3Path = newPath;
                configurationService.Config.TransparentBackground = TransparentBackgroundCheckBox.IsChecked ?? false;
                configurationService.SaveConfig();

                SettingsChanged = newPath != originalPath || configurationService.Config.TransparentBackground != originalTransparentBackground;

                if (SettingsChanged)
                {
                    MessageBox.Show(this, "Settings saved successfully!\n\nThe application will refresh to use the new settings.", 
                                  "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error saving settings:\n{ex.Message}", "Save Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void DarkSouls3PathTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdatePathStatus();
        }
    }
}
