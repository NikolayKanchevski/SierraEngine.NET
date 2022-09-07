using System.Diagnostics;
using SierraEngine.Core.Rendering.Vulkan;

namespace SierraEngine.Engine.Classes;

/// <summary>
/// Provides useful functionality for working with the file system. Extends the functionality of <see cref="File"/>.
/// </summary>
public static class Files
{
    /// <summary>
    /// Path on the file system pointing to where the build of the program is.
    /// </summary>
    public static readonly string OUTPUT_DIRECTORY = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule!.FileName)! + "/";
    
    /// <summary>
    /// Returns only the last directory/file from a given path.
    /// </summary>
    /// <param name="filePath">The file path to use.</param>
    /// <returns></returns>
    public static string TrimPath(in string filePath)
    {
        int index = filePath.LastIndexOf('/') + 1;
        return filePath[index..];
    }
    
    /// <summary>
    /// Searches for a given file name within a directory and all of its subdirectories.
    /// </summary>
    /// <param name="directory">Which directory to search.</param>
    /// <param name="fileName">What file to search for. Must contain an extension!</param>
    /// <returns></returns>
    public static string FindInSubdirectories(in string directory, string fileName)
    {
        string fileExtension = fileName[fileName.IndexOf(".", StringComparison.Ordinal)..];
        string? fileLocation = Directory.GetFiles(directory, $"*{ fileExtension }", SearchOption.AllDirectories).ToList().Find(o => o.Contains(fileName));

        if (fileLocation == null)
        {
            VulkanDebugger.ThrowWarning($"File [{ fileName }] not found anywhere in [{ directory }]");
            fileLocation = "";
        }
        
        return fileLocation.Replace("\\", "/");
    }

    /// <summary>
    /// Reads a file returns the read bytes.
    /// </summary>
    /// <param name="filePath">Path pointing to some file.</param>
    /// <returns></returns>
    public static byte[] GetBytes(string filePath)
    {
        return File.ReadAllBytes(filePath);
    }
}