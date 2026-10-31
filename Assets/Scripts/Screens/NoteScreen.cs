using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A sheet of paper with a title, some text and one button. Used for the intro
// story and the jefe's morning memos. The text can type itself out like a typewriter.
public class NoteScreen : MonoBehaviour
{
    [SerializeField] GameObject root;
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text bodyText;
    [SerializeField] Button button;
    [SerializeField] TMP_Text buttonLabel;

    bool clicked;

    void Awake()
    {
        button.onClick.AddListener(() => clicked = true);
        root.SetActive(false);
    }

    public IEnumerator Show(string title, string body, string buttonText, bool typed)
    {
        titleText.text = title;
        buttonLabel.text = buttonText;
        root.SetActive(true);
        Sfx.Play(Sound.Paper);

        Typewriter typewriter = Typewriter.On(bodyText);
        if (typed) typewriter.Type(body, null);
        else typewriter.Show(body);

        clicked = false;
        while (true)
        {
            if (clicked)
            {
                clicked = false;
                if (typewriter.IsTyping) typewriter.Finish(); // first click: show it all
                else break;                                   // second click: move on
            }
            yield return null;
        }
        root.SetActive(false);
    }
}
