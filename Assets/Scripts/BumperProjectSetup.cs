#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BumperProjectSetup
{
    public static void Configure()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Main";
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true) };
        PlayerSettings.companyName = "BumperZ Studio";
        PlayerSettings.productName = "BumperZ";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.bumperz.game");
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.bumperz.game");
        AssetDatabase.SaveAssets();
        Debug.Log("[BumperZ] Projeto configurado.");
    }
}
#endif
