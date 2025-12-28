using UnityEngine;
using System.IO;
using System.Text;
using System;

/// TXT exporter:
/// - OutputRoot = BuildFolder -> same folder as the game executable (or .app bundle parent on macOS).
/// - Auto-numbering so files are never overwritten.
/// - Includes names, difficulties, scores, mode, rules, servers, timestamp.
/// - If BuildFolder is not writable (Program Files, etc.), it falls back to persistentDataPath/Scores.
public class ScoreExporter : MonoBehaviour
{
    public enum OutputRoot
    {
        PersistentDataPath, // portable, safe default
        BuildFolder,        // next to exe / parent of .app
        CustomAbsolute      // your own absolute folder
    }

    [Header("Refs (auto-found if empty)")]
    public ModeRuntime mode;      // names, difficulties, mode
    public ScoreManager score;    // scores & rules

    [Header("Output")]
    public OutputRoot outputRoot = OutputRoot.PersistentDataPath;
    [Tooltip("Only used when OutputRoot = CustomAbsolute")]
    public string customAbsoluteFolder = "";
    [Tooltip("Optional subfolder under the chosen root. Leave empty to drop files directly in the root.")]
    public string subfolder = "Scores";
    [Tooltip("Base file name (auto-numbered if exists)")]
    public string fileName = "match_result.txt";

    [Header("Debug")]
    public bool logPathToConsole = true;

    // -------------- Public API --------------
    [ContextMenu("Export Now")]
    public void ExportNow()
    {
        if (!score) score = FindObjectOfType<ScoreManager>();
        if (!mode)  mode  = FindObjectOfType<ModeRuntime>();

        string leftName   = mode ? mode.leftPlayerName  : "Left";
        string rightName  = mode ? mode.rightPlayerName : "Right";
        string leftDiff   = mode ? mode.leftDifficulty.ToString()  : "-";
        string rightDiff  = mode ? mode.rightDifficulty.ToString() : "-";
        string modeName   = mode ? mode.currentMode.ToString()     : "Unknown";

        WriteResultFile(
            leftName, rightName, leftDiff, rightDiff,
            score ? score.LeftScore : 0,
            score ? score.RightScore : 0,
            modeName,
            score ? score.rallyScoring : true,
            score ? score.targetPoints : 11,
            score ? score.winByTwo : true,
            score ? score.startingServerSide : 1,
            score ? score.CurrentServerSide   : 1
        );
    }

    public void ExportFromParams(
        int leftScore, int rightScore,
        string leftName, string rightName,
        string leftDiff, string rightDiff,
        string modeName, bool rallyScoring, int targetPoints, bool winByTwo,
        int startingServer01, int finalServer01
    )
    {
        WriteResultFile(leftName, rightName, leftDiff, rightDiff, leftScore, rightScore,
                        modeName, rallyScoring, targetPoints, winByTwo,
                        startingServer01, finalServer01);
    }

    // -------------- Internals --------------
    void WriteResultFile(
        string leftName, string rightName, string leftDiff, string rightDiff,
        int leftScore, int rightScore, string modeName,
        bool rallyScoring, int targetPoints, bool winByTwo,
        int startingServer01, int finalServer01
    )
    {
        var now = DateTime.Now;

        var sb = new StringBuilder(256);
        sb.AppendLine("=== FYP Digital Pickleball Match Result ===");
        sb.AppendLine($"Date: {now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Mode: {modeName}  (Rally Scoring: {(rallyScoring ? "ON" : "OFF")})");
        sb.AppendLine($"Target Points: {targetPoints}  |  Win By Two: {(winByTwo ? "Yes" : "No")}");
        sb.AppendLine($"Starting Server: {(startingServer01 == 0 ? "Left" : "Right")}");
        sb.AppendLine($"Final Server:    {(finalServer01 == 0 ? "Left" : "Right")}");
        sb.AppendLine();
        sb.AppendLine($"Left : {leftName}   (Difficulty: {leftDiff})");
        sb.AppendLine($"Right: {rightName}  (Difficulty: {rightDiff})");
        sb.AppendLine($"Score: {leftName} {leftScore} : {rightScore} {rightName}");
        sb.AppendLine($"Winner: {(leftScore > rightScore ? leftName : rightName)}");
        sb.AppendLine();

        // Try target; if fails (not writable), fall back to persistent path.
        string folder = ResolveFolderWithFallback(out string rootUsed);
        SafeCreateFolder(folder);

        string fullPath = GetUniquePath(Path.Combine(folder, fileName));
        try
        {
            File.WriteAllText(fullPath, sb.ToString());
            if (logPathToConsole)
            {
                Debug.Log($"[ScoreExporter] Wrote result to: {fullPath} (root = {rootUsed})");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ScoreExporter] Failed to write file: {fullPath}\n{ex}");
        }
    }

    string ResolveFolderWithFallback(out string rootUsed)
    {
        string primary = ResolveFolderMain(out rootUsed);
        // Probe write permission
        try
        {
            string probeFolder = primary;
            SafeCreateFolder(probeFolder);
            string probePath = Path.Combine(probeFolder, "~write_probe.tmp");
            File.WriteAllText(probePath, "ok");
            File.Delete(probePath);
            return primary;
        }
        catch
        {
            // Fallback
            rootUsed += " -> FALLBACK:persistentDataPath";
            string fallback = Path.Combine(Application.persistentDataPath, string.IsNullOrWhiteSpace(subfolder) ? "" : subfolder);
            return fallback;
        }
    }

    string ResolveFolderMain(out string rootUsed)
    {
        rootUsed = outputRoot.ToString();
        string rootFolder;

        switch (outputRoot)
        {
            case OutputRoot.BuildFolder:
                rootFolder = GetBuildFolder(); // next to exe / parent of .app
                break;

            case OutputRoot.CustomAbsolute:
                rootFolder = string.IsNullOrWhiteSpace(customAbsoluteFolder)
                    ? Application.persistentDataPath
                    : customAbsoluteFolder;
                break;

            default: // PersistentDataPath
                rootFolder = Application.persistentDataPath;
                break;
        }

        return string.IsNullOrWhiteSpace(subfolder)
            ? rootFolder
            : Path.Combine(rootFolder, subfolder);
    }

    // -------------- Paths --------------
    static string GetBuildFolder()
    {
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX
        // In builds: dataPath ends with <GameName>_Data, so parent is the exe folder.
        // In Editor: dataPath is <project>/Assets; we go one up to project folder and put files in "BuildScores".
        string data = Application.dataPath;
#if UNITY_EDITOR
        // Avoid writing inside Assets; keep editor runs separate.
        return Path.GetFullPath(Path.Combine(data, "..", "BuildScores"));
#else
        return Path.GetDirectoryName(data);
#endif
#elif UNITY_STANDALONE_OSX
        // In mac builds: dataPath ends with MyGame.app/Contents
        // We want the .app's parent folder (where the .app sits).
        string contents = Application.dataPath;
#if UNITY_EDITOR
        // In Editor, similar to Win/Linux: keep in project/BuildScores
        return Path.GetFullPath(Path.Combine(contents, "..", "BuildScores"));
#else
        // .../MyGame.app/Contents -> parent = MyGame.app, parent-of-parent = folder containing .app
        var parent = Directory.GetParent(contents);            // .../MyGame.app
        var appParent = parent != null ? parent.Parent : null; // .../<folder containing .app>
        return appParent != null ? appParent.FullName : Directory.GetParent(contents).FullName;
#endif
#else
        // Other platforms: just use persistent
        return Application.persistentDataPath;
#endif
    }

    static void SafeCreateFolder(string path)
    {
        try { if (!Directory.Exists(path)) Directory.CreateDirectory(path); }
        catch (System.Exception ex) { Debug.LogError($"[ScoreExporter] Could not create directory: {path}\n{ex}"); }
    }

    static string GetUniquePath(string basePath)
    {
        string dir = Path.GetDirectoryName(basePath) ?? "";
        string name = Path.GetFileNameWithoutExtension(basePath);
        string ext = Path.GetExtension(basePath);
        string candidate = basePath;
        int i = 1;
        while (File.Exists(candidate))
        {
            candidate = Path.Combine(dir, $"{name} ({i}){ext}");
            i++;
        }
        return candidate;
    }
}
