using UnityEditor;
using UnityEditor.SceneManagement;

// Menu "Documents Por Favor > Build Everything": runs every generator in the right order.
// 1. Art (sprites, X-ray pictures, scene, car prefab), 2. Story (vehicles + travelers for all 4 days,
// which use the X-ray pictures), 3. Desk UI and pages, 4. Sounds.
public static class BuildEverything
{
    [MenuItem("Documents Por Favor/Build Everything", priority = 0)]
    static void Run()
    {
        if (!EditorUtility.DisplayDialog("Build Everything",
                "Regenerates the art, rewrites the story for all 4 days, rebuilds the desk and the sounds.\n\n" +
                "Your car colors and hand-built X-ray panel are kept. Save afterward with Ctrl+S.",
                "Build", "Cancel"))
            return;

        ArtGenerator.GenerateAndApply();
        StoryWriter.WriteAll();
        DeskBuilder.Build();
        SoundGenerator.GenerateAll();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}
