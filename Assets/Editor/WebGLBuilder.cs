using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;

public class WebGLBuilder
{
    [MenuItem("Build/Build WebGL")]
    public static void BuildWebGL()
    {
        string[] scenes = {
            "Assets/Scenes/MainMenuScene.unity",
            "Assets/Scenes/RaceScene.unity",
            "Assets/Scenes/GarageScene.unity"
        };

        string buildPath = "Builds/WebGL";

        if (!Directory.Exists(buildPath))
            Directory.CreateDirectory(buildPath);

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled; // Uncompressed for seamless Vercel hosting

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            UnityEngine.Debug.Log("[NIGHTSHIFT] WebGL Build succeeded! Output at: " + buildPath);
        }
        else
        {
            UnityEngine.Debug.LogError("[NIGHTSHIFT] WebGL Build failed with result: " + summary.result);
        }
    }
}
