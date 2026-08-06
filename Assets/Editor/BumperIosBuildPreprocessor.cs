using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS;
using UnityEngine;

public sealed class BumperIosBuildPreprocessor : IPreprocessBuildWithReport
{
    const string IconPath = "Assets/Resources/UI/AppIcon.png";
    static readonly string[] RuntimeShaderNames =
    {
        "Standard",
        "Unlit/Color",
        "Sprites/Default",
        "Particles/Standard Unlit",
        "BumperZ/AbyssFog",
        "BumperZ/EnergyTrail",
        "BumperZ/InfernoFire",
        "BumperZ/OilPuddle",
        "BumperZ/OilTrack",
        "BumperZ/CarTrailFire"
    };

    public int callbackOrder => -1000;

    public void OnPreprocessBuild(BuildReport report)
    {
        EnsureRuntimeShadersAreIncluded();

        if (report.summary.platform != BuildTarget.iOS) return;

        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        if (!icon)
            throw new BuildFailedException($"Required iOS app icon was not found at {IconPath}.");
        if (icon.width != 1024 || icon.height != 1024)
            throw new BuildFailedException("The iOS App Store icon must be exactly 1024x1024 pixels.");

        PlatformIcon[] slots = PlayerSettings.GetPlatformIcons(
            NamedBuildTarget.iOS,
            iOSPlatformIconKind.Application);

        bool configuredMarketingIcon = false;
        foreach (PlatformIcon slot in slots)
        {
            // Unity resizes the source texture for every required device slot.
            slot.SetTexture(icon, 0);
            if (slot.width == 1024 && slot.height == 1024)
                configuredMarketingIcon = true;
        }

        if (!configuredMarketingIcon)
            throw new BuildFailedException(
                "Unity did not expose the required 1024x1024 iOS marketing icon slot.");

        PlayerSettings.SetPlatformIcons(
            NamedBuildTarget.iOS,
            iOSPlatformIconKind.Application,
            slots);

        PlatformIcon[] verifiedSlots = PlayerSettings.GetPlatformIcons(
            NamedBuildTarget.iOS,
            iOSPlatformIconKind.Application);
        bool verified = false;
        foreach (PlatformIcon slot in verifiedSlots)
        {
            if (slot.width == 1024 && slot.height == 1024 && slot.GetTexture(0) == icon)
            {
                verified = true;
                break;
            }
        }

        if (!verified)
            throw new BuildFailedException(
                "The required 1024x1024 iOS marketing icon was not persisted by Unity.");

        AssetDatabase.SaveAssets();
        Debug.Log("[BumperZ] Verified the 1024x1024 iOS App Store icon slot.");
    }

    static void EnsureRuntimeShadersAreIncluded()
    {
        UnityEngine.Object[] settingsAssets =
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
        if (settingsAssets.Length == 0)
            throw new BuildFailedException("Could not load ProjectSettings/GraphicsSettings.asset.");

        var settings = new SerializedObject(settingsAssets[0]);
        SerializedProperty included = settings.FindProperty("m_AlwaysIncludedShaders");
        if (included == null)
            throw new BuildFailedException("Could not access the Always Included Shaders list.");

        var existing = new HashSet<Shader>();
        for (int i = 0; i < included.arraySize; i++)
        {
            if (included.GetArrayElementAtIndex(i).objectReferenceValue is Shader shader)
                existing.Add(shader);
        }

        foreach (string shaderName in RuntimeShaderNames)
        {
            Shader shader = Shader.Find(shaderName);
            if (!shader)
                throw new BuildFailedException($"Required runtime shader was not found: {shaderName}");
            if (existing.Contains(shader)) continue;

            int index = included.arraySize;
            included.InsertArrayElementAtIndex(index);
            included.GetArrayElementAtIndex(index).objectReferenceValue = shader;
            existing.Add(shader);
        }

        settings.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        Debug.Log($"[BumperZ] Verified {RuntimeShaderNames.Length} runtime shaders for the player build.");
    }
}
