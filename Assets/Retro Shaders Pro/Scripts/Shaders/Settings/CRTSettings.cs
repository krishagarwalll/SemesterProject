using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RetroShadersPro.URP
{
    [System.Serializable, VolumeComponentMenu("Retro Shaders Pro/CRT")]
    public class CRTSettings : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter showInSceneView = new BoolParameter(true);
        public BoolParameter enabled = new BoolParameter(false);
        public RenderPassEventParameter renderPassEvent = new RenderPassEventParameter(PostProcessRenderPassEvent.AfterURPPostProcessing);
        public BoolParameter useBarrelDistortion = new BoolParameter(false); 
        public ClampedFloatParameter distortionStrength = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);
        public ClampedFloatParameter distortionSmoothing = new ClampedFloatParameter(0.01f, 0.0f, 0.1f);

        public ColorRampModeParameter colorRampMode = new ColorRampModeParameter(ColorRampMode.None);
        public TextureParameter colorRampTex = new TextureParameter(null);
        public ClampedIntParameter redValues = new ClampedIntParameter(256, 2, 256);
        public ClampedIntParameter greenValues = new ClampedIntParameter(256, 2, 256);
        public ClampedIntParameter blueValues = new ClampedIntParameter(256, 2, 256);

        public BoolParameter useDithering = new BoolParameter(false);

        public ColorParameter tintColor = new ColorParameter(Color.white, true, false, true);
        public ColorParameter backgroundColor = new ColorParameter(Color.black);
        public BoolParameter scaleParameters = new BoolParameter(false);
        public IntParameter verticalReferenceResolution = new IntParameter(1080);
        public BoolParameter forcePointFiltering = new BoolParameter(false);
        public TextureParameter rgbTex = new TextureParameter(null);
        public ClampedFloatParameter rgbStrength = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);
        public TextureParameter scanlineTex = new TextureParameter(null);
        public ClampedFloatParameter scanlineStrength = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);
        public ClampedIntParameter scanlineSize = new ClampedIntParameter(8, 1, 64);
        public ClampedFloatParameter scrollSpeed = new ClampedFloatParameter(0.0f, -10.0f, 10.0f);
        public ClampedIntParameter pixelSize = new ClampedIntParameter(1, 1, 256);
        public ClampedFloatParameter randomWear = new ClampedFloatParameter(0.2f, 0.0f, 5.0f);
        public ClampedFloatParameter aberrationStrength = new ClampedFloatParameter(0.5f, 0.0f, 10.0f);

        public BoolParameter useTracking = new BoolParameter(false);
        public TextureParameter trackingTexture = new TextureParameter(null);
        public ClampedFloatParameter trackingSize = new ClampedFloatParameter(1.0f, 0.1f, 2.0f);
        public ClampedFloatParameter trackingStrength = new ClampedFloatParameter(0.1f, 0.0f, 50.0f);
        public ClampedFloatParameter trackingSpeed = new ClampedFloatParameter(0.1f, -2.5f, 2.5f);
        public ClampedFloatParameter trackingJitter = new ClampedFloatParameter(0.01f, 0.0f, 0.1f);
        public ClampedFloatParameter trackingColorDamage = new ClampedFloatParameter(0.05f, 0.0f, 1.0f);
        public ClampedFloatParameter trackingLinesThreshold = new ClampedFloatParameter(0.9f, 0.0f, 1.0f);
        public ColorParameter trackingLinesColor = new ColorParameter(new Color(1.0f, 1.0f, 1.0f, 0.5f));

        public ClampedFloatParameter brightness = new ClampedFloatParameter(1.0f, 0.0f, 5.0f);
        public ClampedFloatParameter contrast = new ClampedFloatParameter(1.0f, 0.0f, 5.0f);
        public BoolParameter enableInterlacing = new BoolParameter(false);

        public bool IsActive()
        {
            return enabled.value && active;
        }

        public bool IsTileCompatible()
        {
            return false;
        }
    }

    public enum PostProcessRenderPassEvent
    {
        [InspectorName("Before URP Post Processing")] BeforeURPPostProcessing,
        [InspectorName("After URP Post Processing")]  AfterURPPostProcessing
    }

    public enum ColorRampMode
    {
        None,
        [InspectorName("Game and Watch")] GameAndWatch,
        [InspectorName("Game Boy")] GB,
        [InspectorName("Game Boy Advance")] GBA,
        [InspectorName("Nintendo DS")] DS,
        Greyscale,
        NES,
        SNES,
        MSX2,
        [InspectorName("IBM PS-2")] IBMPS2,
        [InspectorName("Amstrad CPC")] Amstrad, 
        Teletext,
        [InspectorName("ZX Spectrum")] ZXSpectrum,
        [InspectorName("Sega Master System")] MasterSystem,
        [InspectorName("Sega Genesis")] Genesis,
        [InspectorName("Sega Game Gear")] GameGear,
        [InspectorName("Custom Luminance")] CustomLuminance,
        [InspectorName("Custom RGB")] CustomRGB,
        [InspectorName("Custom RGB+Intensity")] CustomIntensity,
        [InspectorName("Custom RGB Sliders")] CustomSliders
    }

    // Allow each volume settings object to track the render pass event.
    [Serializable]
    public sealed class RenderPassEventParameter : VolumeParameter<PostProcessRenderPassEvent>
    {
        public RenderPassEventParameter(PostProcessRenderPassEvent value, bool overrideState = false) : base(value, overrideState) { }
    }

    [Serializable]
    public sealed class ColorRampModeParameter : VolumeParameter<ColorRampMode>
    {
        public ColorRampModeParameter(ColorRampMode value, bool overrideState = false) : base(value, overrideState) { }
    }

    public static class ParameterTypeExtensions
    {
        public static RenderPassEvent Convert(this PostProcessRenderPassEvent renderPassEvent)
        {
            if (renderPassEvent == PostProcessRenderPassEvent.BeforeURPPostProcessing)
            {
                return RenderPassEvent.BeforeRenderingPostProcessing;
            }

            return RenderPassEvent.AfterRenderingPostProcessing;
        }
    }
}

