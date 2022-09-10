using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Contains the type of system on which the program is run. Can be - Desktop, Laptop/Notebook, or Unknown.
    /// </summary>
    public static DeviceConfiguration deviceConfiguration { get; private set; }

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
    /// Production model name of the device. Only available for laptops or notebooks, otherwise is "Custom Desktop PC".
    /// </summary>
    public static string deviceModelName { get; private set; } = null!;
    
    /// <summary>
    /// Name of the device (is set by the user).
    /// </summary>
    public static string deviceName { get; private set; } = null!;
    

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
        // Get operating system version
        operatingSystemVersion = CommandLine.ExecuteAndReadBetween("wmic os get Caption", "Windows", null);

        // Retrieve the full CPU model
        string fullCpuModel = CommandLine.ExecuteAndReadBetween("wmic CPU get name", " ", null);
        
        // Format it based on its manufacturer
        if (fullCpuModel.Contains("AMD"))
        {
            int startIdx = fullCpuModel.IndexOf("Ryzen", StringComparison.Ordinal);
            int endIdx = fullCpuModel.IndexOf("-Core", StringComparison.Ordinal);
        
            fullCpuModel = fullCpuModel[..(endIdx - 2)];
            cpuModelName = fullCpuModel[startIdx..];
        }
        else if (fullCpuModel.Contains("Intel"))
        {

            int startIdx = fullCpuModel.IndexOf("(TM)", StringComparison.Ordinal);

            cpuModelName = fullCpuModel[(startIdx + 5)..];

            if (fullCpuModel.Contains("Core"))
            {
                cpuModelName = "Core " + cpuModelName;
            }
        }
        else
        {
            cpuModelName = fullCpuModel;
        }
        
        // Get RAM memory size
        ramMemorySize = (int) Math.Round(double.Parse(CommandLine.ExecuteAndReadBetween("wmic computersystem get totalphysicalmemory", " ", null)) / 1073741824) * 1024;

        // Check what kind of device the program is run on - (2, 3) = Desktop, (9) = Laptop, (10) = Note Book, Rest = Unknown
        int deviceChassisType = Int32.Parse(Regex.Replace(CommandLine.ExecuteAndRead("wmic systemenclosure get chassistypes"), @"[^\d]", ""));

        // Retrieve device type and its model based on the type
        if (deviceChassisType == 2 ||deviceChassisType == 3)
        {
            deviceConfiguration = DeviceConfiguration.Desktop;
            deviceModelName = "Custom Desktop PC";
        }
        else if (deviceChassisType == 9 ||deviceChassisType == 10)
        {
            deviceConfiguration = DeviceConfiguration.Laptop;
            deviceModelName = CommandLine.ExecuteAndRead("wmic computersystem get model");
        }
        else
        {
            deviceConfiguration = DeviceConfiguration.Unknown;
            deviceModelName = "Device";
        }

        // Get the device name
        deviceName = Environment.MachineName;
        
        dataRetrieved = true;
    }

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

        // Get device model name and its name set by the user
        deviceModelName = lines[3];
        deviceName = Environment.MachineName.Replace("-", " ");
        
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
               $"Device Name: { deviceName }\n" +
               $"Device Model: { deviceModelName }\n" +
               $"Device Configuration: { deviceConfiguration }";
    }
}