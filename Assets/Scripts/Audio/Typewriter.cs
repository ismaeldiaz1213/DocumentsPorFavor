using System.Collections;
using TMPro;
using UnityEngine;

// Types a line into a text box one letter at a time, with a sound as it goes:
// a talking blip pitched for the speaker, or a typewriter clack for narration.
// Added automatically:  Typewriter.On(lineText).Type("Hola", "Lupita");
public class Typewriter : MonoBehaviour
{
    [SerializeField] float lettersPerSecond = 45f;

    TMP_Text text;
    string voice;       // null = typewriter clacks instead of a voice
    bool finishNow;

    public bool IsTyping { get; private set; }

    public static Typewriter On(TMP_Text text)
    {
        Typewriter typewriter = text.GetComponent<Typewriter>();
        if (typewriter == null) typewriter = text.gameObject.AddComponent<Typewriter>();
        typewriter.text = text;
        return typewriter;
    }

    // Starts typing (and stops any line that was still typing).
    public void Type(string line, string speaker)
    {
        voice = speaker;
        text.text = line;
        StopAllCoroutines();
        finishNow = false;
        if (isActiveAndEnabled)
            StartCoroutine(TypeRoutine());
        else
            text.maxVisibleCharacters = 99999;
    }

    // The whole line at once, no typing and no sound.
    public void Show(string line)
    {
        StopAllCoroutines();
        text.text = line;
        text.maxVisibleCharacters = 99999;
        IsTyping = false;
    }

    // Use this one to wait in a coroutine until the line is fully shown.
    public IEnumerator TypeAndWait(string line, string speaker)
    {
        Type(line, speaker);
        while (IsTyping)
            yield return null;
    }

    // Show the whole line right away (the player clicked to skip).
    public void Finish() => finishNow = true;

    IEnumerator TypeRoutine()
    {
        IsTyping = true;
        text.ForceMeshUpdate();
        int total = text.textInfo.characterCount;

        float shown = 0f;
        int lastShown = 0;
        while (lastShown < total && !finishNow)
        {
            shown += lettersPerSecond * Time.deltaTime;
            int now = Mathf.Min(total, Mathf.FloorToInt(shown));
            for (int i = lastShown; i < now; i++)
                LetterSound(i);
            lastShown = now;
            text.maxVisibleCharacters = now;
            yield return null;
        }

        text.maxVisibleCharacters = 99999;
        IsTyping = false;
    }

    void LetterSound(int index)
    {
        char letter = text.textInfo.characterInfo[index].character;
        if (char.IsWhiteSpace(letter)) return;

        if (voice == null)
            Sfx.PlayVaried(Sound.Type, 0.6f);
        else if (index % 3 == 0) // one blip per few letters sounds like talking, not beeping
            Sfx.Voice(voice);
    }

    void OnDisable()
    {
        // If the box is hidden mid-line, never leave it half-typed.
        if (text != null) text.maxVisibleCharacters = 99999;
        IsTyping = false;
    }
}
