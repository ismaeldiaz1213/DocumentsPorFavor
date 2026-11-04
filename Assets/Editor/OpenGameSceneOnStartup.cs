using UnityEditor;
using UnityEditor.SceneManagement;

// On a fresh clone (or a new computer) Unity doesn't know which scene was open last,
// because Library/ and UserSettings/ aren't in git, so it shows an empty "Untitled"
// scene. This opens the game's scene instead, once per editor session, but only
// if nothing real is open yet.
[InitializeOnLoad]
static class OpenGameSceneOnStartup
{
    const string Scene = "Assets/Scenes/Checkpoint.unity";
    const string DoneKey = "DocumentsPorFavor.OpenedGameScene";

    static OpenGameSceneOnStartup()
    {
        if (SessionState.GetBool(DoneKey, false)) return;
        SessionState.SetBool(DoneKey, true);
        EditorApplication.delayCall += OpenIfUntitled; // wait until the editor has finished loading
    }

    static void OpenIfUntitled()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var active = EditorSceneManager.GetActiveScene();
        bool untitled = string.IsNullOrEmpty(active.path) && !active.isDirty && EditorSceneManager.sceneCount == 1;
        if (untitled && System.IO.File.Exists(Scene))
            EditorSceneManager.OpenScene(Scene);
    }
}
