# DS3 Fog Randomizer Overlay

A lightweight C# .NET 8 WPF overlay application for Dark Souls 3 that provides real-time fog gate information from the Fog Randomizer mod. Optimized for OBS streaming and recording.

## Features

- **Ultra-Minimal Design**: Clean, compact overlay perfect for streaming
- **OBS-Ready**: Transparent background with hidden scrollbars for clean capture
- **Real-time Display**: Shows fog gates with accurate distances in your current area
- **Smart Layout**: Automatically adjusts layout for long gate names
- **Memory Reading**: Reads DS3 process memory to detect current player location
- **Spoiler Log Integration**: Automatically parses the latest spoiler log
- **Boss Highlighting**: Special highlighting for boss fog gates
- **Persistent Settings**: Remembers window size and position
- **Resizable**: Adjustable window size with visual resize grip
- **Hover Tooltips**: Detailed spoiler log information when hovering over fog gates

## Requirements

- Dark Souls 3 (Steam version recommended)
- DS3 Fog Randomizer mod installed
- .NET 8.0 Runtime
- Windows operating system

## Installation

1. Download the latest release or build from source
2. Ensure Dark Souls 3 and the Fog Randomizer mod are installed
3. Run `DS3FogRandoOverlay.exe`

## Building from Source

```bash
git clone <repository-url>
cd DS3FogRandoOverlay
dotnet build
dotnet run
```

## Usage

### Basic Usage
1. Start Dark Souls 3 with the Fog Randomizer mod
2. Launch the overlay application
3. The overlay will automatically:
   - Detect the running DS3 process
   - Load the latest spoiler log
   - Display fog gate information for your current area
   - Switch to minimal mode when connected to DS3

### Interface Modes
- **Setup Mode** (not connected to DS3): Shows status, area info, and settings
- **Minimal Mode** (connected to DS3): Shows only fog gates for clean streaming

## OBS Configuration

To capture the overlay in OBS for streaming/recording:

1. **Add Window Capture Source**:
   - Click the "+" in Sources
   - Select "Window Capture"
   - Name it "DS3 Fog Overlay"

2. **Configure Capture Settings**:
   - **Window**: Select `[DS3FogRandoOverlay.exe]: DS3 Fog Randomizer Overlay`
   - **Capture Method**: `Windows 10 (1903 and up)`
   - **Window Match Priority**: `Match title, otherwise find window of same type`
   - **✓ Client Area**: Check this box
   - **✓ Capture Cursor**: Uncheck (optional)

3. **Position the Overlay**:
   - Drag the overlay window to your desired position in the game
   - Resize using the bottom-right corner grip
   - The overlay will remember your size and position

4. **Tips for Best Results**:
   - Use a small, compact size for minimal screen intrusion
   - Position in a corner or edge where it won't cover important game elements
   - The overlay automatically hides non-essential info when connected to DS3

## Controls

### Keyboard Shortcuts
- **F9**: Reset window position to top-right corner
- **Esc**: Exit application

### Mouse Controls
- **Drag header**: Move window
- **Drag bottom-right corner**: Resize window
- **Right-click**: Access context menu with all options
- **Mouse wheel**: Scroll through fog gates (scrollbar hidden for clean appearance)
- **Hover over fog gates**: Show detailed spoiler log information in tooltips

## Configuration

### Automatic Detection
The application automatically searches for DS3 and Fog Randomizer in common locations:
- `C:\Program Files (x86)\Steam\steamapps\common\DARK SOULS III\Game\fog`
- `C:\Program Files\Steam\steamapps\common\DARK SOULS III\Game\fog`
- `D:\SteamLibrary\steamapps\common\DARK SOULS III\Game\fog`
- `E:\SteamLibrary\steamapps\common\DARK SOULS III\Game\fog`

### Settings Storage
- Window size and position are automatically saved
- Configuration stored in `%APPDATA%\DS3FogRandoOverlay\config.json`
- Settings accessible via right-click context menu

## Technical Details

### Memory Reading
Uses Windows API calls to read Dark Souls 3 process memory for:
- Player position and movement detection
- Current map/area identification
- Real-time distance calculations

### Spoiler Log Parsing
Parses spoiler log files to extract:
- Fog gate connections between areas
- Boss locations (highlighted in orange)
- Distance calculations to nearby gates
- Area scaling information

### Smart Display Features
- **Adaptive Layout**: Horizontal layout for short names, vertical for long names
- **Distance Always Visible**: Smart text handling ensures distance info is never cut off
- **Location Matching**: Gates matching your current location are highlighted in green
- **Real-time Updates**: Distance updates as you move through the world
- **Rich Tooltips**: Hover over fog gates to see detailed spoiler log information including:
  - Destination area details and scaling percentage
  - Boss area indicators
  - Connection information (random vs preexisting)
  - Multiple connection paths from/to the area

## OBS Streaming Tips

- **Minimal Footprint**: The overlay uses very little screen space in minimal mode
- **Transparent Background**: Blends seamlessly with game footage
- **No Visual Clutter**: Hidden scrollbars and status info when connected
- **Consistent Updates**: Smooth distance tracking without flickering
- **Professional Appearance**: Clean, readable text with proper shadows

## Known Limitations

- Memory offsets may need updates for different DS3 versions
- Area detection could be improved with better coordinate mapping
- Requires proper DS3 Fog Randomizer installation

## Contributing

Contributions are welcome! Areas for improvement:
- Better memory offset detection
- Improved area mapping accuracy
- Additional streaming features
- UI enhancements

## License

This project is provided as-is for educational and personal use.

## Disclaimer

This application reads game memory and should be used responsibly. It's designed for single-player use with the Fog Randomizer mod and streaming purposes.
