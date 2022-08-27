using SierraEngine.Core.Rendering.UI;

namespace SierraEngine.Core.Rendering.Vulkan;

public partial class VulkanRenderer
{
    public ImGuiController imGuiController = null!;
    
    private void CreateImGuiContext()
    {
        imGuiController = new ImGuiController(in window, ref this.renderPass,MAX_CONCURRENT_FRAMES, msaaSampleCount);
    }
}