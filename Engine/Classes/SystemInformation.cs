using System.Text.RegularExpressions;

namespace SierraEngine.Engine.Classes;
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
    /// Contains both the name of the operating system and its version.
    /// </summary>
    public static string operatingSystemVersion { get; private set; } = null!;
    
    /// <summary>
    /// Holds the kernel version of the device.
    /// </summary>
    public static string kernelVersion { get; private set; } = null!;
    
    /// <summary>
    /// Model name of the current CPU used by the program.
    /// </summary>
    public static string cpuModelName { get; private set; } = null!;

    /// <summary>
    /// The model of the current in-use by the program graphics card (GPU). Always retrieved!
    /// </summary>
    public static string gpuModelName { get; private set; } = null!;

    /// <summary>
    /// Amount of video (GPU) memory present in MBs.
    /// </summary>
    public static int gpuMemorySize;
    
    /// <summary>
    /// Amount of RAM (Random Access Memory) memory present in MBs.
    /// </summary>
    public static int ramMemorySize;
    
    /// <summary>
    /// Production model name of the device.
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
            throw new NotImplementedException("System information is not available on Linux yet!");
        }
    }

    private static void PopulateWindowsInfo()
    {
        Console.WriteLine(CommandLine.ExecuteAndRead("wmic csproduct get name | find /v \"Name\""));

        dataRetrieved = true;
    }

    private static void PopulateMacOSInfo()
    { 
        operatingSystemVersion = CommandLine.ExecuteAndReadBetween("system_profiler SPSoftwareDataType | grep \"System Version\"", "mac", ")");
        kernelVersion = CommandLine.ExecuteAndRead("system_profiler SPSoftwareDataType | grep \"Kernel Version\"", true);
        cpuModelName = CommandLine.ExecuteAndRead("sysctl -a | grep brand", true);
        ramMemorySize = Int32.Parse(Regex.Replace(CommandLine.ExecuteAndRead("system_profiler SPHardwareDataType | grep \"Memory\"", true), @"[^\d]", "")) * 1024;
        deviceModelName = CommandLine.ExecuteAndRead("sysctl -n hw.model");
        deviceName = CommandLine.ExecuteAndRead("system_profiler SPSoftwareDataType | grep \"Computer Name\"", true);
        
        dataRetrieved = true;
    }

    public new static string ToString()
    {
        return $"Operating System Version: { operatingSystemVersion }\n" +
               $"Kernel Version: { kernelVersion }\n" +
               $"Device Name: { deviceName }\n" +
               $"Device Model: { deviceModelName }";
    }
}