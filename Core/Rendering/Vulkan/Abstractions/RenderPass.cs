using System.Numerics;
using Evergine.Bindings.Vulkan;

namespace SierraEngine.Core.Rendering.Vulkan.Abstractions;

public unsafe class Subpass
{
    public class Builder
    {
        private readonly List<Tuple<VkAttachmentDescription, uint>> colorAttachments = new ();
        private readonly List<Tuple<VkAttachmentDescription, uint, VkImageLayout>> resolveAttachments = new ();
        
        private Tuple<VkAttachmentDescription, uint> depthAttachment = null!;
        private VkPipelineBindPoint pipelineBindPoint;

        public Builder SetPipelineBindPoint(in VkPipelineBindPoint bindPoint)
        {
            this.pipelineBindPoint = bindPoint;
            return this;
        }
        
        public Builder AddColorAttachment(
            in uint binding, in Image colorImage, in VkAttachmentLoadOp loadOp, in VkAttachmentStoreOp storeOp,
            in VkAttachmentLoadOp stencilLoadOp = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_DONT_CARE, VkAttachmentStoreOp stencilStoreOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_DONT_CARE)
        {
            this.colorAttachments.Add(new Tuple<VkAttachmentDescription, uint>(new ()
            {
                format = colorImage.format,
                samples = colorImage.sampling,
                loadOp = loadOp,
                storeOp = storeOp,
                stencilLoadOp = stencilLoadOp,
                stencilStoreOp = stencilStoreOp,
                initialLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
                finalLayout = VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL
            }, binding));
            
            return this;
        }

        public Builder AddResolveAttachment(
            in uint binding, in Image image, in VkImageLayout finalLayout, in VkImageLayout referenceLayout, in 
            VkAttachmentLoadOp loadOp, in VkAttachmentStoreOp storeOp, in VkSampleCountFlags sampling = VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT, 
            VkAttachmentLoadOp stencilLoadOp = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_DONT_CARE, VkAttachmentStoreOp stencilStoreOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_DONT_CARE)
        {
            this.resolveAttachments.Add(new (new ()
            {
                format = image.format,
                samples = sampling,
                loadOp = loadOp,
                storeOp = storeOp,
                stencilLoadOp = stencilLoadOp,
                stencilStoreOp = stencilStoreOp,
                initialLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
                finalLayout = finalLayout
            }, binding, referenceLayout));
            
            return this;
        }

        public Builder SetDepthAttachment(
            in uint binding, in Image depthImage, in VkAttachmentLoadOp loadOp, VkAttachmentStoreOp storeOp,
            in VkAttachmentLoadOp stencilLoadOp = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_DONT_CARE, VkAttachmentStoreOp stencilStoreOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_DONT_CARE)
        {
            this.depthAttachment = new Tuple<VkAttachmentDescription, uint>( new()
            {
                format = depthImage.format,
                samples = depthImage.sampling,
                loadOp = loadOp,
                storeOp = storeOp,
                stencilLoadOp = stencilLoadOp,
                stencilStoreOp = stencilStoreOp,
                initialLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
                finalLayout = VkImageLayout.VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL
            }, binding);

            return this;
        }

        public void Build(out Subpass subpass, in uint srcSubpass = ~0U, in uint dstSubpass = 0)
        {
            subpass = new Subpass(in pipelineBindPoint, in depthAttachment, in colorAttachments, resolveAttachments, srcSubpass, dstSubpass);
        }
    }

    private readonly bool hasDepthReference;
    private readonly VkPipelineBindPoint bindPoint;
    private readonly VkSubpassDependency subpassDependency;
    
    public readonly VkAttachmentDescription[] attachmentDescriptions;

    private readonly VkAttachmentReference depthReference;
    private readonly VkAttachmentReference[] colorAttachmentReferences;
    private readonly VkAttachmentReference[] resolveAttachmentReferences;

    private readonly int attachmentCount;
    
    private Subpass(
        in VkPipelineBindPoint givenBindPoint,
        in Tuple<VkAttachmentDescription, uint> givenDepthAttachment,
        in List<Tuple<VkAttachmentDescription, uint>> givenColorAttachments, 
        in List<Tuple<VkAttachmentDescription, uint, VkImageLayout>> givenResolveAttachments,
        in uint srcSubpass, in uint dstSubpass)
    {
        bindPoint = givenBindPoint;
        
        attachmentCount = givenColorAttachments.Count + givenResolveAttachments.Count + (givenDepthAttachment != null ? 1 : 0);
        attachmentDescriptions = new VkAttachmentDescription[attachmentCount];
        
        colorAttachmentReferences = new VkAttachmentReference[givenColorAttachments.Count];
        for (int i = 0; i < givenColorAttachments.Count; i++)
        {
            colorAttachmentReferences[i] = new VkAttachmentReference()
            {
                attachment = givenColorAttachments[i].Item2,
                layout = givenColorAttachments[i].Item1.finalLayout
            };

            attachmentDescriptions[givenColorAttachments[i].Item2] = givenColorAttachments[i].Item1;
        }
        
        resolveAttachmentReferences = new VkAttachmentReference[givenResolveAttachments.Count];
        for (int i = 0; i < givenResolveAttachments.Count; i++)
        {
            resolveAttachmentReferences[i] = new VkAttachmentReference()
            {
                attachment = givenResolveAttachments[i].Item2,
                layout = givenResolveAttachments[i].Item3
            };

            attachmentDescriptions[givenResolveAttachments[i].Item2] = givenResolveAttachments[i].Item1;
        }

        if (givenDepthAttachment != null)
        {
            attachmentDescriptions[givenDepthAttachment.Item2] = givenDepthAttachment.Item1;

            depthReference = new VkAttachmentReference()
            {
                attachment = givenDepthAttachment.Item2,
                layout = givenDepthAttachment.Item1.finalLayout
            };

            hasDepthReference = true;
        }

        this.subpassDependency = new VkSubpassDependency()
        {
            srcSubpass = srcSubpass,
            dstSubpass = dstSubpass,
            srcStageMask = VkPipelineStageFlags.VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT | VkPipelineStageFlags.VK_PIPELINE_STAGE_EARLY_FRAGMENT_TESTS_BIT,
            srcAccessMask = 0,
            dstStageMask = VkPipelineStageFlags.VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT | VkPipelineStageFlags.VK_PIPELINE_STAGE_EARLY_FRAGMENT_TESTS_BIT,
            dstAccessMask = VkAccessFlags.VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT | VkAccessFlags.VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_WRITE_BIT,
        };
    }

    public VkSubpassDescription GetVkSubpass()
    {
        var output = new VkSubpassDescription()
        {
            pipelineBindPoint = bindPoint,
            colorAttachmentCount = (uint) colorAttachmentReferences.Length
        };

        if (hasDepthReference)
        {
            fixed (VkAttachmentReference* depthReferencePtr = &depthReference)
            {
                output.pDepthStencilAttachment = depthReferencePtr;
            }
        }

        fixed (VkAttachmentReference* colorReferencesPtr = colorAttachmentReferences)
        {
            output.pColorAttachments = colorReferencesPtr;
        }

        fixed (VkAttachmentReference* resolveReferencesPtr = resolveAttachmentReferences)
        {
            output.pResolveAttachments = resolveReferencesPtr;
        }

        return output;
    }

    public VkSubpassDependency GetVkDependency()
    {
        return this.subpassDependency;
    }
}

public unsafe class RenderPass
{
    public class Builder
    {
        private Subpass subpass = null!;
        
        public Builder SetSubpass(Subpass givenSubpass)
        {
            this.subpass = givenSubpass;
            return this;
        }

        public void Build(out RenderPass renderPass)
        {
            renderPass = new RenderPass(subpass);
        }
    }

    private VkRenderPass vkRenderPass;
    private readonly VkClearValue[] clearValues = new VkClearValue[2] { new VkClearValue() with { color = new VkClearColorValue(0.0f, 0.0f, 0.0f) }, new VkClearValue() with { depthStencil = new VkClearDepthStencilValue() with { depth = 1.0f } }};

    private VkFramebuffer vkFramebuffer = VkFramebuffer.Null;

    private RenderPass(Subpass subpass)
    {
        VkSubpassDescription* subpassesPtr = stackalloc VkSubpassDescription[] { subpass.GetVkSubpass() };
        VkSubpassDependency* subpassDependenciesPtr = stackalloc VkSubpassDependency[] { subpass.GetVkDependency() };

        var a = subpass.GetVkSubpass();
        
        VkRenderPassCreateInfo renderPassCreateInfo = new VkRenderPassCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_RENDER_PASS_CREATE_INFO,
            attachmentCount = (uint) subpass.attachmentDescriptions.Length,
            subpassCount = 1,
            pSubpasses = subpassesPtr,
            dependencyCount = 1,
            pDependencies = subpassDependenciesPtr
        };

        fixed (VkAttachmentDescription* attachmentDescriptionsPtr = subpass.attachmentDescriptions)
        {
            renderPassCreateInfo.pAttachments = attachmentDescriptionsPtr;
        }

        fixed (VkRenderPass* renderPassPtr = &vkRenderPass)
        {
            VulkanDebugger.CheckResults(
                VulkanNative.vkCreateRenderPass(VulkanCore.logicalDevice, &renderPassCreateInfo, null, renderPassPtr),
                $"Could not create render pass with [{ renderPassCreateInfo.subpassCount }] subpasses and [{ renderPassCreateInfo.attachmentCount }] attachments"
            );
        }
    }

    public void SetFramebuffer(in Framebuffer framebuffer)
    {
        this.vkFramebuffer = framebuffer.GetVkFramebuffer();
    }

    public void SetBackgroundColor(in Vector3 givenColor)
    {
        clearValues[0].color = new VkClearColorValue(givenColor.X, givenColor.Y, givenColor.Z);
    }

    public void Begin(in VkCommandBuffer commandBuffer)
    {
        VkRenderPassBeginInfo beginInfo = new VkRenderPassBeginInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_RENDER_PASS_BEGIN_INFO,
            renderPass = this.vkRenderPass,
            framebuffer = vkFramebuffer
        };
        
        beginInfo.renderArea.offset = VkOffset2D.Zero;
        beginInfo.renderArea.extent = VulkanCore.swapchainExtent;
        beginInfo.clearValueCount = (uint) clearValues.Length;
        fixed (VkClearValue* clearValuesPtr = clearValues)
        {
            beginInfo.pClearValues = clearValuesPtr;
        }
        
        VulkanNative.vkCmdBeginRenderPass(commandBuffer, &beginInfo, VkSubpassContents.VK_SUBPASS_CONTENTS_INLINE);
    }

    public void End(in VkCommandBuffer commandBuffer)
    {
        VulkanNative.vkCmdEndRenderPass(commandBuffer);
    }

    public VkRenderPass GetVkRenderPass()
    {
        return this.vkRenderPass;
    }

    public void CleanUp()
    {
        VulkanNative.vkDestroyRenderPass(VulkanCore.logicalDevice, vkRenderPass, null);
    }
}