﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Adapted from https://github.com/microsoft/vstest/blob/main/src/Microsoft.TestPlatform.CoreUtilities/Helpers/DotnetHostHelper.cs
using Microsoft.Win32;

namespace Microsoft.Testing.Platform.MSBuild.Tasks;

internal sealed class DotnetMuxerLocator
{
    // Mach-O magic numbers from https://en.wikipedia.org/wiki/Mach-O
    private const uint MachOMagic32BigEndian = 0xfeedface;    // 32-bit big-endian
    private const uint MachOMagic64BigEndian = 0xfeedfacf;    // 64-bit big-endian
    private const uint MachOMagic32LittleEndian = 0xcefaedfe; // 32-bit little-endian
    private const uint MachOMagic64LittleEndian = 0xcffaedfe; // 64-bit little-endian
    private const uint MachOMagicFatBigEndian = 0xcafebabe;   // Multi-architecture big-endian

    private readonly string _muxerName;
    private readonly Action<string> _resolutionLog;
    private readonly string _currentProcessFileName;

    internal DotnetMuxerLocator(Action<string> resolutionLog)
    {
        _muxerName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";
        using var currentProcess = Process.GetCurrentProcess();
        _currentProcessFileName = currentProcess.MainModule!.FileName!;
        _resolutionLog = resolutionLog;
    }

    private static PlatformArchitecture GetCurrentProcessArchitecture()
        => RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X86 => PlatformArchitecture.X86,
            Architecture.X64 => PlatformArchitecture.X64,
            Architecture.Arm => PlatformArchitecture.ARM,
            Architecture.Arm64 => PlatformArchitecture.ARM64,
            _ => throw new NotSupportedException(),
        };

    private static PlatformArchitecture GetOSArchitecture()
        => RuntimeInformation.OSArchitecture switch
        {
            Architecture.X86 => PlatformArchitecture.X86,
            Architecture.X64 => PlatformArchitecture.X64,
            Architecture.Arm => PlatformArchitecture.ARM,
            Architecture.Arm64 => PlatformArchitecture.ARM64,
            _ => throw new NotSupportedException(),
        };

    private static PlatformOperatingSystem GetOperatingSystem() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? PlatformOperatingSystem.Windows
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? PlatformOperatingSystem.OSX : PlatformOperatingSystem.Unix;

    public bool TryGetDotnetPathByArchitecture(
            PlatformArchitecture targetArchitecture,
            [NotNullWhen(true)] out string? muxerPath)
    {
        muxerPath = null;

        // If current process is the same as the target architecture we return the current process filename.
        if (GetCurrentProcessArchitecture() == targetArchitecture)
        {
            if (Path.GetFileName(_currentProcessFileName) == _muxerName)
            {
                muxerPath = _currentProcessFileName;
                _resolutionLog($"DotnetHostHelper.TryGetDotnetPathByArchitecture: Target architecture is the same as the current process architecture '{targetArchitecture}', and the current process is a muxer, using that: '{muxerPath}'");
                return true;
            }

            _resolutionLog($"DotnetHostHelper.TryGetDotnetPathByArchitecture: Target architecture is the same as the current process architecture '{targetArchitecture}', but the current process is not a muxer: '{_currentProcessFileName}'");
        }

        // We used similar approach as the runtime resolver.
        // https://github.com/dotnet/runtime/blob/main/src/native/corehost/fxr_resolver.cpp#L55
        bool isWinOs = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        _resolutionLog($"DotnetHostHelper.TryGetDotnetPathByArchitecture: Searching for muxer named '{_muxerName}'");

        // Try to search using env vars in the order
        // DOTNET_ROOT_{arch}
        // DOTNET_ROOT(x86) if X86 on Win (here we cannot check if current process is WOW64 because this is SDK process arch and not real host arch so it's irrelevant)
        //                  "DOTNET_ROOT(x86) is used instead when running a 32-bit executable on a 64-bit OS."
        // DOTNET_ROOT
        string? envKey = $"DOTNET_ROOT_{targetArchitecture.ToString().ToUpperInvariant()}";

        // Try on arch specific env var
        string? envVar = Environment.GetEnvironmentVariable(envKey);

        // Try on non virtualized x86 var(should happen only on non-x86 architecture)
        if ((envVar == null || !Directory.Exists(envVar)) &&
            targetArchitecture == PlatformArchitecture.X86 && isWinOs)
        {
            envKey = "DOTNET_ROOT(x86)";
            envVar = Environment.GetEnvironmentVariable(envKey);
        }

        // Try on default DOTNET_ROOT
        if (envVar == null || !Directory.Exists(envVar))
        {
            envKey = "DOTNET_ROOT";
            envVar = Environment.GetEnvironmentVariable(envKey);
        }

        if (envVar != null)
        {
            // If directory specified by env vars does not exists, it's like env var doesn't exists as well.
            if (!Directory.Exists(envVar))
            {
                _resolutionLog($"DotnetHostHelper.TryGetDotnetPathByArchitecture: Folder specified by env variable does not exist: '{envVar}={envKey}'");
            }
            else
            {
                muxerPath = Path.Combine(envVar, _muxerName);
                if (!File.Exists(muxerPath))
                {
                    // If environment variable was specified, and the directory it points at exists, but it does not contain a muxer, or the muxer is incompatible with the target architecture
                    // we stop the search to be compliant with the approach that apphost (compiled .NET executables) use to find the muxer.
                    _resolutionLog($"DotnetHostHelper.TryGetDotnetPathByArchitecture: Folder specified by env variable does not contain any muxer: '{envVar}={envKey}'");
                    muxerPath = null;
                    return false;
                }

                if (!IsValidArchitectureMuxer(targetArchitecture, muxerPath))
                {
                    _resolutionLog($"DotnetHostHelper: Invalid muxer resolved using env var key '{envKey}' in '{envVar}'");
                    muxerPath = null;
                    return false;
                }

                _resolutionLog($"DotnetHostHelper.TryGetDotnetPathByArchitecture: Muxer compatible with '{targetArchitecture}' resolved from env variable '{envKey}' in '{muxerPath}'");
                return true;
            }
        }

        _resolutionLog("DotnetHostHelper.TryGetDotnetPathByArchitecture: Muxer was not found using DOTNET_ROOT* env variables.");

        // Search in path
        muxerPath = GetMuxerFromPath();
        if (muxerPath is not null)
        {
            if (IsValidArchitectureMuxer(targetArchitecture, muxerPath))
            {
                _resolutionLog($"DotnetHostHelper.TryGetDotnetPathByArchitecture: Muxer compatible with '{targetArchitecture}' resolved from PATH: '{muxerPath}'");
                return true;
            }

            _resolutionLog($"DotnetHostHelper.TryGetDotnetPathByArchitecture: Muxer resolved using PATH is not compatible with the target architecture: '{muxerPath}'");
        }

        // Try to search for global registration
        muxerPath = isWinOs ? GetMuxerFromGlobalRegistrationWin(targetArchitecture) : GetMuxerFromGlobalRegistrationOnUnix(targetArchitecture);

        if (muxerPath != null)
        {
            if (!File.Exists(muxerPath))
            {
                // If muxer doesn't exists or it's wrong we stop the search
                _resolutionLog($"DotnetHostHelper.TryGetDotnetPathByArchitecture: Muxer file not found for global registration '{muxerPath}'");
                muxerPath = null;
                return false;
            }

            if (!IsValidArchitectureMuxer(targetArchitecture, muxerPath))
            {
                // If muxer is wrong we stop the search
                _resolutionLog($"DotnetHostHelper.TryGetDotnetPathByArchitecture: Muxer resolved using global registration is not compatible with the target architecture: '{muxerPath}'");
                muxerPath = null;
                return false;
            }

            _resolutionLog($"DotnetHostHelper.TryGetDotnetPathByArchitecture: Muxer compatible with '{targetArchitecture}' resolved from global registration: '{muxerPath}'");
            return true;
        }

        _resolutionLog("DotnetHostHelper.TryGetDotnetPathByArchitecture: Muxer not found using global registrations");

        // Try searching in default installation location if it exists
        if (isWinOs)
        {
            // If we're on x64/arm64 SDK and target is x86 we need to search on non virtualized windows folder
            if ((GetOSArchitecture() == PlatformArchitecture.X64 || GetOSArchitecture() == PlatformArchitecture.ARM64) &&
                 targetArchitecture == PlatformArchitecture.X86)
            {
                muxerPath = Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles(x86)")!, "dotnet", _muxerName);
            }
            else
            {
                // If we're on ARM and target is x64 we expect correct installation inside x64 folder
                muxerPath = GetOSArchitecture() == PlatformArchitecture.ARM64 && targetArchitecture == PlatformArchitecture.X64
                    ? Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles")!, "dotnet", "x64", _muxerName)
                    : Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles")!, "dotnet", _muxerName);
            }
        }
        else
        {
            if (GetOperatingSystem() == PlatformOperatingSystem.OSX)
            {
                // If we're on ARM and target is x64 we expect correct installation inside x64 folder
                muxerPath = GetOSArchitecture() == PlatformArchitecture.ARM64 && targetArchitecture == PlatformArchitecture.X64
                    ? Path.Combine("/usr/local/share/dotnet/x64", _muxerName)
                    : Path.Combine("/usr/local/share/dotnet", _muxerName);
            }
            else
            {
                muxerPath = Path.Combine("/usr/share/dotnet", _muxerName);
            }
        }

        if (!File.Exists(muxerPath))
        {
            // If muxer doesn't exists we stop the search
            _resolutionLog($"DotnetHostHelper.TryGetDotnetPathByArchitecture: Muxer was not found in default installation location: '{muxerPath}'");
            muxerPath = null;
            return false;
        }

        if (!IsValidArchitectureMuxer(targetArchitecture, muxerPath))
        {
            // If muxer is wrong we stop the search
            _resolutionLog($"DotnetHostHelper.TryGetDotnetPathByArchitecture: Muxer resolved in default installation path is not compatible with the target architecture: '{muxerPath}'");
            muxerPath = null;
            return false;
        }

        _resolutionLog($"DotnetHostHelper.TryGetDotnetPathByArchitecture: Muxer compatible with '{targetArchitecture}' resolved from default installation path: '{muxerPath}'");
        return true;
    }

    private string? GetMuxerFromPath()
    {
        string values = Environment.GetEnvironmentVariable("PATH")!;
        foreach (string? p in values.Split(Path.PathSeparator))
        {
            string fullPath = Path.Combine(p, _muxerName);
            if (File.Exists(fullPath))
            {
                _resolutionLog($"DotnetMuxerLocator.GetMuxerFromPath: Found {_muxerName} using PATH environment variable: '{fullPath}'");
                return fullPath;
            }
        }

        return null;
    }

    private string? GetMuxerFromGlobalRegistrationWin(PlatformArchitecture targetArchitecture)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new InvalidOperationException("The api GetMuxerFromGlobalRegistrationWin is not expected to be called in a non Windows OS");
        }

        // Installed version are always in 32-bit view of registry
        // https://github.com/dotnet/designs/blob/main/accepted/2020/install-locations.md#globally-registered-install-location-new
        // "Note that this registry key is "redirected" that means that 32-bit processes see different copy of the key than 64bit processes.
        // So it's important that both installers and the host access only the 32-bit view of the registry."
        using var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);

        using RegistryKey? dotnetInstalledVersion = hklm.OpenSubKey(@"SOFTWARE\dotnet\Setup\InstalledVersions");
        if (dotnetInstalledVersion == null)
        {
            _resolutionLog(@"DotnetHostHelper.GetMuxerFromGlobalRegistrationWin: Missing RegistryHive.LocalMachine for RegistryView.Registry32");
            return null;
        }

        using RegistryKey? nativeArch = dotnetInstalledVersion.OpenSubKey(targetArchitecture.ToString().ToLowerInvariant());
        string? installLocation = nativeArch?.GetValue("InstallLocation")?.ToString();
        if (installLocation == null)
        {
            _resolutionLog(@"DotnetHostHelper.GetMuxerFromGlobalRegistrationWin: Missing registry InstallLocation");
            return null;
        }

        string path = Path.Combine(installLocation.Trim(), _muxerName);
        _resolutionLog($@"DotnetHostHelper.GetMuxerFromGlobalRegistrationWin: Muxer resolved using win registry key 'SOFTWARE\dotnet\Setup\InstalledVersions\{targetArchitecture.ToString().ToLowerInvariant()}\InstallLocation' in '{path}'");
        return path;
    }

    private string? GetMuxerFromGlobalRegistrationOnUnix(PlatformArchitecture targetArchitecture)
    {
        string baseInstallLocation = "/etc/dotnet/";

        // We search for architecture specific installation
        string installLocation = $"{baseInstallLocation}install_location_{targetArchitecture.ToString().ToLowerInvariant()}";

        // We try to load archless install location file
        if (!File.Exists(installLocation))
        {
            installLocation = $"{baseInstallLocation}install_location";
        }

        if (!File.Exists(installLocation))
        {
            return null;
        }

        try
        {
            string content = File.ReadAllText(installLocation).Trim();
            _resolutionLog($"DotnetHostHelper: '{installLocation}' content '{content}'");
            string path = Path.Combine(content, _muxerName);
            _resolutionLog($"DotnetHostHelper: Muxer resolved using '{installLocation}' in '{path}'");
            return path;
        }
        catch (Exception ex)
        {
            _resolutionLog($"DotnetHostHelper.GetMuxerFromGlobalRegistrationOnUnix: Exception during '{installLocation}' muxer resolution.\n{ex}");
        }

        return null;
    }

    private static PlatformArchitecture? GetMuxerArchitectureByPEHeaderOnWin(string path, Action<string> resolutionLog)
    {
        // For details refer to below code available on MSDN.
        // https://code.msdn.microsoft.com/windowsapps/CSCheckExeType-aab06100#content
        PlatformArchitecture? archType = null;
        ushort machine = 0;

        uint peHeader;
        const int imageFileMachineAmd64 = 0x8664;
        const int imageFileMachineIa64 = 0x200;
        const int imageFileMachineI386 = 0x14c;
        const int imageFileMachineArm = 0x01c0; // ARM Little-Endian
        const int imageFileMachineThumb = 0x01c2; // ARM Thumb/Thumb-2 Little-Endian
        const int imageFileMachineArmnt = 0x01c4; // ARM Thumb-2 Little-Endian
        const int imageFileMachineArm64 = 0xAA64;

        // get the input stream
        using Stream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(fs);
        bool validImage = true;

        // PE Header starts @ 0x3C (60). Its a 4 byte header.
        fs.Position = 0x3C;
        peHeader = reader.ReadUInt32();

        // Check if the offset is invalid
        if (peHeader > fs.Length - 5)
        {
            resolutionLog("[GetMuxerArchitectureByPEHeaderOnWin]Invalid offset");
            validImage = false;
        }

        if (validImage)
        {
            // Moving to PE Header start location...
            fs.Position = peHeader;

            // peHeaderSignature
            // 0x00004550 is the letters "PE" followed by two terminating zeros.
            if (reader.ReadUInt32() != 0x00004550)
            {
                validImage = false;
                resolutionLog("[GetMuxerArchitectureByPEHeaderOnWin]Missing PE signature");
            }

            if (validImage)
            {
                // Read the image file header.
                machine = reader.ReadUInt16();
                reader.ReadUInt16(); // NumberOfSections
                reader.ReadUInt32(); // TimeDateStamp
                reader.ReadUInt32(); // PointerToSymbolTable
                reader.ReadUInt32(); // NumberOfSymbols
                reader.ReadUInt16(); // SizeOfOptionalHeader
                reader.ReadUInt16(); // Characteristics

                // magic number.32bit or 64bit assembly.
                ushort magic = reader.ReadUInt16();
                if (magic is not 0x010B and not 0x020B)
                {
                    validImage = false;
                }
            }

            if (validImage)
            {
                switch (machine)
                {
                    case imageFileMachineI386:
                        archType = PlatformArchitecture.X86;
                        break;

                    case imageFileMachineAmd64:
                    case imageFileMachineIa64:
                        archType = PlatformArchitecture.X64;
                        break;

                    case imageFileMachineArm64:
                        archType = PlatformArchitecture.ARM64;
                        break;

                    case imageFileMachineArm:
                    case imageFileMachineThumb:
                    case imageFileMachineArmnt:
                        archType = PlatformArchitecture.ARM;
                        break;
                }
            }
        }

        return archType ?? throw new InvalidOperationException("Invalid image");
    }

    // See https://opensource.apple.com/source/xnu/xnu-2050.18.24/EXTERNAL_HEADERS/mach-o/loader.h
    // https://opensource.apple.com/source/xnu/xnu-4570.41.2/osfmk/mach/machine.h.auto.html
    private PlatformArchitecture? GetMuxerArchitectureByMachoOnMac(string path)
    {
        try
        {
            using var headerReader = new FileStream(path, FileMode.Open, FileAccess.Read);
            byte[] magicBytes = new byte[4];
            byte[] cpuInfoBytes = new byte[4];
#pragma warning disable CA2022 // Avoid inexact read with 'Stream.Read'
            headerReader.Read(magicBytes, 0, magicBytes.Length);
            headerReader.Read(cpuInfoBytes, 0, cpuInfoBytes.Length);
#pragma warning restore CA2022 // Avoid inexact read with 'Stream.Read'

            uint magic = BitConverter.ToUInt32(magicBytes, 0);

            // Validate magic bytes to ensure this is a valid Mach-O binary
            if (magic is not (MachOMagic32BigEndian or MachOMagic64BigEndian or MachOMagic32LittleEndian or MachOMagic64LittleEndian or MachOMagicFatBigEndian))
            {
                _resolutionLog($"DotnetHostHelper.GetMuxerArchitectureByMachoOnMac: Invalid Mach-O magic bytes: 0x{magic:X8}");
                return null;
            }

            uint cpuInfo = BitConverter.ToUInt32(cpuInfoBytes, 0);
            PlatformArchitecture? architecture = (MacOsCpuType)cpuInfo switch
            {
                MacOsCpuType.Arm64Magic or MacOsCpuType.Arm64Cigam => PlatformArchitecture.ARM64,
                MacOsCpuType.X64Magic or MacOsCpuType.X64Cigam => PlatformArchitecture.X64,
                MacOsCpuType.X86Magic or MacOsCpuType.X86Cigam => PlatformArchitecture.X86,
                _ => null,
            };

            return architecture;
        }
        catch (Exception ex)
        {
            // In case of failure during header reading we must fallback to the next place(default installation path)
            _resolutionLog($"DotnetHostHelper.GetMuxerArchitectureByMachoOnMac: Failed to get architecture from Mach-O for '{path}'\n{ex}");
        }

        return null;
    }

    internal enum MacOsCpuType : uint
    {
        /// <summary>
        /// Arm64Magic.
        /// </summary>
        Arm64Magic = 0x0100000c,

        /// <summary>
        /// Arm64Cigam.
        /// </summary>
        Arm64Cigam = 0x0c000001,

        /// <summary>
        /// X64Magic.
        /// </summary>
        X64Magic = 0x01000007,

        /// <summary>
        /// X64Cigam.
        /// </summary>
        X64Cigam = 0x07000001,

        /// <summary>
        /// X86Magic.
        /// </summary>
        X86Magic = 0x00000007,

        /// <summary>
        /// X86Cigam.
        /// </summary>
        X86Cigam = 0x07000000,
    }

    private bool IsValidArchitectureMuxer(PlatformArchitecture targetArchitecture, string path)
    {
        PlatformArchitecture? muxerPlatform = null;
        if (GetOperatingSystem() == PlatformOperatingSystem.Windows)
        {
            muxerPlatform = GetMuxerArchitectureByPEHeaderOnWin(path, _resolutionLog);
        }
        else if (GetOperatingSystem() == PlatformOperatingSystem.OSX)
        {
            muxerPlatform = GetMuxerArchitectureByMachoOnMac(path);
        }

        if (targetArchitecture != muxerPlatform)
        {
            _resolutionLog($"DotnetHostHelper.IsValidArchitectureMuxer: Incompatible architecture muxer, target architecture '{targetArchitecture}', actual '{muxerPlatform}'");
            return false;
        }

        _resolutionLog($"DotnetHostHelper.IsValidArchitectureMuxer: Compatible architecture muxer, target architecture '{targetArchitecture}', actual '{muxerPlatform}'");
        return true;
    }

    public enum PlatformArchitecture
    {
        /// <summary>
        /// X86.
        /// </summary>
        X86,

        /// <summary>
        /// X64.
        /// </summary>
        X64,

        /// <summary>
        /// ARM.
        /// </summary>
        ARM,

        /// <summary>
        /// ARM64.
        /// </summary>
        ARM64,

        /// <summary>
        /// S390x.
        /// </summary>
        S390x,

        /// <summary>
        /// Ppc64le.
        /// </summary>
        Ppc64le,

        /// <summary>
        /// RiscV64.
        /// </summary>
        RiscV64,
    }

    private enum PlatformOperatingSystem
    {
        /// <summary>
        /// Windows.
        /// </summary>
        Windows,

        /// <summary>
        /// Unix.
        /// </summary>
        Unix,

        /// <summary>
        /// OSX.
        /// </summary>
        OSX,
    }
}
