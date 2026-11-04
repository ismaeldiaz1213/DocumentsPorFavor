using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The title screen: the checkpoint at sunset, a waving flag, the name of the game
// stamped on like a document, the content note, and START SHIFT. Waits for a click,
// SPACE or ENTER. It's built in code the first time it's shown; the plain title
// page made by Build Desk ("root") stays hidden.
public class TitleScreen : MonoBehaviour
{
    [SerializeField] GameObject root;
    [SerializeField] Button startButton;
    [SerializeField] string credit = "A game by Ismael Diaz";

    [TextArea(2, 4)]
    [SerializeField] string contentNote =
        "This game deals with immigration, human smuggling, detention and death. " +
        "Nothing graphic is ever shown, but some of it is heavy. The jokes are real too.";

    bool started;
    RectTransform screen;
    CanvasGroup group;
    RectTransform titleBlock;
    RectTransform stamp;
    RectTransform start;
    Image flag;

    void Awake()
    {
        startButton.onClick.AddListener(() => started = true);
    }

    public IEnumerator Show()
    {
        started = false;
        root.SetActive(false);
        if (screen == null) Build();
        screen.gameObject.SetActive(true);
        group.alpha = 1f;
        Sfx.Loop(Sound.Music, true, 0.6f);

        Coroutine intro = StartCoroutine(Entrance());
        Coroutine wave = StartCoroutine(Wave());
        float t = 0f;
        while (!started)
        {
            if (NoteScreen.ContinuePressed()) started = true;
            t += Time.deltaTime;
            start.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(t * 4f)); // a "press me" pulse
            yield return null;
        }
        StopCoroutine(intro);
        Sfx.Play(Sound.Stamp);
        // The music carries on into the orientation (it lowers itself there).

        // Fade to the orientation's dark background, never to the desk underneath.
        Image curtain = UiKit.Panel("Curtain", screen, 0, 0, 1920, 1080, Palette.WithAlpha(Palette.Ink, 0f));
        UiKit.StretchFull(curtain.rectTransform);
        for (float e = 0f; e < 0.5f; e += Time.deltaTime)
        {
            curtain.color = Palette.WithAlpha(Palette.Ink, e / 0.5f);
            yield return null;
        }
        Destroy(curtain.gameObject);
        StopCoroutine(wave);
        screen.gameObject.SetActive(false);
    }

    // ---------- Building it ----------

    void Build()
    {
        screen = UiKit.Stretch("Title", root.transform.parent);
        screen.SetSiblingIndex(root.transform.GetSiblingIndex() + 1);
        group = screen.gameObject.AddComponent<CanvasGroup>();

        // The checkpoint at sunset, filling the screen.
        Image backdrop = screen.gameObject.AddComponent<Image>();
        backdrop.sprite = PixelArt.TitleBackdrop();
        backdrop.color = Color.white;

        // Darken the top so the title reads.
        for (int i = 0; i < 12; i++)
        {
            Image shade = UiKit.Panel("Shade", screen, 0, 1080 - (i + 1) * 45, 1920, 45, Palette.WithAlpha(Palette.Ink, 0.55f * (1f - i / 12f)));
            shade.raycastTarget = false;
        }

        flag = UiKit.Picture("Flag", screen, PixelArt.Flag(0), 1560, 505, 300, 190); // its pole stands on the canopy

        titleBlock = UiKit.Rect("TitleBlock", screen);
        UiKit.Place(titleBlock, 0, 640, 1920, 380);
        TextMeshProUGUI shadow = UiKit.Label("Shadow", titleBlock, "DOCUMENTS,", 150, Palette.WithAlpha(Palette.Ink, 0.8f),
            TextAlignmentOptions.Center, 8, 182, 1920, 170, bold: true);
        shadow.characterSpacing = 6;
        TextMeshProUGUI title = UiKit.Label("Title", titleBlock, "DOCUMENTS,", 150, Palette.Paper,
            TextAlignmentOptions.Center, 0, 190, 1920, 170, bold: true);
        title.characterSpacing = 6;

        // "POR FAVOR!" is stamped on in red, a little crooked: a pale ink box, a thick border, the words.
        Image box = UiKit.Panel("Stamp", titleBlock, 610, 30, 700, 140, Palette.WithAlpha(Palette.Paper, 0.85f));
        box.raycastTarget = false;
        UiKit.Frame("Border", box.transform, 8, 8, 684, 124, 7, Palette.Reject);
        UiKit.Label("Words", box.transform, "POR FAVOR!", 104, Palette.Reject, TextAlignmentOptions.Center, 20, 8, 660, 124, bold: true);
        stamp = box.rectTransform;
        stamp.pivot = new Vector2(0.5f, 0.5f);
        stamp.anchoredPosition += new Vector2(350, 70);
        stamp.localEulerAngles = new Vector3(0, 0, -6f);


        // START SHIFT reuses the scene's button, so everything listening to it still works.
        start = (RectTransform)startButton.transform;
        start.SetParent(screen, false);
        UiKit.Place(start, 720, 300, 480, 96);
        start.pivot = new Vector2(0.5f, 0.5f);
        start.anchoredPosition += new Vector2(240, 48);
        startButton.image.color = Palette.Approve;
        TMP_Text startLabel = startButton.GetComponentInChildren<TMP_Text>(true);
        if (startLabel != null)
        {
            startLabel.enableAutoSizing = true;
            startLabel.fontSizeMin = 14f;
            startLabel.text = "START SHIFT";
        }
        UiKit.Label("Press", screen, "or press SPACE", 22, Palette.WithAlpha(Palette.Paper, 0.8f), TextAlignmentOptions.Center, 720, 262, 480, 32);

        // Along the bottom, under the line of cars.
        Image note = UiKit.Panel("ContentNote", screen, 360, 10, 1200, 100, Palette.WithAlpha(Palette.Speech, 0.92f));
        note.raycastTarget = false;
        UiKit.Label("Heading", note.transform, "CONTENT NOTE", 18, Palette.Warning, TextAlignmentOptions.Center, 20, 68, 1160, 28, bold: true);
        UiKit.Label("Text", note.transform, contentNote, 21, Palette.Paper, TextAlignmentOptions.Center, 30, 6, 1140, 62);

        UiKit.Label("Credit", screen, credit, 20, Palette.WithAlpha(Palette.Paper, 0.75f), TextAlignmentOptions.Right, 1580, 16, 320, 60);
    }

    // The title drops in, then POR FAVOR! slams down with a stamp.
    IEnumerator Entrance()
    {
        Vector2 home = titleBlock.anchoredPosition;
        stamp.gameObject.SetActive(false);
        for (float e = 0f; e < 0.6f; e += Time.deltaTime)
        {
            float k = 1f - Mathf.Pow(1f - e / 0.6f, 3f);
            titleBlock.anchoredPosition = home + new Vector2(0, Mathf.Lerp(260f, 0f, k));
            yield return null;
        }
        titleBlock.anchoredPosition = home;

        yield return new WaitForSeconds(0.25f);
        stamp.gameObject.SetActive(true);
        Sfx.Play(Sound.Stamp);
        for (float e = 0f; e < 0.15f; e += Time.deltaTime)
        {
            stamp.localScale = Vector3.one * Mathf.Lerp(1.7f, 1f, e / 0.15f);
            yield return null;
        }
        stamp.localScale = Vector3.one;
    }

    IEnumerator Wave()
    {
        int frame = 0;
        while (true)
        {
            flag.sprite = PixelArt.Flag(frame);
            frame = (frame + 1) % PixelArt.FlagFrames;
            yield return new WaitForSeconds(0.11f);
        }
    }
}
