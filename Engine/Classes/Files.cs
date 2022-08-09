using SierraEngine.Core.Rendering.Vulkan;

namespace SierraEngine.Engine.Classes;

public static class Files
{
    public static string FindInSubdirectories(in string directory, string fileName)
    {
        string fileExtension = fileName[fileName.IndexOf(".", StringComparison.Ordinal)..];
        string fileLocation = Directory.GetFiles(directory, $"*{ fileExtension }", SearchOption.AllDirectories).ToList().Find(o => o.Contains(fileName));

        if (fileLocation == null)
        {
            VulkanDebugger.ThrowWarning($"File [{ fileName }] not found anywhere in [{ directory }]");
            fileLocation = "";
        }
        
        return fileLocation.Replace("\\", "/");
    }
}