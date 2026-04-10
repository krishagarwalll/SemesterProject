using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_6000_0_OR_NEWER
    using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace RetroShadersPro.URP
{
    public class CRTEffect : ScriptableRendererFeature
    {
        CRTRenderPass pass;

        public override void Create()
        {
            pass = new CRTRenderPass();
            name = "CRT";

            Shader.SetGlobalInteger("_RetroPixelSize", 1);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var settings = VolumeManager.instance.stack.GetComponent<CRTSettings>();

            if (settings != null && settings.IsActive())
            {
#if UNITY_6000_0_OR_NEWER
                pass.CreateInterlacingTexture();
#endif

                renderer.EnqueuePass(pass);
            }

            if (settings == null || !settings.showInSceneView.value || !settings.IsActive())
            {
                Shader.SetGlobalInteger("_RetroPixelSize", 1);
            }
        }

        protected override void Dispose(bool disposing)
        {
            pass.Dispose();
            base.Dispose(disposing);

            Shader.SetGlobalInteger("_RetroPixelSize", 1);
        }

        class CRTRenderPass : ScriptableRenderPass
        {
            private Material material;
            private RTHandle tempTexHandle;
            private RTHandle interlaceTexHandle;

            private int frameCounter = 0;

            public CRTRenderPass()
            {
                profilingSampler = new ProfilingSampler("CRT Effect");

#if UNITY_6000_0_OR_NEWER
                requiresIntermediateTexture = true;
#endif
            }

            private void CreateMaterial()
            {
                var shader = Shader.Find("Retro Shaders Pro/Post Processing/CRT");

                if (shader == null)
                {
                    Debug.LogError("Cannot find shader: \"Retro Shaders Pro/Post Processing/CRT\".");
                    return;
                }

                material = new Material(shader);
            }

            private static RenderTextureDescriptor GetCopyPassDescriptor(RenderTextureDescriptor descriptor)
            {
                descriptor.msaaSamples = 1;
                descriptor.depthBufferBits = (int)DepthBits.None;

                var settings = VolumeManager.instance.stack.GetComponent<CRTSettings>();

                float modifier = 1.0f;

                if (settings.scaleParameters.value)
                {
                    modifier = (float)settings.verticalReferenceResolution.value / descriptor.height;
                }

                int width = (int)Mathf.Max(4, descriptor.width / (settings.pixelSize.value / modifier));
                int height = (int)Mathf.Max(4, descriptor.height / (settings.pixelSize.value / modifier));

                descriptor.width = width;
                descriptor.height = height;

                return descriptor;
            }

            private static RenderTextureDescriptor GetInterlaceDescriptor(RenderTextureDescriptor descriptor)
            {
                descriptor.msaaSamples = 1;
                descriptor.depthBufferBits = (int)DepthBits.None;

                return descriptor;
            }

#if UNITY_6000_0_OR_NEWER
            // Need to create the interlacing texture somewhere outside of Configure for Render Graph.
            public void CreateInterlacingTexture()
            {
                var descriptor = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0);
                RenderingUtils.ReAllocateHandleIfNeeded(ref interlaceTexHandle, GetInterlaceDescriptor(descriptor), name: "_CRTInterlacingTexture");
            }
#endif

#if UNITY_6000_0_OR_NEWER
            [System.Obsolete]
#endif
            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                ResetTarget();

#if UNITY_6000_0_OR_NEWER
                RenderingUtils.ReAllocateHandleIfNeeded(ref tempTexHandle, GetCopyPassDescriptor(cameraTextureDescriptor), name: "_CRTColorCopy");
                RenderingUtils.ReAllocateHandleIfNeeded(ref interlaceTexHandle, GetInterlaceDescriptor(cameraTextureDescriptor), name: "_CRTInterlacingTexture");
#endif
                RenderingUtils.ReAllocateIfNeeded(ref tempTexHandle, GetCopyPassDescriptor(cameraTextureDescriptor), name: "_CRTColorCopy");
                RenderingUtils.ReAllocateIfNeeded(ref interlaceTexHandle, GetInterlaceDescriptor(cameraTextureDescriptor), name: "_CRTInterlacingTexture");

                base.Configure(cmd, cameraTextureDescriptor);
            }

            private void SetMaterialProperties(RTHandle interlacingTexture, int targetHeight, Material material)
            {
                var settings = VolumeManager.instance.stack.GetComponent<CRTSettings>();

                renderPassEvent = settings.renderPassEvent.value.Convert();

                var rgbTex = settings.rgbTex.value == null ? Texture2D.whiteTexture : settings.rgbTex.value;
                var scanlineTex = settings.scanlineTex.value == null ? Texture2D.whiteTexture : settings.scanlineTex.value;
                var trackingTex = settings.trackingTexture.value == null ? Texture2D.grayTexture : settings.trackingTexture.value;

                var distortionStrength = settings.useBarrelDistortion.value ? settings.distortionStrength.value : 0;
                var distortionSmoothing = settings.useBarrelDistortion.value ? settings.distortionSmoothing.value : 0;

                // Set CRT effect properties.
                material.SetColor("_TintColor", settings.tintColor.value);
                material.SetColor("_BackgroundColor", settings.backgroundColor.value);
                material.SetFloat("_DistortionStrength", distortionStrength);
                material.SetFloat("_DistortionSmoothing", distortionSmoothing);
                material.SetTexture("_RGBTex", rgbTex);
                material.SetFloat("_RGBStrength", settings.rgbStrength.value);
                material.SetTexture("_ScanlineTex", scanlineTex);
                material.SetFloat("_ScanlineStrength", settings.scanlineStrength.value);
                material.SetFloat("_ScrollSpeed", settings.scrollSpeed.value);
                material.SetFloat("_RandomWear", settings.randomWear.value);
                material.SetFloat("_AberrationStrength", settings.aberrationStrength.value);

                if (settings.useTracking.value)
                {
                    material.EnableKeyword("_TRACKING_ON");

                    material.SetTexture("_TrackingTex", trackingTex);
                    material.SetFloat("_TrackingSize", settings.trackingSize.value);
                    material.SetFloat("_TrackingStrength", settings.trackingStrength.value);
                    material.SetFloat("_TrackingSpeed", settings.trackingSpeed.value);
                    material.SetFloat("_TrackingJitter", settings.trackingJitter.value);
                    material.SetFloat("_TrackingColorDamage", settings.trackingColorDamage.value);
                    material.SetFloat("_TrackingLinesThreshold", settings.trackingLinesThreshold.value);
                    material.SetColor("_TrackingLinesColor", settings.trackingLinesColor.value);
                }
                else
                {
                    material.DisableKeyword("_TRACKING_ON");
                }

                material.SetFloat("_Brightness", settings.brightness.value);
                material.SetFloat("_Contrast", settings.contrast.value);
                material.SetInteger("_Interlacing", frameCounter++ % 2);
                material.SetTexture("_InputTexture", interlacingTexture);

                if (settings.scaleParameters.value)
                {
                    float modifier = (float)settings.verticalReferenceResolution.value / targetHeight;
                    material.SetInt("_Size", (int)(settings.scanlineSize.value / modifier));
                }
                else
                {
                    material.SetInt("_Size", settings.scanlineSize.value);
                }

                if (settings.enableInterlacing.value && frameCounter > 1)
                {
                    material.EnableKeyword("_INTERLACING_ON");
                }
                else
                {
                    material.DisableKeyword("_INTERLACING_ON");
                }

                if (settings.forcePointFiltering.value)
                {
                    material.EnableKeyword("_POINT_FILTERING_ON");
                }
                else
                {
                    material.DisableKeyword("_POINT_FILTERING_ON");
                }

                if (settings.aberrationStrength.value > 0.01f)
                {
                    material.EnableKeyword("_CHROMATIC_ABERRATION_ON");
                }
                else
                {
                    material.DisableKeyword("_CHROMATIC_ABERRATION_ON");
                }

                var rampMode = settings.colorRampMode.value;

                if (settings.colorRampTex.value == null && rampMode != ColorRampMode.CustomSliders)
                {
                    rampMode = ColorRampMode.None;
                }

                switch (rampMode)
                {
                    case ColorRampMode.GameAndWatch:
                    case ColorRampMode.GB:
                    case ColorRampMode.Greyscale:
                    case ColorRampMode.CustomLuminance:
                        material.SetTexture("_ColorRampTex", settings.colorRampTex.value);
                        material.EnableKeyword("_COLOR_RAMP_LUMINANCE");
                        material.DisableKeyword("_COLOR_RAMP_RGB");
                        material.DisableKeyword("_COLOR_RAMP_INTENSITY");
                        material.DisableKeyword("_COLOR_RAMP_SLIDERS");
                        material.DisableKeyword("_COLOR_RAMP_NONE");
                        break;
                    case ColorRampMode.GBA:
                    case ColorRampMode.DS:
                    case ColorRampMode.NES:
                    case ColorRampMode.SNES:
                    case ColorRampMode.MSX2:
                    case ColorRampMode.IBMPS2:
                    case ColorRampMode.Amstrad:
                    case ColorRampMode.Teletext:
                    case ColorRampMode.MasterSystem:
                    case ColorRampMode.Genesis:
                    case ColorRampMode.GameGear:
                    case ColorRampMode.CustomRGB:
                        material.SetTexture("_ColorRampTex", settings.colorRampTex.value);
                        material.DisableKeyword("_COLOR_RAMP_LUMINANCE");
                        material.EnableKeyword("_COLOR_RAMP_RGB");
                        material.DisableKeyword("_COLOR_RAMP_INTENSITY");
                        material.DisableKeyword("_COLOR_RAMP_SLIDERS");
                        material.DisableKeyword("_COLOR_RAMP_NONE");
                        break;
                    case ColorRampMode.ZXSpectrum:
                    case ColorRampMode.CustomIntensity:
                        material.SetTexture("_ColorRampTex", settings.colorRampTex.value);
                        material.DisableKeyword("_COLOR_RAMP_LUMINANCE");
                        material.DisableKeyword("_COLOR_RAMP_RGB");
                        material.EnableKeyword("_COLOR_RAMP_INTENSITY");
                        material.DisableKeyword("_COLOR_RAMP_SLIDERS");
                        material.DisableKeyword("_COLOR_RAMP_NONE");
                        break;
                    case ColorRampMode.CustomSliders:
                        material.SetInteger("_RedValues", settings.redValues.value);
                        material.SetInteger("_GreenValues", settings.greenValues.value);
                        material.SetInteger("_BlueValues", settings.blueValues.value);
                        material.DisableKeyword("_COLOR_RAMP_LUMINANCE");
                        material.DisableKeyword("_COLOR_RAMP_RGB");
                        material.DisableKeyword("_COLOR_RAMP_INTENSITY");
                        material.EnableKeyword("_COLOR_RAMP_SLIDERS");
                        material.DisableKeyword("_COLOR_RAMP_NONE");

                        if(settings.useDithering.value)
                        {
                            material.EnableKeyword("_DITHERING_ON");
                        }
                        else
                        {
                            material.DisableKeyword("_DITHERING_ON");
                        }
                        break;
                    case ColorRampMode.None:
                        material.DisableKeyword("_COLOR_RAMP_LUMINANCE");
                        material.DisableKeyword("_COLOR_RAMP_RGB");
                        material.DisableKeyword("_COLOR_RAMP_INTENSITY");
                        material.DisableKeyword("_COLOR_RAMP_SLIDERS");
                        material.EnableKeyword("_COLOR_RAMP_NONE");
                        break;
                }

                Shader.SetGlobalInteger("_RetroPixelSize", settings.pixelSize.value);
            }

#if UNITY_6000_0_OR_NEWER
            [System.Obsolete]
#endif
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (material == null)
                {
                    CreateMaterial();
                }

                var settings = VolumeManager.instance.stack.GetComponent<CRTSettings>();

                if (renderingData.cameraData.isSceneViewCamera && !settings.showInSceneView.value)
                {
                    return;
                }

                if (renderingData.cameraData.isPreviewCamera)
                {
                    return;
                }

                CommandBuffer cmd = CommandBufferPool.Get();

                RTHandle cameraTargetHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;

                SetMaterialProperties(interlaceTexHandle, cameraTargetHandle.rt.height, material);

                using (new ProfilingScope(cmd, profilingSampler))
                {
                    // Perform the Blit operations for the CRT effect.
                    using (new ProfilingScope(cmd, profilingSampler))
                    {
                        Blitter.BlitCameraTexture(cmd, cameraTargetHandle, tempTexHandle, bilinear: !settings.forcePointFiltering.value);
                        Blitter.BlitCameraTexture(cmd, tempTexHandle, cameraTargetHandle, material, 0);

                        if (settings.enableInterlacing.value)
                        {
                            Blitter.BlitCameraTexture(cmd, cameraTargetHandle, interlaceTexHandle, bilinear: !settings.forcePointFiltering.value);
                        }
                    }
                }

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }

            public void Dispose()
            {
                tempTexHandle?.Release();
            }

#if UNITY_6000_0_OR_NEWER

            private class CopyPassData
            {
                public TextureHandle inputTexture;
                public bool useBilinear;
            }

            private class MainPassData
            {
                public Material material;
                public TextureHandle inputTexture;
            }

            private class InterlacePassData
            {
                public TextureHandle inputTexture;
                public bool useBilinear;
            }

            private static void ExecuteCopyPass(RasterCommandBuffer cmd, RTHandle source, bool useBilinear)
            {
                Blitter.BlitTexture(cmd, source, new Vector4(1, 1, 0, 0), 0.0f, useBilinear);
            }

            private static void ExecuteMainPass(RasterCommandBuffer cmd, RTHandle source, Material material)
            {
                Blitter.BlitTexture(cmd, source, new Vector4(1, 1, 0, 0), material, 0);
            }

            private static void ExecuteInterlacePass(RasterCommandBuffer cmd, RTHandle source, bool useBilinear)
            {
                Blitter.BlitTexture(cmd, source, new Vector4(1, 1, 0, 0), 0.0f, useBilinear);
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (material == null)
                {
                    CreateMaterial();
                }

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                var settings = VolumeManager.instance.stack.GetComponent<CRTSettings>();

                if (cameraData.isSceneViewCamera && !settings.showInSceneView.value)
                {
                    return;
                }

                if (cameraData.isPreviewCamera)
                {
                    return;
                }

                SetMaterialProperties(interlaceTexHandle, cameraData.cameraTargetDescriptor.height, material);

                var colorCopyDescriptor = GetCopyPassDescriptor(cameraData.cameraTargetDescriptor);
                var interlacingDescriptor = GetInterlaceDescriptor(cameraData.cameraTargetDescriptor);

                // Perform the intermediate copy pass (source -> temp).
                TextureHandle copiedColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorCopyDescriptor, "_CRTColorCopy", false);
                TextureHandle interlacingTexture = TextureHandle.nullHandle;

                if (interlaceTexHandle != null)
                {
                    interlacingTexture = renderGraph.ImportTexture(interlaceTexHandle);
                }

                using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>("CRT_CopyColor", out var passData, profilingSampler))
                {
                    passData.inputTexture = resourceData.activeColorTexture;
                    passData.useBilinear = !settings.forcePointFiltering.value;

                    builder.UseTexture(resourceData.activeColorTexture, AccessFlags.Read);
                    builder.SetRenderAttachment(copiedColor, 0, AccessFlags.Write);
                    builder.SetRenderFunc(static (CopyPassData data, RasterGraphContext context) => ExecuteCopyPass(context.cmd, data.inputTexture, data.useBilinear));
                }

                // Perform main pass (temp -> source).
                using (var builder = renderGraph.AddRasterRenderPass<MainPassData>("CRT_MainPass", out var passData, profilingSampler))
                {
                    passData.material = material;
                    passData.inputTexture = copiedColor;

                    builder.UseTexture(copiedColor, AccessFlags.Read);
                    if(interlacingTexture.IsValid())
                    {
                        builder.UseTexture(interlacingTexture, AccessFlags.Read);
                    }
                    
                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                    builder.SetRenderFunc(static (MainPassData data, RasterGraphContext context) => ExecuteMainPass(context.cmd, data.inputTexture, data.material));
                }

                if(settings.enableInterlacing.value && interlacingTexture.IsValid())
                {
                    using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>("CRT_CopyInterlacingTexture", out var passData, profilingSampler))
                    {
                        passData.inputTexture = resourceData.activeColorTexture;
                        passData.useBilinear = !settings.forcePointFiltering.value;

                        builder.UseTexture(resourceData.activeColorTexture, AccessFlags.Read);
                        builder.SetRenderAttachment(interlacingTexture, 0, AccessFlags.Write);
                        builder.SetRenderFunc(static (CopyPassData data, RasterGraphContext context) => ExecuteCopyPass(context.cmd, data.inputTexture, data.useBilinear));
                    }
                }
            }
#endif
        }
    }
}

