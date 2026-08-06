using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildScript
{
    const string BundleIdentifier = "com.bumperz.game";

    [MenuItem("Build/BumperZ/iOS (Xcode)")]
    public static void BuildIos()
    {
        ApplySharedSettings(BuildTargetGroup.iOS);
        PlayerSettings.iOS.buildNumber = BuildNumber().ToString();

        string output = Environment.GetEnvironmentVariable("UNITY_IOS_DIR");
        if (string.IsNullOrWhiteSpace(output)) output = "build/ios";

        Build(output, BuildTarget.iOS, BuildOptions.None);
    }

    [MenuItem("Build/BumperZ/Android (AAB)")]
    public static void BuildAndroid()
    {
        ApplySharedSettings(BuildTargetGroup.Android);
        PlayerSettings.Android.bundleVersionCode = BuildNumber();
        EditorUserBuildSettings.buildAppBundle = true;
        ConfigureAndroidSigning();

        string output = Environment.GetEnvironmentVariable("UNITY_ANDROID_PATH");
        if (string.IsNullOrWhiteSpace(output)) output = "build/android/BumperZ.aab";

        Build(output, BuildTarget.Android, BuildOptions.None);
    }

    static void ApplySharedSettings(BuildTargetGroup group)
    {
        PlayerSettings.SetApplicationIdentifier(group, BundleIdentifier);
        PlayerSettings.bundleVersion = Environment.GetEnvironmentVariable("APP_VERSION") ?? "1.0.0";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
    }

    static int BuildNumber()
    {
        string raw = Environment.GetEnvironmentVariable("NEW_BUILD_NUMBER");
        if (string.IsNullOrWhiteSpace(raw)) raw = Environment.GetEnvironmentVariable("CM_BUILD_NUMBER");
        return int.TryParse(raw, out int value) && value > 0 ? value : 1;
    }

    static void ConfigureAndroidSigning()
    {
        string keystore = Environment.GetEnvironmentVariable("CM_KEYSTORE_PATH");
        if (string.IsNullOrWhiteSpace(keystore))
        {
            Debug.Log("Codemagic keystore not supplied; Unity will use debug signing where supported.");
            PlayerSettings.Android.useCustomKeystore = false;
            return;
        }

        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = keystore;
        PlayerSettings.Android.keystorePass = Environment.GetEnvironmentVariable("CM_KEYSTORE_PASSWORD") ?? "";
        PlayerSettings.Android.keyaliasName = Environment.GetEnvironmentVariable("CM_KEY_ALIAS") ?? "";
        PlayerSettings.Android.keyaliasPass = Environment.GetEnvironmentVariable("CM_KEY_PASSWORD") ?? "";
    }

    static void Build(string output, BuildTarget target, BuildOptions options)
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
            throw new InvalidOperationException("No enabled scenes were found in Build Settings.");

        string fullOutput = Path.GetFullPath(output);
        string directory = target == BuildTarget.iOS ? fullOutput : Path.GetDirectoryName(fullOutput);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        Debug.Log($"Building BumperZ for {target} at {fullOutput}");
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = fullOutput,
            target = target,
            options = options
        });

        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException(
                $"BumperZ {target} build failed: {report.summary.result}, {report.summary.totalErrors} errors.");

        Debug.Log($"BumperZ {target} build succeeded ({report.summary.totalSize} bytes). ");
    }
}
