# DS3 Fog Randomizer Overlay

A lightweight C# .NET 8 WPF overlay application for Dark Souls 3 that provides real-time fog gate information from the Fog Randomizer mod. Designed for streamers and players who want to track fog gate connections during randomized runs.

## Features

### Core Functionality
- **Real-time Fog Gate Display**: Shows all fog gates and warps in your current area
- **Distance Calculation**: Accurate distance measurements to nearby fog gates
- **Memory Reading**: Reads DS3 process memory to detect current player location and area
- **Spoiler Log Integration**: Automatically parses the latest spoiler log files
- **Boss Highlighting**: Special gold highlighting for boss fog gates
- **Connection Information**: Shows destination areas and connection types (random vs original)

### Streaming Features
- **OBS-Ready**: Transparent background with clean, professional appearance
- **Minimal Mode**: Automatically hides non-essential UI when connected to DS3
- **Compact Design**: Small footprint that doesn't obstruct gameplay
- **Smart Layout**: Automatically adjusts layout based on fog gate name length
- **Hidden Scrollbars**: Clean appearance for streaming without visual clutter

### User Interface
- **Resizable Window**: Drag bottom-right corner to resize
- **Persistent Settings**: Automatically saves window size and position
- **Context Menu**: Right-click access to all settings and options
- **Keyboard Shortcuts**: F9 to reset position, Esc to exit
- **Toggle Spoiler Info**: Show/hide destination information on demand

## Requirements

- **Dark Souls 3** (Steam version recommended & needs 1.15 crashfix)
- **DS3 Fog Randomizer mod** installed and configured
- **.NET 8.0 Runtime** (Windows Desktop Runtime)
- **Windows 10/11** operating system

## Installation

### Quick Start
1. Download the latest release from the [releases page](https://github.com/Drommedhar/DS3FogRandoOverlay/releases)
2. Extract the files to a folder of your choice
3. Ensure Dark Souls 3 and the DS3 Fog Randomizer mod are installed
4. Run `DS3FogRandoOverlay.exe`

### Building from Source
```bash
git clone https://github.com/Drommedhar/DS3FogRandoOverlay.git
cd DS3FogRandoOverlay
dotnet build --configuration Release
```

#### Project Structure
- **`src/Overlay/`**: Main WPF application
- **`src/DS3Parser/`**: Library for parsing fog randomizer data
- **`src/SoulsFormats/`**: Data format parsers for Dark Souls files
- **`src/Tests/`**: Unit tests and integration tests

## Usage

### First Run
1. **Start Dark Souls 3** with the Fog Randomizer mod enabled
2. **Launch the overlay** (`DS3FogRandoOverlay.exe`)
3. The application will automatically:
   - Search for DS3 installation in common Steam directories
   - Detect the running DS3 process
   - Load the latest spoiler log from the fog randomizer
   - Display fog gate information for your current area

### Interface Modes
- **Setup Mode** (DS3 not running): Shows connection status, current area, and settings
- **Minimal Mode** (DS3 connected): Shows only fog gates and essential info for clean streaming

### Data Sources
The overlay automatically searches for DS3 and fog randomizer data in these locations:
- `C:\Program Files (x86)\Steam\steamapps\common\DARK SOULS III\Game\fog\`
- `C:\Program Files\Steam\steamapps\common\DARK SOULS III\Game\fog\`
- `D:\SteamLibrary\steamapps\common\DARK SOULS III\Game\fog\`
- `E:\SteamLibrary\steamapps\common\DARK SOULS III\Game\fog\`

## Controls and Navigation

### Keyboard Shortcuts
- **F9**: Reset window position to top-right corner
- **Esc**: Exit application

### Mouse Controls
- **Drag window header**: Move the overlay window
- **Drag bottom-right corner**: Resize the window
- **Right-click anywhere**: Access context menu with all options
- **Mouse wheel**: Scroll through fog gates (when list is longer than window)

### Context Menu Options
- **Show/Hide Spoiler Information**: Toggle destination area display (NOTE: Currently disabled, as its not working correctly)
- **Settings**: Open configuration window
- **Reset Position**: Move window to top-right corner
- **Exit**: Close the application

## OBS Integration for Streaming

### Setting up Window Capture
1. **Add a Window Capture Source**:
   - In OBS, click the "+" in Sources
   - Select "Window Capture"
   - Name it "DS3 Fog Overlay"

2. **Configure Capture Settings**:
   - **Window**: Select `[DS3FogRandoOverlay.exe]: DS3 Fog Randomizer Overlay`
   - **Capture Method**: `Windows 10 (1903 and up)`
   - **Window Match Priority**: `Match title, otherwise find window of same type`
   - **✓ Client Area**: Enable for cleaner capture
   - **✗ Capture Cursor**: Disable (recommended)

3. **Position and Size**:
   - Drag the overlay to your desired position in-game
   - Resize using the bottom-right corner grip
   - The overlay will remember your preferences

### Streaming Tips
- **Minimal Footprint**: In minimal mode, the overlay uses very little screen space
- **Professional Look**: Clean, readable text with proper contrast
- **No Clutter**: Status information is hidden when connected to DS3
- **Smooth Updates**: Distance calculations update smoothly without flickering

## Display Information

### Fog Gates
- **🚪 Regular Gates**: Standard fog gates leading to different areas
- **👑 Boss Gates**: Boss fog gates highlighted in gold
- **Distance**: Real-time distance in game units (when available)
- **Destination**: Shows target area when spoiler information is enabled

### Warps
- **🔵 Warp Points**: Bonfire and other warp locations
- **Connection Type**: Shows if the connection is randomized or original
- **Area Information**: Displays destination area and scaling percentages

### Visual Indicators
- **Gold Text**: Boss fog gates
- **White Text**: Regular fog gates  
- **Light Blue Text**: Warp points
- **Cyan Text**: Distance information
- **Green Text**: Destination areas
- **Orange Text**: Randomized connections
- **Gray Text**: Original connections

## Technical Details

### Memory Reading
The application uses Windows API calls to read Dark Souls 3 process memory:
- **Player Position**: X, Y, Z coordinates for distance calculations
- **Map ID**: Current area identification
- **Real-time Updates**: Continuous monitoring of player location

### Data Parsing
- **Fog Distribution**: Parses `fog.txt` for gate locations and properties
- **Spoiler Logs**: Reads randomizer output files for connection mapping
- **Area Mapping**: Converts map IDs to human-readable area names
- **Distance Calculation**: Uses 3D math for accurate distance measurements

### File Locations
- **Configuration**: `%APPDATA%\DS3FogRandoOverlay\config.json`
- **Debug Logs**: `ds3_debug.log` (in application directory)
- **Spoiler Logs**: Automatically detected in DS3 fog randomizer directory

## Architecture

### Core Components
- **MainWindow.xaml**: WPF user interface and overlay presentation
- **DS3MemoryReader**: Process memory reading and player position tracking
- **FogGateService**: Fog gate data management and distance calculations
- **ConfigurationService**: Settings persistence and path management
- **AreaMapper**: Map ID to area name conversion

### Data Flow
1. **Memory Reader** → Gets player position and map ID from DS3 process
2. **Area Mapper** → Converts map ID to area name
3. **Fog Gate Service** → Retrieves fog gates for current area
4. **Distance Calculator** → Computes distances to nearby gates
5. **UI Layer** → Displays sorted fog gates with visual indicators

### Libraries Used
- **WPF**: User interface framework
- **Newtonsoft.Json**: Configuration serialization
- **System.Diagnostics.Process**: Process monitoring and memory reading
- **YamlDotNet**: YAML parsing for configuration files
- **SoulsFormats**: Dark Souls file format parsing

## Troubleshooting

### Common Issues

#### "DS3 not running" Error
- Ensure Dark Souls 3 is actually running
- Make sure you're using the Steam version (other versions may have different memory layouts)
- Try running the overlay as administrator
- Check that DS3 is not running in compatibility mode

#### "No fog randomizer data found" Error
- Verify the DS3 Fog Randomizer mod is properly installed
- Check that spoiler log files exist in the `fog/spoiler_logs` directory
- Ensure the randomizer has generated at least one run
- Try running a new randomization to generate fresh data

#### Overlay not displaying fog gates
- Make sure you're in an area with fog gates
- Check the debug log (`ds3_debug.log`) for detailed error information
- Verify your current area is recognized by the area mapper
- Try restarting the overlay after loading a save file

#### Distance calculations not working
- Ensure DS3 is running and the overlay is connected
- Check that your game version matches the expected memory offsets
- Try moving in-game to trigger position updates
- Restart the overlay if distances seem stuck

### Debug Information
- **Log File**: `ds3_debug.log` contains detailed debugging information
- **Configuration**: Check `%APPDATA%\DS3FogRandoOverlay\config.json` for settings

### Performance Tips
- The overlay uses minimal system resources
- Memory reading is optimized for minimal DS3 performance impact

## Known Limitations

### Memory Offsets
- Memory offsets may need updates for different DS3 versions
- The application is primarily tested with the Steam version (1.15 crashfix)
- Some game updates may break memory reading functionality

### Area Detection
- Area detection relies on map IDs and may not be 100% accurate in all locations
- Some transition areas may not be recognized immediately
- Custom or modded areas may not be properly detected

### Fog Randomizer Compatibility
- Requires the standard DS3 Fog Randomizer mod
- Other randomizer variants may not be compatible
- Spoiler log format changes may break parsing

## Contributing

We welcome contributions to improve the DS3 Fog Randomizer Overlay!

### Development Setup
1. Clone the repository
2. Install .NET 8.0 SDK
3. Open `DS3FogRandoOverlay.sln` in Visual Studio or your preferred IDE
4. Build and run the solution

### Submitting Changes
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes and add tests if applicable
4. Commit your changes (`git commit -m 'Add amazing feature'`)
5. Push to the branch (`git push origin feature/amazing-feature`)
6. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Disclaimer

This application reads game memory for educational and personal use. It is designed specifically for single-player use with the DS3 Fog Randomizer mod and streaming purposes. Use responsibly and in accordance with the game's terms of service.

## Acknowledgments

- **DS3 Fog Randomizer**: Created by the Dark Souls modding community
- **SoulsFormats**: TKGP and contributors for the Dark Souls file format library
- **Dark Souls Community**: For continued support and testing
- **Streamers and Players**: Who provided feedback and feature requests

---

*For additional support, bug reports, or feature requests, please visit the [GitHub Issues](https://github.com/Drommedhar/DS3FogRandoOverlay/issues) page.*
