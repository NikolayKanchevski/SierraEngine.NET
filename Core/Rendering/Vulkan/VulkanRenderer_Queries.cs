using Evergine.Bindings.Vulkan;

namespace SierraEngine.Core.Rendering.Vulkan;

public unsafe partial class VulkanRenderer
{
    private VkQueryPool drawTimeQueryPool;
    private readonly float[] drawTimeQueryResults = new float[MAX_CONCURRENT_FRAMES];
    private float timestampPeriod;
    
    private void CreateQueryPool()
    {
        // Set up draw time query creation info
        VkQueryPoolCreateInfo queryPoolCreateInfo = new VkQueryPoolCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_QUERY_POOL_CREATE_INFO,
            queryCount = (uint) this.commandBuffers.Length * 2,
            queryType = VkQueryType.VK_QUERY_TYPE_TIMESTAMP
        };

        // Create the draw time query pool
        fixed (VkQueryPool* queryPoolPtr = &drawTimeQueryPool)
        {
            if (VulkanNative.vkCreateQueryPool(this.logicalDevice, &queryPoolCreateInfo, null, queryPoolPtr) != VkResult.VK_SUCCESS)
            {
                VulkanDebugger.ThrowError("Failed to create query pool");
            }
        }

        // Get the timestamp period of the physical device
        timestampPeriod = physicalDeviceProperties.limits.timestampPeriod;
        
        // Reset the query pool so it is ready to be used
        VulkanNative.vkResetQueryPool(this.logicalDevice, drawTimeQueryPool, 0, queryPoolCreateInfo.queryCount);
    }
}