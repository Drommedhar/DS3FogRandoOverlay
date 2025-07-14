using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;

namespace DS3FogRandoOverlay.Services
{
    public class DS3MemoryReader
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private const int PROCESS_READ = 0x0010;
        private Process? ds3Process;
        private IntPtr processHandle;
        private IntPtr baseAddress;

        // Memory offsets from the cheat table
        private readonly int[] mapIdOffsets = { 0x80, 0x1FE0 }; // BaseB + 80 + 1FE0
        private const string logFileName = "ds3_debug.log";

        public DS3MemoryReader()
        {
            processHandle = IntPtr.Zero;
        }

        public bool AttachToDS3()
        {
            try
            {
                ds3Process = Process.GetProcessesByName("DarkSoulsIII").FirstOrDefault();
                if (ds3Process == null)
                {
                    LogDebug("Dark Souls III process not found");
                    return false;
                }

                processHandle = OpenProcess(PROCESS_READ, false, ds3Process.Id);
                if (processHandle == IntPtr.Zero)
                {
                    LogDebug("Failed to open Dark Souls III process");
                    return false;
                }

                // Get base address of DarkSoulsIII.exe
                baseAddress = ds3Process.MainModule?.BaseAddress ?? IntPtr.Zero;
                if (baseAddress == IntPtr.Zero)
                {
                    LogDebug("Failed to get base address of DarkSoulsIII.exe");
                    return false;
                }

                LogDebug($"Successfully attached to Dark Souls III (PID: {ds3Process.Id}, Base: 0x{baseAddress.ToInt64():X})");
                return true;
            }
            catch (Exception ex)
            {
                LogDebug($"Error attaching to DS3: {ex.Message}");
                return false;
            }
        }

        public bool IsDS3Running()
        {
            try
            {
                return ds3Process != null && !ds3Process.HasExited;
            }
            catch
            {
                return false;
            }
        }

        public string? GetCurrentMapId()
        {
            if (!IsDS3Running() || processHandle == IntPtr.Zero)
                return null;

            try
            {
                // Calculate WorldChrMan base address (BaseB)
                // From cheat table: BaseB = DarkSoulsIII.exe+4768E78
                IntPtr worldChrManPtr = IntPtr.Add(baseAddress, 0x4768E78);

                // Read WorldChrMan pointer
                IntPtr worldChrManAddress = ReadPointer(worldChrManPtr);
                if (worldChrManAddress == IntPtr.Zero)
                {
                    LogDebug("WorldChrMan pointer is null");
                    return null;
                }

                LogDebug($"WorldChrMan address: 0x{worldChrManAddress.ToInt64():X}");

                // Apply offsets: +80, then +1FE0
                IntPtr firstOffset = IntPtr.Add(worldChrManAddress, 0x80);
                IntPtr secondOffsetPtr = ReadPointer(firstOffset);
                if (secondOffsetPtr == IntPtr.Zero)
                {
                    LogDebug("First offset pointer is null");
                    return null;
                }

                IntPtr mapIdAddress = IntPtr.Add(secondOffsetPtr, 0x1FE0);
                LogDebug($"Map ID address: 0x{mapIdAddress.ToInt64():X}");

                // Read the 4-byte map ID
                byte[] buffer = new byte[4];
                if (ReadProcessMemory(processHandle, mapIdAddress, buffer, 4, out int bytesRead) && bytesRead == 4)
                {
                    uint mapId = BitConverter.ToUInt32(buffer, 0);

                    // Parse the map ID according to Dark Souls 3 format
                    // The value 0x28000000 corresponds to m40_00_00_00 (Iudex Gundyr)
                    // The format appears to be: m[AA]_[BB]_[CC]_[DD] where each part is extracted differently

                    // Extract map parts - DS3 uses a specific encoding
                    byte a = (byte)(mapId & 0xFF);        // First byte
                    byte b = (byte)((mapId >> 8) & 0xFF); // Second byte  
                    byte c = (byte)((mapId >> 16) & 0xFF); // Third byte
                    byte d = (byte)((mapId >> 24) & 0xFF); // Fourth byte

                    string mapIdStr = $"m{d:D2}_{c:D2}_{b:D2}_{a:D2}";

                    LogDebug($"Raw map ID: 0x{mapId:X8} ({mapId}), Formatted: {mapIdStr}");
                    return mapIdStr;
                }
                else
                {
                    LogDebug($"Failed to read map ID. Bytes read: {bytesRead}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Error reading map ID: {ex.Message}");
                return null;
            }
        }

        public Vector3? GetPlayerPosition()
        {
            if (!IsDS3Running() || processHandle == IntPtr.Zero)
                return null;

            try
            {
                // For now, return null - this would require additional memory addresses
                // that aren't provided in the current cheat table
                return null;
            }
            catch (Exception ex)
            {
                LogDebug($"Error reading player position: {ex.Message}");
                return null;
            }
        }

        private IntPtr ReadPointer(IntPtr address)
        {
            byte[] buffer = new byte[8]; // 64-bit pointer
            if (ReadProcessMemory(processHandle, address, buffer, 8, out int bytesRead) && bytesRead == 8)
            {
                return new IntPtr(BitConverter.ToInt64(buffer, 0));
            }
            return IntPtr.Zero;
        }

        private uint ReadUInt32(IntPtr address)
        {
            byte[] buffer = new byte[4];
            if (ReadProcessMemory(processHandle, address, buffer, 4, out int bytesRead) && bytesRead == 4)
            {
                return BitConverter.ToUInt32(buffer, 0);
            }
            return 0;
        }

        public void ScanForAreaIds()
        {
            LogDebug("=== Area ID Scan ===");
            var mapId = GetCurrentMapId();
            LogDebug($"Current detected map ID: {mapId}");

            if (!IsDS3Running() || processHandle == IntPtr.Zero)
            {
                LogDebug("DS3 not running or process handle invalid");
                return;
            }

            try
            {
                // Scan around the WorldChrMan area for potential map IDs
                IntPtr worldChrManPtr = IntPtr.Add(baseAddress, 0x4768E78);
                IntPtr worldChrManAddress = ReadPointer(worldChrManPtr);

                if (worldChrManAddress != IntPtr.Zero)
                {
                    LogDebug($"Scanning around WorldChrMan: 0x{worldChrManAddress.ToInt64():X}");

                    for (int offset = 0; offset < 0x3000; offset += 4)
                    {
                        IntPtr scanAddress = IntPtr.Add(worldChrManAddress, offset);
                        uint value = ReadUInt32(scanAddress);

                        // Look for values that could be map IDs (reasonable range)
                        if (value > 0x1E000000 && value < 0x60000000)
                        {
                            string possibleMapId = $"m{value & 0xFF:D2}_{(value >> 8) & 0xFF:D2}_{(value >> 16) & 0xFF:D2}_{(value >> 24) & 0xFF:D2}";
                            LogDebug($"Offset +0x{offset:X}: 0x{value:X8} -> {possibleMapId}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Error during area ID scan: {ex.Message}");
            }
        }

        public void ScanForWorldChrMan()
        {
            LogDebug("=== WorldChrMan Scan ===");

            if (!IsDS3Running() || processHandle == IntPtr.Zero)
            {
                LogDebug("DS3 not running or process handle invalid");
                return;
            }

            try
            {
                // Test the known WorldChrMan address
                IntPtr worldChrManPtr = IntPtr.Add(baseAddress, 0x4768E78);
                LogDebug($"Testing WorldChrMan pointer at: 0x{worldChrManPtr.ToInt64():X}");

                IntPtr worldChrManAddress = ReadPointer(worldChrManPtr);
                LogDebug($"WorldChrMan address: 0x{worldChrManAddress.ToInt64():X}");

                if (worldChrManAddress != IntPtr.Zero)
                {
                    // Test various offsets to see what we can find
                    int[] testOffsets = { 0x80, 0x100, 0x200, 0x400, 0x800, 0x1000, 0x1FE0, 0x2000 };

                    foreach (int offset in testOffsets)
                    {
                        IntPtr testAddress = IntPtr.Add(worldChrManAddress, offset);
                        uint value = ReadUInt32(testAddress);
                        LogDebug($"WorldChrMan+0x{offset:X}: 0x{value:X8}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Error during WorldChrMan scan: {ex.Message}");
            }
        }

        private void LogDebug(string message)
        {
            try
            {
                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
                File.AppendAllText(logFileName, logMessage + Environment.NewLine);
            }
            catch
            {
                // Ignore logging errors
            }
        }

        public void Detach()
        {
            if (processHandle != IntPtr.Zero)
            {
                CloseHandle(processHandle);
                processHandle = IntPtr.Zero;
            }
            ds3Process = null;
        }
    }

    public class Vector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }
}
