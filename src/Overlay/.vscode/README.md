# DS3 Fog Randomizer Overlay - Development Setup

## VS Code Configuration

This project includes comprehensive VS Code configuration for C# WPF development.

### Quick Start

1. Open the workspace file: `DS3FogRandoOverlay.code-workspace`
2. Install recommended extensions when prompted
3. Press `F5` to build and run with debugging
4. Use `Ctrl+Shift+P` → "Tasks: Run Task" to access build tasks

### Available Run Configurations

#### Debug Configurations (F5 menu)
- **Launch DS3 Fog Randomizer Overlay** - Build and run with debugging
- **Launch DS3 Fog Randomizer Overlay (No Build)** - Run without building (faster)
- **Launch DS3 Fog Randomizer Overlay (Debug Mode)** - Run with debug environment variables
- **Attach to DS3 Fog Randomizer Overlay** - Attach debugger to running process

#### Tasks (Ctrl+Shift+P → "Tasks: Run Task")
- **build** - Build debug configuration (default: Ctrl+Shift+B)
- **build-release** - Build release configuration
- **clean** - Clean build artifacts
- **restore** - Restore NuGet packages
- **run** - Build and run debug version
- **run-release** - Build and run release version
- **publish** - Create self-contained executable in ./publish folder
- **watch** - Watch for file changes and auto-rebuild

### Keyboard Shortcuts

- `F5` - Start debugging
- `Ctrl+F5` - Run without debugging
- `Shift+F5` - Stop debugging
- `Ctrl+Shift+B` - Build project
- `Ctrl+Shift+P` - Command palette
- `Ctrl+Shift+~` - Open terminal

### Project Structure

```
DS3FogRandoOverlay/
├── .vscode/                 # VS Code configuration
│   ├── launch.json         # Debug configurations
│   ├── tasks.json          # Build tasks
│   ├── settings.json       # Editor settings
│   └── extensions.json     # Recommended extensions
├── Services/               # Core services
│   ├── DS3MemoryReader.cs  # Memory access with CT addresses
│   ├── AreaMapper.cs       # Area and fog gate mapping
│   ├── SpoilerLogParser.cs # Spoiler log parsing
│   └── ConfigurationService.cs
├── Models/                 # Data models
│   └── FogGate.cs
├── MainWindow.xaml         # Main overlay window
├── MainWindow.xaml.cs      # Window code-behind with drag support
└── App.xaml               # Application entry point
```

### Memory Reading Features

The overlay uses memory addresses extracted from DS3_TGA_v3.4.0.CT:
- **Player Position**: WorldChrMan + 80 + 28 + 40/84/88 (X/Z/Y)
- **Area ID**: DarkSoulsIII.exe + 47451D0
- **Player Stats**: WorldChrMan + 80 + 1F90 + 18 + D8/E4 (Health/Focus)

### Debugging Tips

1. **Memory Reading Issues**: Use "Launch (Debug Mode)" to enable memory debug output
2. **Window Positioning**: Right-click overlay → "Reset Position" or press `Ctrl+R`
3. **Performance**: Use "Launch (No Build)" for faster iteration
4. **Process Attachment**: Use "Attach" configuration if overlay is already running

### Build Outputs

- **Debug**: `bin/Debug/net8.0-windows/`
- **Release**: `bin/Release/net8.0-windows/`
- **Publish**: `publish/` (self-contained executable)

### Recommended Extensions

The project will prompt to install these extensions:
- **C# Dev Kit** - Essential C# support
- **C#** - Language support and IntelliSense
- **PowerShell** - Terminal support
- **GitLens** - Enhanced Git integration
- **Code Spell Checker** - Spell checking in comments

### Environment Variables

- `DS3_DEBUG=true` - Enable debug mode with verbose logging
- Set automatically when using "Debug Mode" launch configuration

### Troubleshooting

- **Build Errors**: Run "restore" task to restore NuGet packages
- **IntelliSense Issues**: Reload window (Ctrl+Shift+P → "Developer: Reload Window")
- **Debugger Won't Attach**: Ensure DS3FogRandoOverlay.exe is running as administrator
- **Memory Access Fails**: Check if Dark Souls III is running and accessible

### Contributing

1. Use the provided code formatting settings
2. Run "build" task before committing
3. Test with both debug and release configurations
4. Update this README if adding new configurations
