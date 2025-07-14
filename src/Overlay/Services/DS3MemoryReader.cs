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
                // Calculate WorldChrMan base address (BaseB)
                // From cheat table: BaseB = DarkSoulsIII.exe+4768E78
                IntPtr worldChrManPtr = IntPtr.Add(baseAddress, 0x4768E78);

                // Read WorldChrMan pointer
                IntPtr worldChrManAddress = ReadPointer(worldChrManPtr);
                if (worldChrManAddress == IntPtr.Zero)
                {
                    LogDebug("WorldChrMan pointer is null for player position");
                    return null;
                }

                // Use the exact Cheat Engine path we discovered: BaseB + 80 → +0x18 → +0x28 → +0x80
                IntPtr firstOffsetAddress = IntPtr.Add(worldChrManAddress, 0x80);
                IntPtr firstPtr = ReadPointer(firstOffsetAddress);
                if (firstPtr == IntPtr.Zero)
                {
                    LogDebug("First pointer (BaseB+80) is null for player position");
                    return null;
                }

                IntPtr secondOffsetAddress = IntPtr.Add(firstPtr, 0x18);
                IntPtr secondPtr = ReadPointer(secondOffsetAddress);
                if (secondPtr == IntPtr.Zero)
                {
                    LogDebug("Second pointer (+0x18) is null for player position");
                    return null;
                }

                IntPtr thirdOffsetAddress = IntPtr.Add(secondPtr, 0x28);
                IntPtr thirdPtr = ReadPointer(thirdOffsetAddress);
                if (thirdPtr == IntPtr.Zero)
                {
                    LogDebug("Third pointer (+0x28) is null for player position");
                    return null;
                }

                // The position data is at the final pointer + 0x80
                IntPtr positionAddress = IntPtr.Add(thirdPtr, 0x80);
                LogDebug($"Player position address: 0x{positionAddress.ToInt64():X}");

                // Read 12 bytes (3 floats: X, Y, Z)
                byte[] buffer = new byte[12];
                if (ReadProcessMemory(processHandle, positionAddress, buffer, 12, out int bytesRead) && bytesRead == 12)
                {
                    float x = BitConverter.ToSingle(buffer, 0);
                    float y = BitConverter.ToSingle(buffer, 4);
                    float z = BitConverter.ToSingle(buffer, 8);

                    LogDebug($"Player position: X={x:F6}, Y={y:F6}, Z={z:F6}");
                    return new Vector3 { X = x, Y = y, Z = z };
                }
                else
                {
                    LogDebug($"Failed to read player position. Bytes read: {bytesRead}");
                    return null;
                }
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

        public void ScanForPlayerPosition()
        {
            LogDebug("=== Player Position Scan ===");

            if (!IsDS3Running() || processHandle == IntPtr.Zero)
            {
                LogDebug("DS3 not running or process handle invalid");
                return;
            }

            try
            {
                // Get WorldChrMan base address
                IntPtr worldChrManPtr = IntPtr.Add(baseAddress, 0x4768E78);
                IntPtr worldChrManAddress = ReadPointer(worldChrManPtr);

                if (worldChrManAddress != IntPtr.Zero)
                {
                    LogDebug($"WorldChrMan address: 0x{worldChrManAddress.ToInt64():X}");

                    // Check the first offset (+80)
                    IntPtr firstOffset = IntPtr.Add(worldChrManAddress, 0x80);
                    IntPtr firstOffsetPtr = ReadPointer(firstOffset);
                    LogDebug($"First offset (+80) pointer: 0x{firstOffsetPtr.ToInt64():X}");

                    if (firstOffsetPtr != IntPtr.Zero)
                    {
                        // Scan around the first offset pointer for potential next pointers
                        LogDebug("Scanning around first offset pointer:");
                        for (int offset = 0; offset < 0x100; offset += 8)
                        {
                            IntPtr scanAddress = IntPtr.Add(firstOffsetPtr, offset);
                            IntPtr value = ReadPointer(scanAddress);
                            if (value != IntPtr.Zero)
                            {
                                LogDebug($"  +0x{offset:X}: 0x{value.ToInt64():X}");
                            }
                        }

                        // Specifically check offset +28
                        IntPtr secondOffset = IntPtr.Add(firstOffsetPtr, 0x28);
                        IntPtr secondOffsetPtr = ReadPointer(secondOffset);
                        LogDebug($"Second offset (+28) pointer: 0x{secondOffsetPtr.ToInt64():X}");

                        if (secondOffsetPtr != IntPtr.Zero)
                        {
                            // Check offset +18 from second pointer
                            IntPtr thirdOffset = IntPtr.Add(secondOffsetPtr, 0x18);
                            IntPtr thirdOffsetPtr = ReadPointer(thirdOffset);
                            LogDebug($"Third offset (+18) pointer: 0x{thirdOffsetPtr.ToInt64():X}");

                            if (thirdOffsetPtr != IntPtr.Zero)
                            {
                                // Try to read position data at +80
                                IntPtr positionAddress = IntPtr.Add(thirdOffsetPtr, 0x80);
                                byte[] buffer = new byte[12];
                                if (ReadProcessMemory(processHandle, positionAddress, buffer, 12, out int bytesRead) && bytesRead == 12)
                                {
                                    float x = BitConverter.ToSingle(buffer, 0);
                                    float y = BitConverter.ToSingle(buffer, 4);
                                    float z = BitConverter.ToSingle(buffer, 8);
                                    LogDebug($"Position data at +80: X={x:F2}, Y={y:F2}, Z={z:F2}");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Error during player position scan: {ex.Message}");
            }
        }

        public void TestCheatEnginePosition()
        {
            LogDebug("=== Testing Cheat Engine Position Path ===");

            if (!IsDS3Running() || processHandle == IntPtr.Zero)
            {
                LogDebug("DS3 not running or process handle invalid");
                return;
            }

            try
            {
                // Get WorldChrMan base address (BaseB)
                IntPtr worldChrManPtr = IntPtr.Add(baseAddress, 0x4768E78);
                IntPtr worldChrManAddress = ReadPointer(worldChrManPtr);
                LogDebug($"BaseB (WorldChrMan): 0x{worldChrManAddress.ToInt64():X}");

                if (worldChrManAddress == IntPtr.Zero)
                {
                    LogDebug("WorldChrMan is null, cannot proceed");
                    return;
                }

                // Step 1: BaseB + 80
                IntPtr step1Address = IntPtr.Add(worldChrManAddress, 0x80);
                IntPtr step1Pointer = ReadPointer(step1Address);
                LogDebug($"Step 1 - BaseB+0x80: address=0x{step1Address.ToInt64():X}, pointer=0x{step1Pointer.ToInt64():X}");

                if (step1Pointer == IntPtr.Zero)
                {
                    LogDebug("Step 1 pointer is null, scanning around BaseB+80 area:");
                    for (int offset = 0x70; offset <= 0x90; offset += 8)
                    {
                        IntPtr scanAddr = IntPtr.Add(worldChrManAddress, offset);
                        IntPtr scanPtr = ReadPointer(scanAddr);
                        LogDebug($"  BaseB+0x{offset:X}: 0x{scanPtr.ToInt64():X}");
                    }
                    return;
                }

                // Step 2: Try different offsets since +0x28 is null but +0x20 has a pointer
                IntPtr step2Pointer = IntPtr.Zero;
                int[] possibleOffsets = { 0x28, 0x20, 0x30 };
                int usedOffset = 0;
                
                foreach (int offset in possibleOffsets)
                {
                    IntPtr step2Address = IntPtr.Add(step1Pointer, offset);
                    step2Pointer = ReadPointer(step2Address);
                    LogDebug($"Trying Step 2 - +0x{offset:X}: address=0x{step2Address.ToInt64():X}, pointer=0x{step2Pointer.ToInt64():X}");
                    
                    if (step2Pointer != IntPtr.Zero)
                    {
                        usedOffset = offset;
                        LogDebug($"Found valid pointer at offset +0x{offset:X}");
                        break;
                    }
                }

                if (step2Pointer == IntPtr.Zero)
                {
                    LogDebug("No valid pointer found at any expected offset");
                    return;
                }

                // Step 3: Try different offsets for the third step
                IntPtr step3Pointer = IntPtr.Zero;
                int[] possibleStep3Offsets = { 0x18, 0x10, 0x20, 0x08, 0x00 };
                int usedStep3Offset = 0;
                
                foreach (int offset in possibleStep3Offsets)
                {
                    IntPtr step3Address = IntPtr.Add(step2Pointer, offset);
                    step3Pointer = ReadPointer(step3Address);
                    LogDebug($"Trying Step 3 - +0x{offset:X}: address=0x{step3Address.ToInt64():X}, pointer=0x{step3Pointer.ToInt64():X}");
                    
                    if (step3Pointer != IntPtr.Zero)
                    {
                        usedStep3Offset = offset;
                        LogDebug($"Found valid pointer at step 3 offset +0x{offset:X}");
                        break;
                    }
                }

                if (step3Pointer == IntPtr.Zero)
                {
                    LogDebug("Step 3 pointer is null, scanning around the area:");
                    for (int offset = 0x00; offset <= 0x30; offset += 8)
                    {
                        IntPtr scanAddr = IntPtr.Add(step2Pointer, offset);
                        IntPtr scanPtr = ReadPointer(scanAddr);
                        LogDebug($"  +0x{offset:X}: 0x{scanPtr.ToInt64():X}");
                    }
                    return;
                }

                // Step 4: Try different final offsets to find the position data
                int[] possibleFinalOffsets = { 0x80, 0x70, 0x90, 0x60, 0x50 };
                bool positionFound = false;
                
                foreach (int finalOffset in possibleFinalOffsets)
                {
                    IntPtr positionAddress = IntPtr.Add(step3Pointer, finalOffset);
                    LogDebug($"Trying final offset +0x{finalOffset:X}: address=0x{positionAddress.ToInt64():X}");

                    // Try to read the position data
                    byte[] buffer = new byte[12];
                    if (ReadProcessMemory(processHandle, positionAddress, buffer, 12, out int bytesRead) && bytesRead == 12)
                    {
                        float x = BitConverter.ToSingle(buffer, 0);
                        float y = BitConverter.ToSingle(buffer, 4);
                        float z = BitConverter.ToSingle(buffer, 8);
                        
                        // Check if these look like reasonable position values (not NaN, not extremely large)
                        if (!float.IsNaN(x) && !float.IsNaN(y) && !float.IsNaN(z) && 
                            Math.Abs(x) < 10000 && Math.Abs(y) < 10000 && Math.Abs(z) < 10000)
                        {
                            LogDebug($"Position found at +0x{finalOffset:X}: X={x:F6}, Y={y:F6}, Z={z:F6}");
                            
                            // Also log the raw hex values to compare with Cheat Engine
                            string hexValues = BitConverter.ToString(buffer).Replace("-", " ");
                            LogDebug($"Raw hex: {hexValues}");
                            LogDebug($"Working offsets: Step2=+0x{usedOffset:X}, Step3=+0x{usedStep3Offset:X}, Final=+0x{finalOffset:X}");
                            positionFound = true;
                            break;
                        }
                        else
                        {
                            LogDebug($"Invalid position values at +0x{finalOffset:X}: X={x}, Y={y}, Z={z}");
                        }
                    }
                    else
                    {
                        LogDebug($"Failed to read position data at +0x{finalOffset:X}. Bytes read: {bytesRead}");
                    }
                }

                if (!positionFound)
                {
                    LogDebug("No valid position data found at any final offset");
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Error during Cheat Engine position test: {ex.Message}");
            }
        }

        public void TestDirectPositionAccess()
        {
            LogDebug("=== Testing Direct Position Access ===");

            if (!IsDS3Running() || processHandle == IntPtr.Zero)
            {
                LogDebug("DS3 not running or process handle invalid");
                return;
            }

            try
            {
                // Get WorldChrMan base address (BaseB)
                IntPtr worldChrManPtr = IntPtr.Add(baseAddress, 0x4768E78);
                IntPtr worldChrManAddress = ReadPointer(worldChrManPtr);
                LogDebug($"BaseB (WorldChrMan): 0x{worldChrManAddress.ToInt64():X}");

                if (worldChrManAddress == IntPtr.Zero)
                {
                    LogDebug("WorldChrMan is null, cannot proceed");
                    return;
                }

                // We know BaseB + 80 works for map ID, let's try similar approach for position
                // The map ID uses: BaseB + 80 -> [pointer] + 1FE0
                // The position might use: BaseB + 80 -> [pointer] + some other offset

                IntPtr firstStep = IntPtr.Add(worldChrManAddress, 0x80);
                IntPtr firstPtr = ReadPointer(firstStep);
                LogDebug($"First step (BaseB+80): 0x{firstPtr.ToInt64():X}");

                if (firstPtr != IntPtr.Zero)
                {
                    // Try different offsets from this base pointer to find position data
                    int[] possibleOffsets = { 
                        0x80, 0x70, 0x90, 0x60, 0x50, 0xA0, 0xB0, 0xC0, 0xD0, 0xE0, 0xF0,
                        0x100, 0x110, 0x120, 0x130, 0x140, 0x150, 0x160, 0x170, 0x180, 0x190, 0x1A0
                    };

                    foreach (int offset in possibleOffsets)
                    {
                        IntPtr testAddress = IntPtr.Add(firstPtr, offset);
                        byte[] buffer = new byte[12];
                        
                        if (ReadProcessMemory(processHandle, testAddress, buffer, 12, out int bytesRead) && bytesRead == 12)
                        {
                            float x = BitConverter.ToSingle(buffer, 0);
                            float y = BitConverter.ToSingle(buffer, 4);
                            float z = BitConverter.ToSingle(buffer, 8);
                            
                            // Check if these look like reasonable position values
                            if (!float.IsNaN(x) && !float.IsNaN(y) && !float.IsNaN(z) && 
                                Math.Abs(x) < 10000 && Math.Abs(y) < 10000 && Math.Abs(z) < 10000 &&
                                (Math.Abs(x) > 0.1 || Math.Abs(y) > 0.1 || Math.Abs(z) > 0.1)) // Not all zeros
                            {
                                LogDebug($"Potential position at +0x{offset:X}: X={x:F6}, Y={y:F6}, Z={z:F6}");
                                
                                string hexValues = BitConverter.ToString(buffer).Replace("-", " ");
                                LogDebug($"Raw hex at +0x{offset:X}: {hexValues}");
                            }
                        }
                    }
                }

                // Also try a completely different approach - scan for the known position values
                // We know from Cheat Engine the values should be around: X=373.3, Y=-252.2, Z=-765.1
                LogDebug("Scanning for known position values...");
                this.ScanForKnownPositionValues(worldChrManAddress);

            }
            catch (Exception ex)
            {
                LogDebug($"Error during direct position access test: {ex.Message}");
            }
        }

        private void ScanForKnownPositionValues(IntPtr baseAddress)
        {
            try
            {
                // Convert the known values to bytes for scanning
                float targetX = 373.3025818f;
                byte[] targetXBytes = BitConverter.GetBytes(targetX);
                uint targetXAsUint = BitConverter.ToUInt32(targetXBytes, 0);

                LogDebug($"Scanning for X value {targetX} (0x{targetXAsUint:X8})...");

                // Scan a reasonable range around the base address
                for (int offset = 0; offset < 0x5000; offset += 4)
                {
                    IntPtr scanAddress = IntPtr.Add(baseAddress, offset);
                    uint value = ReadUInt32(scanAddress);
                    
                    if (value == targetXAsUint)
                    {
                        LogDebug($"Found potential X value at offset +0x{offset:X}");
                        
                        // Try to read full position (12 bytes) from this location
                        byte[] buffer = new byte[12];
                        if (ReadProcessMemory(processHandle, scanAddress, buffer, 12, out int bytesRead) && bytesRead == 12)
                        {
                            float x = BitConverter.ToSingle(buffer, 0);
                            float y = BitConverter.ToSingle(buffer, 4);
                            float z = BitConverter.ToSingle(buffer, 8);
                            
                            LogDebug($"Full position at +0x{offset:X}: X={x:F6}, Y={y:F6}, Z={z:F6}");
                            
                            string hexValues = BitConverter.ToString(buffer).Replace("-", " ");
                            LogDebug($"Raw hex: {hexValues}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Error scanning for known position values: {ex.Message}");
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

        public void ScanForExactCheatEngineValues()
        {
            LogDebug("=== Scanning for Exact Cheat Engine Values ===");

            if (!IsDS3Running() || processHandle == IntPtr.Zero)
            {
                LogDebug("DS3 not running or process handle invalid");
                return;
            }

            try
            {
                // The exact values from Cheat Engine
                float targetX = 392.5711365f;
                float targetY = -258.1099854f;
                float targetZ = -775.3391113f;

                // Convert to uint for searching
                uint targetXAsUint = BitConverter.ToUInt32(BitConverter.GetBytes(targetX), 0);
                uint targetYAsUint = BitConverter.ToUInt32(BitConverter.GetBytes(targetY), 0);
                uint targetZAsUint = BitConverter.ToUInt32(BitConverter.GetBytes(targetZ), 0);

                LogDebug($"Looking for X={targetX} (0x{targetXAsUint:X8}), Y={targetY} (0x{targetYAsUint:X8}), Z={targetZ} (0x{targetZAsUint:X8})");

                // Get WorldChrMan base address
                IntPtr worldChrManPtr = IntPtr.Add(baseAddress, 0x4768E78);
                IntPtr worldChrManAddress = ReadPointer(worldChrManPtr);
                
                if (worldChrManAddress == IntPtr.Zero)
                {
                    LogDebug("WorldChrMan is null, cannot proceed");
                    return;
                }

                // Method 1: Search around the WorldChrMan base address directly
                LogDebug($"Method 1: Scanning around WorldChrMan base 0x{worldChrManAddress.ToInt64():X}...");
                ScanAreaForTargetValues(worldChrManAddress, 0x3000, "WorldChrMan base", targetXAsUint, targetYAsUint, targetZAsUint, targetX, targetY, targetZ);

                // Method 2: Search around the first pointer (BaseB + 80)
                IntPtr firstStep = IntPtr.Add(worldChrManAddress, 0x80);
                IntPtr firstPtr = ReadPointer(firstStep);
                
                if (firstPtr != IntPtr.Zero)
                {
                    LogDebug($"Method 2: Scanning around first pointer 0x{firstPtr.ToInt64():X}...");
                    ScanAreaForTargetValues(firstPtr, 0x2000, "first pointer", targetXAsUint, targetYAsUint, targetZAsUint, targetX, targetY, targetZ);
                }

                // Method 3: Try the original Cheat Engine path if any pointers exist
                LogDebug("Method 3: Attempting original Cheat Engine offset path...");
                TryOriginalCheatEnginePath(worldChrManAddress, targetX, targetY, targetZ);

                // Method 4: Check our current location (+0xE0) to see what it contains
                if (firstPtr != IntPtr.Zero)
                {
                    IntPtr currentPosAddress = IntPtr.Add(firstPtr, 0xE0);
                    byte[] currentBuffer = new byte[12];
                    if (ReadProcessMemory(processHandle, currentPosAddress, currentBuffer, 12, out int currentBytesRead) && currentBytesRead == 12)
                    {
                        float currentX = BitConverter.ToSingle(currentBuffer, 0);
                        float currentY = BitConverter.ToSingle(currentBuffer, 4);
                        float currentZ = BitConverter.ToSingle(currentBuffer, 8);
                        
                        LogDebug($"Current position at +0xE0: X={currentX:F6}, Y={currentY:F6}, Z={currentZ:F6}");
                        
                        string currentHex = BitConverter.ToString(currentBuffer).Replace("-", " ");
                        LogDebug($"Current raw hex: {currentHex}");
                        
                        // Calculate the difference
                        float deltaX = targetX - currentX;
                        float deltaY = targetY - currentY;
                        float deltaZ = targetZ - currentZ;
                        LogDebug($"Delta from target: X={deltaX:F6}, Y={deltaY:F6}, Z={deltaZ:F6}");
                    }
                }

            }
            catch (Exception ex)
            {
                LogDebug($"Error scanning for exact Cheat Engine values: {ex.Message}");
            }
        }

        private void ScanAreaForTargetValues(IntPtr baseAddr, int rangeSize, string areaName, uint targetXAsUint, uint targetYAsUint, uint targetZAsUint, float targetX, float targetY, float targetZ)
        {
            try
            {
                bool foundAny = false;
                for (int offset = 0; offset < rangeSize; offset += 4)
                {
                    IntPtr scanAddress = IntPtr.Add(baseAddr, offset);
                    uint value = ReadUInt32(scanAddress);
                    
                    // Check if we found any of the target values
                    if (value == targetXAsUint || value == targetYAsUint || value == targetZAsUint)
                    {
                        LogDebug($"Found potential target value in {areaName} at +0x{offset:X}: 0x{value:X8}");
                        foundAny = true;
                        
                        // Try to read 12 bytes from this location
                        byte[] buffer = new byte[12];
                        if (ReadProcessMemory(processHandle, scanAddress, buffer, 12, out int bytesRead) && bytesRead == 12)
                        {
                            float x = BitConverter.ToSingle(buffer, 0);
                            float y = BitConverter.ToSingle(buffer, 4);
                            float z = BitConverter.ToSingle(buffer, 8);
                            
                            LogDebug($"Position at +0x{offset:X}: X={x:F6}, Y={y:F6}, Z={z:F6}");
                            
                            // Check if this matches exactly
                            if (Math.Abs(x - targetX) < 0.001f && Math.Abs(y - targetY) < 0.001f && Math.Abs(z - targetZ) < 0.001f)
                            {
                                LogDebug($"*** EXACT MATCH FOUND IN {areaName.ToUpper()} AT +0x{offset:X} ***");
                                string hexValues = BitConverter.ToString(buffer).Replace("-", " ");
                                LogDebug($"Raw hex: {hexValues}");
                            }
                        }
                    }
                }
                
                if (!foundAny)
                {
                    LogDebug($"No target values found in {areaName} (scanned 0x{rangeSize:X} bytes)");
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Error scanning {areaName}: {ex.Message}");
            }
        }

        private void TryOriginalCheatEnginePath(IntPtr worldChrManAddress, float targetX, float targetY, float targetZ)
        {
            try
            {
                // Try: BaseB + 80 -> +28 -> +18 -> +80
                IntPtr step1Address = IntPtr.Add(worldChrManAddress, 0x80);
                IntPtr step1Pointer = ReadPointer(step1Address);
                LogDebug($"Original path Step 1 (BaseB+0x80): 0x{step1Pointer.ToInt64():X}");

                if (step1Pointer != IntPtr.Zero)
                {
                    // Try multiple offsets for step 2 since +0x28 was null before
                    int[] step2Offsets = { 0x28, 0x20, 0x30, 0x18, 0x38, 0x40 };
                    
                    foreach (int offset2 in step2Offsets)
                    {
                        IntPtr step2Address = IntPtr.Add(step1Pointer, offset2);
                        IntPtr step2Pointer = ReadPointer(step2Address);
                        
                        if (step2Pointer != IntPtr.Zero)
                        {
                            LogDebug($"Original path Step 2 (+0x{offset2:X}): 0x{step2Pointer.ToInt64():X}");
                            
                            // Try multiple offsets for step 3
                            int[] step3Offsets = { 0x18, 0x10, 0x20, 0x08, 0x00, 0x28, 0x30 };
                            
                            foreach (int offset3 in step3Offsets)
                            {
                                IntPtr step3Address = IntPtr.Add(step2Pointer, offset3);
                                IntPtr step3Pointer = ReadPointer(step3Address);
                                
                                if (step3Pointer != IntPtr.Zero)
                                {
                                    // Try final offsets for position data
                                    int[] finalOffsets = { 0x80, 0x70, 0x90, 0x60, 0x50, 0xA0, 0xB0, 0xC0 };
                                    
                                    foreach (int finalOffset in finalOffsets)
                                    {
                                        IntPtr posAddress = IntPtr.Add(step3Pointer, finalOffset);
                                        byte[] buffer = new byte[12];
                                        
                                        if (ReadProcessMemory(processHandle, posAddress, buffer, 12, out int bytesRead) && bytesRead == 12)
                                        {
                                            float x = BitConverter.ToSingle(buffer, 0);
                                            float y = BitConverter.ToSingle(buffer, 4);
                                            float z = BitConverter.ToSingle(buffer, 8);
                                            
                                            // Check if this matches the target
                                            if (Math.Abs(x - targetX) < 0.001f && Math.Abs(y - targetY) < 0.001f && Math.Abs(z - targetZ) < 0.001f)
                                            {
                                                LogDebug($"*** EXACT MATCH via original path: +0x{offset2:X} -> +0x{offset3:X} -> +0x{finalOffset:X} ***");
                                                LogDebug($"Position: X={x:F6}, Y={y:F6}, Z={z:F6}");
                                                string hexValues = BitConverter.ToString(buffer).Replace("-", " ");
                                                LogDebug($"Raw hex: {hexValues}");
                                                return;
                                            }
                                            else if (!float.IsNaN(x) && !float.IsNaN(y) && !float.IsNaN(z) && 
                                                    Math.Abs(x) < 10000 && Math.Abs(y) < 10000 && Math.Abs(z) < 10000)
                                            {
                                                // Log reasonable values that don't match exactly
                                                float deltaX = Math.Abs(x - targetX);
                                                float deltaY = Math.Abs(y - targetY);
                                                float deltaZ = Math.Abs(z - targetZ);
                                                
                                                if (deltaX < 100 && deltaY < 100 && deltaZ < 100) // Close but not exact
                                                {
                                                    LogDebug($"Close match via +0x{offset2:X} -> +0x{offset3:X} -> +0x{finalOffset:X}: X={x:F6}, Y={y:F6}, Z={z:F6}");
                                                    LogDebug($"Delta: X={deltaX:F6}, Y={deltaY:F6}, Z={deltaZ:F6}");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Error trying original Cheat Engine path: {ex.Message}");
            }
        }
    }

    public class Vector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }
}
