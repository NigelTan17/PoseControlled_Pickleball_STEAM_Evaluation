// Assets/Editor/BuildDoctor.cs
// Unity 6–safe minimal build helper + quick Editor.log opener.

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildDoctor
{
    // ---------- Menus ----------
    [MenuItem("Tools/Build Doctor/Build Windows (Mono)")]
    public static void BuildWindowsMono()
    {
        if (!PrepareScenes()) return;
        PreparePlayerSettings(ScriptingImplementation.Mono2x);
        DoBuild("Builds/Windows_Mono/PickleBall.exe", BuildTarget.StandaloneWindows64);
    }

    [MenuItem("Tools/Build Doctor/Build Windows (IL2CPP)")]
    public static void BuildWindowsIL2CPP()
    {
        if (!PrepareScenes()) return;
        PreparePlayerSettings(ScriptingImplementation.IL2CPP);
        DoBuild("Builds/Windows_IL2CPP/PickleBall.exe", BuildTarget.StandaloneWindows64);
    }

    [MenuItem("Tools/Build Doctor/Open Editor Log")]
    public static void OpenEditorLog()
    {
        var path = GetEditorLogPath();
        if (File.Exists(path)) EditorUtility.OpenWithDefaultApp(path);
        else Debug.LogWarning($"Editor.log not found at: {path}");
    }

    // ---------- Core ----------
    private static bool PrepareScenes()
    {
        // Prefer fixed path, fall back to search by name.
        var scene = "Assets/PB/Scenes/PickleBallScene.unity";
        if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), scene)))
        {
            var hit = AssetDatabase.FindAssets("t:Scene PickleBallScene")
                                   .Select(AssetDatabase.GUIDToAssetPath)
                                   .FirstOrDefault(p => p.EndsWith("PickleBallScene.unity", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(hit)) scene = hit;
        }

        if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), scene)))
        {
            Debug.LogError("BuildDoctor: Could not locate PickleBallScene.unity. Check Assets/PB/Scenes.");
            return false;
        }

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scene, true) };
        return true;
    }

    private static void PreparePlayerSettings(ScriptingImplementation backend)
    {
        var group = BuildTargetGroup.Standalone;

        // Company/Product so persistentDataPath is nice and unique.
        if (string.IsNullOrEmpty(PlayerSettings.companyName)) PlayerSettings.companyName = "TZE";
        if (string.IsNullOrEmpty(PlayerSettings.productName)) PlayerSettings.productName = "PickleBall Game";

        // Conservative, build-friendly toggles
        PlayerSettings.gcIncremental = true;
        PlayerSettings.stripEngineCode = false;
        PlayerSettings.SetManagedStrippingLevel(group, ManagedStrippingLevel.Low);
        PlayerSettings.SetScriptingBackend(group, backend);

        // Do NOT poke API Compatibility in Unity 6 (enum changed).
        // We keep defaults which are fine for Mono and IL2CPP.
    }

    private static void DoBuild(string relativeExePath, BuildTarget target)
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("BuildDoctor: Exiting Play Mode before build.");
            EditorApplication.isPlaying = false;
        }

        var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        var exePath = Path.GetFullPath(Path.Combine(projectRoot, relativeExePath));
        Directory.CreateDirectory(Path.GetDirectoryName(exePath));

        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0)
        {
            Debug.LogError("BuildDoctor: No enabled scenes in EditorBuildSettings.");
            return;
        }

        var opts = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = exePath,
            target = target,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(opts);
        var summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"✅ Build succeeded ({summary.totalSize / (1024 * 1024)} MB)\n{exePath}");
            EditorUtility.RevealInFinder(exePath);
        }
        else
        {
            // Print last few build messages if Unity supplies them.
            try
            {
                var msgs = report.steps.SelectMany(s => s.messages).ToArray();
                if (msgs.Length > 0)
                {
                    int start = Mathf.Max(0, msgs.Length - 8);
                    for (int i = start; i < msgs.Length; i++)
                        Debug.LogError($"[{msgs[i].type}] {msgs[i].content}");
                }
            }
            catch { /* Unity API differences – ignore */ }

            Debug.LogError($"❌ Build failed: {summary.result}. Use Tools ▸ Build Doctor ▸ Open Editor Log to view the exact cause.");
            OpenEditorLog();
        }
    }

    private static string GetEditorLogPath()
    {
#if UNITY_EDITOR_WIN
        // %LOCALAPPDATA%\Unity\Editor\Editor.log
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(local, "Unity", "Editor", "Editor.log");
#elif UNITY_EDITOR_OSX
        // ~/Library/Logs/Unity/Editor.log
        var home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        return Path.Combine(home, "Library/Logs/Unity/Editor.log");
#else
        // Linux: ~/.config/unity3d/Editor.log
        var home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        return Path.Combine(home, ".config", "unity3d", "Editor.log");
#endif
    }
}
