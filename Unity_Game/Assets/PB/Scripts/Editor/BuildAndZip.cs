// Assets/PB/Scripts/Editor/BuildAndZip.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System;
using System.IO;
using System.Linq; // <-- needed for Where/Select

// NOTE: Do NOT `using System.IO.Compression;` to avoid name clash
// with UnityEngine.CompressionLevel. We'll fully-qualify it.

public static class BuildAndZip
{
    private const string LastZipKey = "PB.LastBuildZipPath";

    [MenuItem("Tools/Build/Build Windows + Zip", priority = 1)]
    public static void BuildWindowsAndZip()
    {
        // 1) Collect enabled scenes
        string[] enabledScenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (enabledScenes.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "No Scenes",
                "Add at least one enabled scene in File → Build Settings.",
                "OK");
            return;
        }

        // 2) Pick output folder
        string defaultRoot = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string targetRoot = EditorUtility.SaveFolderPanel("Choose build output folder", defaultRoot, "PB_Builds");
        if (string.IsNullOrEmpty(targetRoot)) return;

        // 3) Unique subfolder + exe path
        string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        string buildFolder = Path.Combine(targetRoot, $"PickleBall_Game-Win64-{stamp}");
        Directory.CreateDirectory(buildFolder);

        string exePath = Path.Combine(buildFolder, "PickleBall Game.exe");

        // 4) Build
        var opts = new BuildPlayerOptions
        {
            scenes = enabledScenes,
            target = BuildTarget.StandaloneWindows64,
            locationPathName = exePath,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result != BuildResult.Succeeded)
        {
            EditorUtility.DisplayDialog(
                "Build Failed",
                $"Result: {report.summary.result}\nCheck Console / Editor.log for details.",
                "OK");
            return;
        }

        // 5) Zip the build folder
        string zipPath = buildFolder + ".zip";
        try
        {
            if (File.Exists(zipPath)) File.Delete(zipPath);

            System.IO.Compression.ZipFile.CreateFromDirectory(
                buildFolder,
                zipPath,
                System.IO.Compression.CompressionLevel.Optimal,
                includeBaseDirectory: false
            );

            EditorPrefs.SetString(LastZipKey, zipPath);
            Debug.Log($"[BuildAndZip] Zipped build to: {zipPath}");
            EditorUtility.RevealInFinder(zipPath);
        }
        catch (Exception e)
        {
            Debug.LogError($"[BuildAndZip] Zip failed: {e}");
            EditorUtility.DisplayDialog("Zip failed", e.Message, "OK");
        }
    }

    [MenuItem("Tools/Build/Open Last Build Zip", priority = 20)]
    public static void OpenLastZip()
    {
        string zip = EditorPrefs.GetString(LastZipKey, "");
        if (string.IsNullOrEmpty(zip) || !File.Exists(zip))
        {
            EditorUtility.DisplayDialog("No zip found", "Run Build Windows + Zip first.", "OK");
            return;
        }
        EditorUtility.RevealInFinder(zip);
    }
}
#endif
