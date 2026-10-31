using System.Collections;
using UnityEngine;

// Primo José, the checkpoint's security guard. He walks over to your booth when
// there's something new to learn, and leans in to talk in the same dialogue box
// as everyone else (the left side of the desk), so you learn where to look.
public class TutorialGuide : MonoBehaviour
{
    [SerializeField] DeskUI desk;
    [SerializeField] CheckpointStage stage;

    const string Speaker = "PRIMO JOSÉ  ·  SECURITY";
    static readonly Face JoseFace = PortraitArt.Guard();

    bool here;

    // True while a line is on screen waiting for the player to press OK.
    public bool IsWaiting { get; private set; }

    public IEnumerator WalkOver()
    {
        if (here) yield break;
        here = true;
        yield return stage.GuardWalksOver();
    }

    // Back to his post. (If he's escorting someone you imprisoned, he leaves with them instead.)
    public IEnumerator WalkBack()
    {
        Hide();
        if (!here) yield break;
        here = false;
        yield return stage.GuardWalksBack();
    }

    public void LeftWithPrisoner() => here = false;

    // Shows a line and keeps playing.
    public void Say(string line) => desk.Aside(Speaker, JoseFace, line);

    // Shows a line and waits for OK.
    public IEnumerator SayAndWait(string line)
    {
        IsWaiting = true;
        yield return desk.AsideAndWait(Speaker, JoseFace, line);
        IsWaiting = false;
    }

    public void Hide() => desk.EndAside();
}
