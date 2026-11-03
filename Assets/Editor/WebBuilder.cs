using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Editor-only helper. Menu "Documents Por Favor > Build for Web".
// Makes a WebGL build in <project>/Builds/Web: index.html plus Build/ and TemplateData/
// folders. Copy that whole folder onto any static website and open index.html.
//
// Settings chosen so it works on plain static hosting (Vercel, GitHub Pages, itch.io)
// with no special server setup:
//   Gzip + "decompression fallback": the browser's JavaScript unzips the files itself,
//   so the server doesn't need to send Content-Encoding headers.
public static class WebBuilder
{
    const string Scene = "Assets/Scenes/Checkpoint.unity";
    const string Output = "Builds/Web";

    [MenuItem("Documents Por Favor/Build for Web")]
    static void Build()
    {
        if (!EditorUtility.DisplayDialog("Build for Web",
                "Builds the game for the browser into Builds/Web.\n\n" +
                "The first time, Unity switches the project to WebGL, which can take several minutes.",
                "Build", "Cancel"))
            return;

        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        // "Minimal" = just the game canvas, no Unity footer bar. Best for putting in an iframe.
        PlayerSettings.WebGL.template = "APPLICATION:Minimal";
        PlayerSettings.defaultWebScreenWidth = 960; // 16:9, like FixedAspectCamera
        PlayerSettings.defaultWebScreenHeight = 540;
        PlayerSettings.runInBackground = true;

        var options = new BuildPlayerOptions
        {
            scenes = new[] { Scene },
            locationPathName = Output,
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
        {
            string full = Path.GetFullPath(Output);
            Debug.Log($"Web build done ({report.summary.totalSize / (1024 * 1024)} MB): {full}");
            EditorUtility.RevealInFinder(Path.Combine(full, "index.html"));
        }
        else
        {
            Debug.LogError($"Web build failed: {report.summary.result}. See the Console for the first red error.");
        }
    }
}
