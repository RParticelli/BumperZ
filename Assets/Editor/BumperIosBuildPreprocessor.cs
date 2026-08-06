using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
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

#pragma warning disable CS0618
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.iOS, new[] { icon });
#pragma warning restore CS0618
        Debug.Log("[BumperZ] Configured 1024x1024 iOS App Store icon.");
    }
}
