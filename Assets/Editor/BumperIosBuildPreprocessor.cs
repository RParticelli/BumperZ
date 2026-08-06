using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS;
using UnityEngine;

public sealed class BumperIosBuildPreprocessor : IPreprocessBuildWithReport
{
    const string IconPath = "Assets/Resources/UI/AppIcon.png";

    public int callbackOrder => -1000;

    public void OnPreprocessBuild(BuildReport report)
    {
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
}
