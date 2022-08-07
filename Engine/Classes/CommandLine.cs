using System.Diagnostics;
using System.Text;
using SierraEngine.Core.Rendering.Vulkan;

namespace SierraEngine.Engine.Classes;
/// <summary>
/// Handles all the logic involving the Command Prompt (on Windows), and Terminal (macOS, Linux).
/// </summary>
public static class CommandLine
{
    private static readonly ProcessStartInfo cmd = new ProcessStartInfo()
    {
        RedirectStandardError = true,
        CreateNoWindow = true,
        UseShellExecute = false,
        RedirectStandardOutput = true
    };
    
    /// <summary>
    /// Executes a command in terminal and returns the output. If the command fails, an empty string is returned and a warning is produced. 
    /// </summary>
    /// <param name="command">The command to be executed.</param>
    /// <param name="trimBeforeColonSymbol">Whether to trim all characters before the last colon [:].</param>
    /// <returns>Command line output.</returns>
    public static string ExecuteAndRead(in string command, bool trimBeforeColonSymbol = false)
    {
        if (System.OperatingSystem.IsWindows())
        {
            cmd.FileName = "CMD.exe";
            cmd.Arguments = $"/C { command }";
        }
        else if (System.OperatingSystem.IsMacOS())
        {
            cmd.FileName = "sh";
            cmd.Arguments = $"-c \"{ command }\"";
        }

        try
        {
            var builder = new StringBuilder();
            using (Process process = Process.Start(cmd)!)
            {
                process.WaitForExit();
                builder.Append(process.StandardOutput.ReadToEnd());
            }

            if (trimBeforeColonSymbol)
            {
                string result = builder.ToString();
                int idx = result.IndexOf(":", StringComparison.Ordinal);
                idx = Mathematics.Clamp(idx, 0, int.MaxValue);
                return result[(idx + 1)..].Trim();
            }

            return builder.ToString().Trim();
        }
        catch (Exception exception)
        {
            VulkanDebugger.ThrowWarning($"Command line failed to execute { command }: [{ exception.Message }]");
        }

        return "";
    }
    
    /// <summary>
    /// Executes a command in terminal and returns the output trimmed so that it starts at the starting bounds and ends just after the end bounds. If the command fails, an empty string is returned and a warning is produced. 
    /// </summary>
    /// <param name="command">The command to be executed.</param>
    /// <param name="startingBounds">Starting point of the trimmed output.</param>
    /// <param name="endBounds">End point of the trimmed output.</param>
    /// <returns>Command line output.</returns>
    
    public static string ExecuteAndReadBetween(in string command, string startingBounds, string endBounds)
    {
        if (System.OperatingSystem.IsWindows())
        {
            cmd.FileName = "CMD.exe";
            cmd.Arguments = $"/C { command }";
        }
        else if (System.OperatingSystem.IsMacOS())
        {
            cmd.FileName = "sh";
            cmd.Arguments = $"-c \"{ command }\"";
        }

        try
        {
            var builder = new StringBuilder();
            using (Process process = Process.Start(cmd)!)
            {
                process.WaitForExit();
                builder.Append(process.StandardOutput.ReadToEnd());
            }
            
            string result = builder.ToString();
            
            int startIndex = result.IndexOf(startingBounds, StringComparison.Ordinal);
            startIndex = Mathematics.Clamp(startIndex, 0, result.Length);

            result = result[startIndex..];

            int endIndex = result.IndexOf(endBounds, StringComparison.Ordinal);
            endIndex = Mathematics.Clamp(endIndex, 0, result.Length);
            
            return result[..(endIndex + 1)].Trim();
        }
        catch (Exception exception)
        {
            VulkanDebugger.ThrowWarning($"Command line failed to execute [{ command }]: [{ exception.Message }]");
        }

        return "";
    }
}