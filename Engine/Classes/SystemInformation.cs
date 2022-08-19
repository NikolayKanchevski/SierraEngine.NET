using System.Text.RegularExpressions;

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
    /// The company (Manufacturer) of the current CPU used by the program.
    /// </summary>
    public static string cpuManufacturer { get; private set; } = null!;
    
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
            throw new NotImplementedException("System information is not available on Linux yet!");
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
            cpuManufacturer = "AMD";
            
            int startIdx = fullCpuModel.IndexOf("Ryzen", StringComparison.Ordinal);
            int endIdx = fullCpuModel.IndexOf("-Core", StringComparison.Ordinal);
        
            fullCpuModel = fullCpuModel[..(endIdx - 2)];
            cpuModelName = fullCpuModel[startIdx..];
        }
        // TODO: Add support for obsolete Intel chips (e.g. Pentium) 
        else if (fullCpuModel.Contains("Intel"))
        {
            cpuManufacturer = "Intel";

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
        // Get operating system and kernel names
        operatingSystemVersion = "MacOS " + CommandLine.ExecuteAndRead("awk '/SOFTWARE LICENSE AGREEMENT FOR macOS/' '/System/Library/CoreServices/Setup Assistant.app/Contents/Resources/en.lproj/OSXSoftwareLicense.rtf' | awk -F 'macOS ' '{print $NF}' | awk '{print substr($0, 0, length($0)-1)}'");
        
        // Retrieve the CPU model or chip name if the device uses Apple Silicon
        cpuModelName = CommandLine.ExecuteAndRead("sysctl -a | grep brand", true);
        cpuManufacturer = cpuModelName.Contains("Intel") ? "Intel" : "Apple";
        
        // Get the total RAM as an integer (in GB) and multiply it by 1024 to get exact RAM memory value 
        ramMemorySize = Int32.Parse(Regex.Replace(CommandLine.ExecuteAndRead("system_profiler SPHardwareDataType | grep \"Memory\"", true), @"[^\d]", "")) * 1024;
        
        // Get device model name and its name set by the user
        deviceModelName = CommandLine.ExecuteAndRead("sysctl -n hw.model");
        deviceName = CommandLine.ExecuteAndRead("system_profiler SPSoftwareDataType | grep \"Computer Name\"", true);

        // Get device configuration
        deviceConfiguration = deviceModelName.Contains("MacBook") ? DeviceConfiguration.Laptop : DeviceConfiguration.Desktop;
        
        // Toggle the successfully retrieved data bool
        dataRetrieved = true;
    }

    public new static string ToString()
    {
        return $"Operating System Version: { operatingSystemVersion }\n" +
               $"CPU Model: { cpuModelName }\n" +
               $"CPU Manufacturer: { cpuManufacturer }\n" +
               $"Total RAM: { ramMemorySize }\n" +
               $"Device Name: { deviceName }\n" +
               $"Device Model: { deviceModelName }\n" +
               $"Device Configuration: { deviceConfiguration }";
    }
}