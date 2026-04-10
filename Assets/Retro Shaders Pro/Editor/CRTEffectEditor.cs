using UnityEditor.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RetroShadersPro.URP
{
#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(CRTSettings))]
#else
    [VolumeComponentEditor(typeof(CRTSettings))]
#endif
    public class CRTEffectEditor : VolumeComponentEditor
    {
        private SerializedDataParameter showInSceneView;
        private readonly GUIContent showInSceneViewInfo = new("Show in Scene View",
            "Should the effect be visible in the Scene View?");

        private SerializedDataParameter enabled;
        private readonly GUIContent enabledInfo = new("Enabled",
            "Should the effect be rendered?");

        private SerializedDataParameter renderPassEvent;
        private readonly GUIContent renderPassEventInfo = new("Render Pass Event",
            "Choose where to insert this pass in URP's render loop.\n" +
            "\nURP's internal post processing includes effects like bloom and color-correction, which may impact the appearance of the CRT effect.\n" +
            "\nFor example, with the Before setting, high-intensity HDR colors will be impacted by Bloom.");

        private SerializedDataParameter tintColor;
        private readonly GUIContent tintColorInfo = new("Tint Color",
            "Tint applied to the entire screen.");

        private SerializedDataParameter useBarrelDistortion;
        private readonly GUIContent useBarrelDistortionInfo = new("Use Barrel Distortion",
            "Choose whether to bend the edges of the screen using barrel distortion.");

        private SerializedDataParameter distortionStrength;
        private readonly GUIContent distortionStrengthInfo = new("Distortion Strength",
            "Strength of the barrel distortion. Values above zero cause CRT screen-like distortion; values below zero bulge outwards.");

        private SerializedDataParameter distortionSmoothing;
        private readonly GUIContent distortionSmoothingInfo = new("Distortion Smoothing",
            "Amount of smoothing applied to edges of the distorted screen.");

        private SerializedDataParameter backgroundColor;
        private readonly GUIContent backgroundColorInfo = new("Background Color",
            "Color of the area outside of the barrel-distorted 'screen'.");

        private SerializedDataParameter scaleParameters;
        private readonly GUIContent scaleParametersInfo = new("Scale in Screen Space",
            "Enable if you want pixelation, scanline, and RGB effects to scale seamlessly with screen size.");

        private SerializedDataParameter verticalReferenceResolution;
        private readonly GUIContent verticalReferenceResolutionInfo = new("Reference Resolution (Vertical)",
            "Base vertical resolution to use as a reference point for scaling properties." +
            "\nIf the real screen resolution matches the reference, then no scaling is performed.");

        private SerializedDataParameter forcePointFiltering;
        private readonly GUIContent forcePointFilteringInfo = new("Force Point Filtering",
            "Should the effect use point filtering when rescaling?");

        private SerializedDataParameter rgbTex;
        private readonly GUIContent rgbTexInfo = new("RGB Subpixel Texture",
            "Small texture denoting the shape of the red, green, and blue subpixels." +
            "\nFor best results, make sure the texture dimensions are a multiple of the Pixel Size.");

        private SerializedDataParameter rgbStrength;
        private readonly GUIContent rgbStrengthInfo = new("RGB Subpixel Strength",
            "How strongly the screen colors get multiplied with the subpixel texture.");

        private SerializedDataParameter scanlineTex;
        private readonly GUIContent scanlineTexInfo = new("Scanline Texture",
            "Small texture denoting the scanline pattern which scrolls over the screen.");

        private SerializedDataParameter scanlineStrength;
        private readonly GUIContent scanlineStrengthInfo = new("Scanline Strength",
            "How strongly the scanline texture is overlaid onto the screen.");

        private SerializedDataParameter scanlineSize;
        private readonly GUIContent scanlineSizeInfo = new("Scanline/RGB Size",
            "The scanline and RGB textures cover this number of pixels." +
            "\nFor best results, this should be a multiple of the Pixel Size.");

        private SerializedDataParameter scrollSpeed;
        private readonly GUIContent scrollSpeedInfo = new("Scanline Scroll Speed",
            "How quickly the scanlines scroll vertically over the screen." +
            "\nUse negative values to scroll upwards instead.");

        private SerializedDataParameter pixelSize;
        private readonly GUIContent pixelSizeInfo = new("Pixel Size",
            "Size of each 'pixel' on the new image, after rescaling the source camera texture.");

        private SerializedDataParameter randomWear;
        private readonly GUIContent randomWearInfo = new("Random Wear",
            "How strongly each texture line is offset horizontally.");

        private SerializedDataParameter aberrationStrength;
        private readonly GUIContent aberrationStrengthInfo = new("Aberration Strength",
            "Amount of color channel separation at the screen edges.");

        private SerializedDataParameter useTracking;
        private readonly GUIContent useTrackingInfo = new("Use VHS Tracking",
            "Should the shader apply VHS tracking artifacts?");

        private SerializedDataParameter trackingTexture;
        private readonly GUIContent trackingTextureInfo = new("Tracking Texture",
            "A control texture for VHS tracking artifacts." +
            "\nThe red channel of the texture contains the strength of the UV offsets." +
            "\nThe green channel of the texture contains tracking line strength." +
            "\nStrength values are centered around 0.5 (gray), and get stronger the closer you get to 0 or 1.");

        private SerializedDataParameter trackingSize;
        private readonly GUIContent trackingSizeInfo = new("Tracking Size",
            "How many times the tracking texture is tiled on-screen.");

        private SerializedDataParameter trackingStrength;
        private readonly GUIContent trackingStrengthInfo = new("Tracking Strength",
            "How strongly the tracking texture offsets screen UVs.");

        private SerializedDataParameter trackingSpeed;
        private readonly GUIContent trackingSpeedInfo = new("Tracking Speed",
            "How quickly the tracking texture scrolls across the screen." +
            "\nUse negative values to scroll upwards instead.");

        private SerializedDataParameter trackingJitter;
        private readonly GUIContent trackingJitterInfo = new("Tracking Jitter",
            "How jittery the scrolling movement is.");

        private SerializedDataParameter trackingColorDamage;
        private readonly GUIContent trackingColorDamageInfo = new("Tracking Color Damage",
            "How strongly the chrominance of the image is distorted." +
            "\nThe distortion is applied in YIQ color space, to the I and Q channels (chrominance)." +
            "\nA value of 1 distorts colors back to the original chrominance.");

        private SerializedDataParameter trackingLinesThreshold;
        private readonly GUIContent trackingLinesThresholdInfo = new("Tracking Lines Threshold",
            "Higher threshold values mean fewer pixels are registered as tracking lines.");

        private SerializedDataParameter trackingLinesColor;
        private readonly GUIContent trackingLinesColorInfo = new("Tracking Lines Color",
            "Color of the tracking lines. The alpha component acts as a global multiplier on strength.");

        private SerializedDataParameter brightness;
        private readonly GUIContent brightnessInfo = new("Brightness",
            "Global brightness adjustment control. 1 represents no change." +
            "\nThis setting can be increased if other features like RGB subpixels and scanlines darken your image too much.");

        private SerializedDataParameter contrast;
        private readonly GUIContent contrastInfo = new("Contrast",
            "Global contrast modifier. 1 represents no change.");

        private SerializedDataParameter colorRampMode;
        private readonly GUIContent colorRampModeInfo = new("Color Ramp Mode",
            "Should the shader apply a color mapping based on a supplied ramp texture?" +
            "\n\nMost of the settings automatically retrieve a texture map from the Resources folder." +
            "\n\nIn Custom Luminance mode, the input color's luminance value is used as a coordinate along the x-axis to sample the color ramp." +
            "\n\nIn Custom RGB mode, the red, green, and blue input color values are used as three coordinates along the x-axis to sample the color ramp three times, for independent color ramps." +
            "\n\nIn Custom RGB+Intensity mode, the input color's luminance is used to sample the alpha channel of the color ramp. That value multiplies the output color." +
            "\n\nIn Custom RGB Sliders mode, you may use integer sliders to set the number of possible values for the red, green, and blue channels of the color.");

        private SerializedDataParameter colorRampTex;
        private readonly GUIContent colorRampTexInfo = new("Color Ramp Texture",
            "A texture which encodes the color mapping from a normal screen output to your chosen color palette.");

        private SerializedDataParameter redValues;
        private readonly GUIContent redValuesInfo = new("Red Values",
            "The number of possible values that the red channel can take on." +
            "\nA typical modern display probably uses 256 values.");

        private SerializedDataParameter greenValues;
        private readonly GUIContent greenValuesInfo = new("Green Values",
            "The number of possible values that the green channel can take on." +
            "\nA typical modern display probably uses 256 values.");

        private SerializedDataParameter blueValues;
        private readonly GUIContent blueValuesInfo = new("Blue Values",
            "The number of possible values that the blue channel can take on." +
            "\nA typical modern display probably uses 256 values.");

        private SerializedDataParameter useDithering;
        private readonly GUIContent useDitheringInfo = new("Use Dithering",
            "Should the shader dither colors that fall between color values?" + 
            "\nNote that individual materials may already be using dithering for their colors, which may look strange when combined with this setting.");

        private SerializedDataParameter enableInterlacing;
        private readonly GUIContent enableInterlacingInfo = new("Interlaced Rendering",
            "Should Unity render half of lines this frame, and the other half the next frame?");

        private static GUIStyle _boxStyle;
        private static GUIStyle BoxStyle
        {
            get
            {
                return _boxStyle ?? (_boxStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(10, 10, 5, 10)
                });
            }
        }

        private static GUIStyle _labelStyle;
        private static GUIStyle LabelStyle
        {
            get
            {
                return _labelStyle ?? (_labelStyle = new GUIStyle(EditorStyles.boldLabel));
            }
        }

        private const string gbTextureFilepath = "Textures/CR_Lum_GameBoy";
        private const string gbaTextureFilepath = "Textures/CR_RGB_GBA";
        private const string dsTextureFilepath = "Textures/CR_RGB_DS";
        private const string nesTextureFilepath = "Textures/CR_RGB_NES";
        private const string snesTextureFilepath = "Textures/CR_RGB_SNES";
        private const string msx2TextureFilepath = "Textures/CR_RGB_MSX2";
        private const string greyscaleTextureFilepath = "Textures/CR_Lum_Greyscale";
        private const string amstradTextureFilepath = "Textures/CR_RGB_Amstrad";
        private const string teletextTextureFilepath = "Textures/CR_RGB_Teletext";
        private const string zxSpectrumTextureFilepath = "Textures/CR_RGBI_ZXSpectrum";
        private const string masterSystemTextureFilepath = "Textures/CR_RGB_MasterSystem";
        private const string genesisTextureFilepath = "Textures/CR_RGB_Genesis";
        private const string gameGearTextureFilepath = "Textures/CR_RGB_GameGear";
        private const string ibmPS2TextureFilepath = "Textures/CR_RGB_XGA";

        public override void OnEnable()
        {
            var o = new PropertyFetcher<CRTSettings>(serializedObject);
            showInSceneView = Unpack(o.Find(x => x.showInSceneView));
            enabled = Unpack(o.Find(x => x.enabled));
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            tintColor = Unpack(o.Find(x => x.tintColor));
            useBarrelDistortion = Unpack(o.Find(x => x.useBarrelDistortion));
            distortionStrength = Unpack(o.Find(x => x.distortionStrength));
            backgroundColor = Unpack(o.Find(x => x.backgroundColor));
            scaleParameters = Unpack(o.Find(x => x.scaleParameters));
            verticalReferenceResolution = Unpack(o.Find(x => x.verticalReferenceResolution));
            forcePointFiltering = Unpack(o.Find(x => x.forcePointFiltering));
            rgbTex = Unpack(o.Find(x => x.rgbTex));
            rgbStrength = Unpack(o.Find(x => x.rgbStrength));
            scanlineTex = Unpack(o.Find(x => x.scanlineTex));
            scanlineStrength = Unpack(o.Find(x => x.scanlineStrength));
            scanlineSize = Unpack(o.Find(x => x.scanlineSize));
            scrollSpeed = Unpack(o.Find(x => x.scrollSpeed));
            pixelSize = Unpack(o.Find(x => x.pixelSize));
            randomWear = Unpack(o.Find(x => x.randomWear));
            aberrationStrength = Unpack(o.Find(x => x.aberrationStrength));
            distortionSmoothing = Unpack(o.Find(x => x.distortionSmoothing));
            useTracking = Unpack(o.Find(x => x.useTracking));
            trackingTexture = Unpack(o.Find(x => x.trackingTexture));
            trackingSize = Unpack(o.Find(x => x.trackingSize));
            trackingStrength = Unpack(o.Find(x => x.trackingStrength));
            trackingSpeed = Unpack(o.Find(x => x.trackingSpeed));
            trackingJitter = Unpack(o.Find(x => x.trackingJitter));
            trackingColorDamage = Unpack(o.Find(x => x.trackingColorDamage));
            trackingLinesThreshold = Unpack(o.Find(x => x.trackingLinesThreshold));
            trackingLinesColor = Unpack(o.Find(x => x.trackingLinesColor));
            brightness = Unpack(o.Find(x => x.brightness));
            contrast = Unpack(o.Find(x => x.contrast));
            colorRampMode = Unpack(o.Find(x => x.colorRampMode));
            colorRampTex = Unpack(o.Find(x => x.colorRampTex));
            redValues = Unpack(o.Find(x => x.redValues));
            greenValues = Unpack(o.Find(x => x.greenValues));
            blueValues = Unpack(o.Find(x => x.blueValues));
            useDithering = Unpack(o.Find(x => x.useDithering));
            enableInterlacing = Unpack(o.Find(x => x.enableInterlacing));
        }

        public override void OnInspectorGUI()
        {
            if (!RetroShaderUtility.CheckEffectEnabled<CRTEffect>())
            {
                EditorGUILayout.HelpBox("The CRT effect must be added to your renderer's Renderer Features list.", MessageType.Error);
                if (GUILayout.Button("Add CRT Renderer Feature"))
                {
                    RetroShaderUtility.AddEffectToPipelineAsset<CRTEffect>();
                }
            }

            GUILayout.Space(5);

            EditorGUILayout.BeginVertical(BoxStyle);
            EditorGUILayout.LabelField("Basic Settings", LabelStyle);

            PropertyField(showInSceneView, showInSceneViewInfo);
            PropertyField(enabled, enabledInfo);
            PropertyField(renderPassEvent, renderPassEventInfo);

            EditorGUILayout.EndVertical();

            GUILayout.Space(5);

            EditorGUILayout.BeginVertical(BoxStyle);
            EditorGUILayout.LabelField("Resolution & Fidelity", LabelStyle);

            PropertyField(pixelSize, pixelSizeInfo);
            PropertyField(scaleParameters, scaleParametersInfo);

            if (scaleParameters.value.boolValue)
            {
                EditorGUI.indentLevel++;
                PropertyField(verticalReferenceResolution, verticalReferenceResolutionInfo);
                EditorGUI.indentLevel--;
            }

            PropertyField(forcePointFiltering, forcePointFilteringInfo);
            PropertyField(enableInterlacing, enableInterlacingInfo);

            EditorGUILayout.EndVertical();

            GUILayout.Space(5);

            EditorGUILayout.BeginVertical(BoxStyle);
            EditorGUILayout.LabelField("Barrel Distortion", LabelStyle);

            PropertyField(useBarrelDistortion, useBarrelDistortionInfo);

            if(useBarrelDistortion.value.boolValue && useBarrelDistortion.overrideState.boolValue)
            {
                PropertyField(distortionStrength, distortionStrengthInfo);
                PropertyField(distortionSmoothing, distortionSmoothingInfo);
                PropertyField(backgroundColor, backgroundColorInfo);
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(5);

            EditorGUILayout.BeginVertical(BoxStyle);
            EditorGUILayout.LabelField("RGB Subpixels & Scanlines", LabelStyle);

            PropertyField(rgbTex, rgbTexInfo);
            PropertyField(rgbStrength, rgbStrengthInfo);

            PropertyField(scanlineTex, scanlineTexInfo);
            PropertyField(scanlineStrength, scanlineStrengthInfo);
            PropertyField(scanlineSize, scanlineSizeInfo);
            PropertyField(scrollSpeed, scrollSpeedInfo);

            EditorGUILayout.EndVertical();

            GUILayout.Space(5);

            EditorGUILayout.BeginVertical(BoxStyle);
            EditorGUILayout.LabelField("VHS Artifacts", LabelStyle);

            PropertyField(randomWear, randomWearInfo);
            PropertyField(aberrationStrength, aberrationStrengthInfo);
            PropertyField(useTracking, useTrackingInfo);

            if(useTracking.value.boolValue)
            {
                EditorGUI.indentLevel++;
                PropertyField(trackingTexture, trackingTextureInfo);
                PropertyField(trackingSize, trackingSizeInfo);
                PropertyField(trackingStrength, trackingStrengthInfo);
                PropertyField(trackingSpeed, trackingSpeedInfo);
                PropertyField(trackingJitter, trackingJitterInfo);
                PropertyField(trackingColorDamage, trackingColorDamageInfo);
                PropertyField(trackingLinesThreshold, trackingLinesThresholdInfo);
                PropertyField(trackingLinesColor, trackingLinesColorInfo);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(5);

            EditorGUILayout.BeginVertical(BoxStyle);
            EditorGUILayout.LabelField("Color Adjustments", LabelStyle);

            PropertyField(tintColor, tintColorInfo);
            PropertyField(brightness, brightnessInfo);
            PropertyField(contrast, contrastInfo);

            var crtTarget = target as CRTSettings;
            var rampMode = colorRampMode.value.GetEnumValue<ColorRampMode>();

            EditorGUI.BeginChangeCheck();
            PropertyField(colorRampMode, colorRampModeInfo);
            if(EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                rampMode = colorRampMode.value.GetEnumValue<ColorRampMode>();
                crtTarget.colorRampTex.overrideState = true;

                switch (rampMode)
                {
                    case ColorRampMode.GB:
                        crtTarget.colorRampTex.value = Resources.Load<Texture>(gbTextureFilepath);
                        break;
                    case ColorRampMode.GBA:
                        crtTarget.colorRampTex.value = Resources.Load<Texture>(gbaTextureFilepath);
                        break;
                    case ColorRampMode.DS:
                        crtTarget.colorRampTex.value = Resources.Load<Texture>(dsTextureFilepath);
                        break;
                    case ColorRampMode.NES:
                        crtTarget.colorRampTex.value = Resources.Load<Texture>(nesTextureFilepath);
                        break;
                    case ColorRampMode.SNES:
                        crtTarget.colorRampTex.value = Resources.Load<Texture>(snesTextureFilepath);
                        break;
                    case ColorRampMode.MSX2:
                        crtTarget.colorRampTex.value = Resources.Load<Texture>(msx2TextureFilepath);
                        break;
                    case ColorRampMode.IBMPS2:
                        crtTarget.colorRampTex.value = Resources.Load<Texture>(ibmPS2TextureFilepath);
                        break;
                    case ColorRampMode.Amstrad:
                        crtTarget.colorRampTex.value = Resources.Load<Texture>(amstradTextureFilepath);
                        break;
                    case ColorRampMode.Teletext:
                    case ColorRampMode.GameAndWatch:
                        crtTarget.colorRampTex.value = Resources.Load<Texture>(teletextTextureFilepath);
                        break;
                    case ColorRampMode.ZXSpectrum:
                        crtTarget.colorRampTex.value = Resources.Load<Texture>(zxSpectrumTextureFilepath);
                        break;
                    case ColorRampMode.MasterSystem:
                        crtTarget.colorRampTex.value = Resources.Load<Texture>(masterSystemTextureFilepath);
                        break;
                    case ColorRampMode.Genesis:
                        crtTarget.colorRampTex.value = Resources.Load<Texture>(genesisTextureFilepath);
                        break;
                    case ColorRampMode.GameGear:
                        crtTarget.colorRampTex.value = Resources.Load<Texture>(gameGearTextureFilepath);
                        break;
                    case ColorRampMode.Greyscale:
                        crtTarget.colorRampTex.value = Resources.Load<Texture>(greyscaleTextureFilepath);
                        break;
                    case ColorRampMode.None:
                        //crtTarget.colorRampTex.value = null;
                        crtTarget.colorRampTex.overrideState = false;
                        break;
                }
            }

            if(rampMode == ColorRampMode.CustomLuminance || rampMode == ColorRampMode.CustomRGB ||
                rampMode == ColorRampMode.CustomIntensity)
            {
                PropertyField(colorRampTex, colorRampTexInfo);
            }
            else if(rampMode == ColorRampMode.CustomSliders)
            {
                PropertyField(redValues, redValuesInfo);
                PropertyField(greenValues, greenValuesInfo);
                PropertyField(blueValues, blueValuesInfo);
                PropertyField(useDithering, useDitheringInfo);
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                PropertyField(colorRampTex, colorRampTexInfo);
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.EndVertical();
        }
    }
}
