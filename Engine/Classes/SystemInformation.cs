using System.Runtime.InteropServices;
using Microsoft.Win32;
using SierraEngine.Core.Rendering.Vulkan;

namespace SierraEngine.Engine.Classes;

public enum DeviceConfiguration { Desktop, Laptop, Unknown }

/// <summary>
/// Stores both all the software and hardware properties of the device running the program.
/// </summary>
public static class SystemInformation
{
    /// <summary>
    /// Shows whether the data has been successfully loaded. If false, rest of the fields would be empty strings.
    /// </summary>
    public static bool dataRetrieved { get; private set; }

    /// <summary>
    /// Contains the name of the current operating system run by the machine.
    /// </summary>
    public static string operatingSystemVersion { get; private set; } = null!;
    
    /// <summary>
    /// Model name of the current CPU used by the program.
    /// </summary>
    public static string cpuModelName { get; private set; } = null!;

    /// <summary>
    /// The model of the current in-use by the program graphics card (GPU). Always retrieved!
    /// </summary>
    public static string gpuModelName { get; private set; } = null!;
    
    /// <summary>
    /// Amount of RAM (Random Access Memory) memory present in MBs.
    /// </summary>
    public static int ramMemorySize { get; private set; }

    /// <summary>
    /// Name of the company which produced the device.
    /// </summary>
    public static string deviceManufacturer { get; private set; } = null!;
    
    /// <summary>
    /// Contains the type of system on which the program is run. Can be - Desktop, Laptop/Notebook, or Unknown.
    /// </summary>
    public static DeviceConfiguration deviceConfiguration { get; private set; }
    
    /// <summary>
    /// Production model name of the device. Only available for laptops or notebooks, otherwise is "Custom Desktop PC".
    /// </summary>
    public static string deviceModelName { get; private set; } = "Unknown";
    
    /// <summary>
    /// Name of the current user.
    /// </summary>
    public static string deviceUserName { get; private set; } = null!;
    

    public static void SetUsedGPUModel(in string usedGPUModelName)
    {
        gpuModelName = usedGPUModelName;
    }
    
    public static void PopulateSystemInfo()
    {
        if (OperatingSystem.IsWindows())
        {
            PopulateWindowsInfo();
        }
        else if (OperatingSystem.IsMacOS())
        {
            PopulateMacOSInfo();
        }
        else if (OperatingSystem.IsLinux())
        {
            VulkanDebugger.ThrowWarning("System information is not available on Linux yet!");
        }
        else
        {
            VulkanDebugger.ThrowWarning("System information is not supported on this operating system.");
        }
    }

    private static void PopulateWindowsInfo()
    {
        #pragma warning disable CA1416
        
        // Get operating system name
        operatingSystemVersion = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion", "ProductName", "")?.ToString()!.Trim()!;
        
        // Retrieve the CPU model name
        cpuModelName = Registry.GetValue("HKEY_LOCAL_MACHINE\\HARDWARE\\DESCRIPTION\\System\\CentralProcessor\\0", "ProcessorNameString", "")?.ToString()!.Trim()!;
        
        string? readType = Registry.GetValue("HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\pcmcia", "Type", "")?.ToString()?.Trim();
        if (readType == "1")
        {
            // Set configuration type and set a default model name for all undefined PCs
            deviceConfiguration = DeviceConfiguration.Desktop;
            deviceModelName = "Custom PC";
        }
        else if (readType == null) deviceConfiguration = DeviceConfiguration.Unknown;
        else
        {
            // Set configuration type and get the model of the laptop
            deviceConfiguration = DeviceConfiguration.Laptop;
            deviceModelName = Registry.GetValue("HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\SystemInformation", "SystemProductName", "")?.ToString()!.Trim()!;
        }
        
        // Set the manufacturer
        deviceManufacturer = Registry.GetValue("HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\SystemInformation", "SystemManufacturer", "")?.ToString()!.Trim()!;

        // Get device current user's name
        deviceUserName = Environment.UserName.Replace("-", " ");
        
        // Get the total RAM in kilobytes and convert it to MBs
        GetPhysicallyInstalledSystemMemory(out long kbRam);
        ramMemorySize = (int) (kbRam * 0.0009765625);
        
        // Toggle the successfully retrieved data bool
        dataRetrieved = true;

        #pragma warning restore CA1416
    }

    // Only on Windows!
    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetPhysicallyInstalledSystemMemory(out long totalMemoryInKilobytes);

    private static void PopulateMacOSInfo()
    {
        // Run the commands required
        string commandOutput = CommandLine.ExecuteAndRead("awk '/SOFTWARE LICENSE AGREEMENT FOR macOS/' '/System/Library/CoreServices/Setup Assistant.app/Contents/Resources/en.lproj/OSXSoftwareLicense.rtf' | awk -F 'macOS ' '{print $NF}' | awk '{print substr($0, 0, length($0)-1)}' && sysctl -n machdep.cpu.brand_string && sysctl -n hw.memsize && sysctl -n hw.model");
        string[] lines = commandOutput.Split('\n');
        
        // Get operating system name
        operatingSystemVersion = $"macOS { Environment.OSVersion.Version } { lines[0] }";

        // Retrieve the CPU model or chip name if the device uses Apple Silicon
        cpuModelName = lines[1];

        // Get the total RAM in bytes and convert it to MBs
        ramMemorySize = (int) (ulong.Parse(lines[2]) / 1048576);

        // Get device model name and its current user's name
        deviceModelName = lines[3];
        deviceUserName = Environment.MachineName.Replace("-", " ");

        // Set the manufacturer
        deviceManufacturer = "Apple";
        
        // Get device configuration
        deviceConfiguration = deviceModelName.Contains("Book") ? DeviceConfiguration.Laptop : DeviceConfiguration.Desktop;
        
        // Toggle the successfully retrieved data bool
        dataRetrieved = true;
    }
    
    public new static string ToString()
    {
        return $"Operating System Version: { operatingSystemVersion }\n" +
               $"CPU Model: { cpuModelName }\n" +
               $"GPU Model: { gpuModelName }\n" +
               $"Total RAM: { ramMemorySize }\n" +
               $"Device Manufacturer: { deviceManufacturer }\n" +
               $"Device Name: { deviceUserName }\n" +
               $"Device Model: { deviceModelName }\n" +
               $"Device Configuration: { deviceConfiguration }";
    }
}