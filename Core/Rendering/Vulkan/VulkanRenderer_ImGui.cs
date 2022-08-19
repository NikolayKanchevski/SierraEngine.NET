using System.Diagnostics;
using ImGuiNET;
using SierraEngine.Core.Rendering.ImGui;

namespace SierraEngine.Core.Rendering.Vulkan;

public partial class VulkanRenderer
{
    public ImGuiController imGuiController = null!;
    
    private void CreateImGuiContext()
    {
        imGuiController = new ImGuiController(in window, MAX_CONCURRENT_FRAMES, swapchainImageFormat, depthImageFormat, msaaSampleCount);
    }
}