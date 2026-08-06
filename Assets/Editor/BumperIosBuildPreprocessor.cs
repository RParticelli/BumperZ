using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
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

        // Unity 6000.5 does not expose the App Store marketing slot through
        // GetPlatformIcons on every build host. Set the legacy application
        // icons here; the Codemagic upload workflow injects and validates the
        // required 1024x1024 asset-catalog entry before re-signing the IPA.
#pragma warning disable CS0618
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.iOS, new[] { icon });
#pragma warning restore CS0618
        AssetDatabase.SaveAssets();
        Debug.Log("[BumperZ] Configured iOS application icons; marketing icon is finalized during upload.");
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
