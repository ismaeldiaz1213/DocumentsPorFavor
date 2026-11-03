using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
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
        buttonLabel.enableAutoSizing = true; // room for the [SPACE] hint
        buttonLabel.fontSizeMin = 12f;
        buttonLabel.fontSizeMax = buttonLabel.fontSize;
        root.SetActive(false);
    }

    public IEnumerator Show(string title, string body, string buttonText, bool typed)
    {
        titleText.text = title;
        buttonLabel.text = $"{buttonText} <size=60%><alpha=#B0>[SPACE]</size>";
        root.SetActive(true);
        Sfx.Play(Sound.Paper);

        Typewriter typewriter = Typewriter.On(bodyText);
        if (typed) typewriter.Type(body, null);
        else typewriter.Show(body);

        clicked = false;
        while (true)
        {
            if (ContinuePressed()) clicked = true;
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

    // SPACE or ENTER works like the button.
    public static bool ContinuePressed()
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame ||
                                    keyboard.numpadEnterKey.wasPressedThisFrame);
    }
}
