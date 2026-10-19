using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MemoryProjectBuilder
{
    [MenuItem("Lesson 3/Create Scenes and Build Development Player")]
    public static void Build()
    {
        Directory.CreateDirectory("Assets/Scenes");
        var payload = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Payload Scene Owner").AddComponent<PayloadScene>();
        EditorSceneManager.SaveScene(payload, "Assets/Scenes/Payload.unity");
        var main = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera").AddComponent<Camera>();
        camera.backgroundColor = new Color(0.035f, 0.08f, 0.11f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        var controller = new GameObject("Memory Lesson");
        controller.AddComponent<AllocationExercise>();
        controller.AddComponent<MemoryLesson>();
        EditorSceneManager.SaveScene(main, "Assets/Scenes/MemoryLab.unity");
        string[] scenes = { "Assets/Scenes/MemoryLab.unity", "Assets/Scenes/Payload.unity" };
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenes[0], true), new EditorBuildSettingsScene(scenes[1], true) };
        PlayerSettings.companyName = "Mire Education";
        PlayerSettings.productName = "Unity Memory Lab";
        PlayerSettings.defaultScreenWidth = 1100;
        PlayerSettings.defaultScreenHeight = 650;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.runInBackground = true;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        PlayerSettings.gcIncremental = true;
        QualitySettings.vSyncCount = 0;
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Build");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.GetFullPath("Build/UnityMemoryLab.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        });
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new BuildFailedException(report.summary.result.ToString());
        Debug.Log($"LESSON3 BUILD PASS bytes={report.summary.totalSize}");
    }
}
