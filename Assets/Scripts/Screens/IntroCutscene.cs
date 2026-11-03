using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// The orientation slideshow before the first shift: one picture and a sentence or
// two per page, each page telling you a little more about the job. It builds itself
// in code on top of the given canvas, so there's nothing to set up in the scene.
//     yield return IntroCutscene.Play(canvas, pages);
// Click, SPACE or ENTER: finish the line, then the next page. SKIP jumps to the end.
public class IntroCutscene : MonoBehaviour
{
    public enum Picture { Flag, Badge, Booth, Passport, Record, Memo, Keys, Home }

    public struct Page
    {
        public Picture picture;
        public string title;
        public string body;
        public Page(Picture picture, string title, string body)
        {
            this.picture = picture;
            this.title = title;
            this.body = body;
        }
    }

    // The orientation, in order. Each page adds a little more.
    public static List<Page> Orientation(string employeeNumber) => new List<Page>
    {
        new Page(Picture.Flag, $"CONGRATULATIONS, EMPLOYEE #{employeeNumber}!",
            "You have been selected as the newest BORDER INSPECTOR of the United States of America."),
        new Page(Picture.Badge, "YOU ARE NOT CROSSING THE BORDER.",
            "You ARE the border. This is your badge. Wear it at all times."),
        new Page(Picture.Booth, "BOOTH 3  ·  SANTA TERESA PORT OF ENTRY",
            "Every day, cars line up on the Mexican side. One at a time, each driver walks up to your window and hands you their papers."),
        new Page(Picture.Passport, "CHECK THE PAPERS. DECIDE.",
            "Compare every document with the RULEBOOK.\nEverything in order: <color=#6E8B57><b>APPROVE</b></color>.   Anything wrong: <color=#A8503F><b>REJECT</b></color>."),
        new Page(Picture.Record, "YOU ARE PAID PER CORRECT DECISION.",
            "Every mistake prints a CITATION and goes on your record.\nYour supervisor reads your record. He does not like to read it twice."),
        new Page(Picture.Memo, "THE RULES WILL CHANGE.",
            "Each morning a memo tells you what's new: permits, then an X-ray scanner, then more.\nRead the memo. Then read the rulebook again."),
        new Page(Picture.Keys, "AT YOUR BOOTH",
            "Click the buttons, or use the keys. The RULES book has pictures of what good and bad papers look like."),
        new Page(Picture.Home, "WELCOME ABOARD, INSPECTOR.",
            "Your country thanks you for your service.\nYour family is counting on the paycheck."),
    };

    RectTransform root;
    RectTransform pictureArea;
    TextMeshProUGUI titleText;
    TextMeshProUGUI bodyText;
    TextMeshProUGUI pageNumber;
    CanvasGroup group;
    bool skip;
    bool advance;

    public static IEnumerator Play(Transform canvas, List<Page> pages)
    {
        var go = new GameObject("IntroCutscene", typeof(RectTransform));
        go.transform.SetParent(canvas, false);
        go.transform.SetAsLastSibling();
        IntroCutscene cutscene = go.AddComponent<IntroCutscene>();
        cutscene.Build();
        yield return cutscene.Run(pages);
        Destroy(go);
    }

    void Build()
    {
        root = (RectTransform)transform;
        UiKit.StretchFull(root);
        group = gameObject.AddComponent<CanvasGroup>();

        // The whole screen is the "next" button.
        Image backdrop = gameObject.AddComponent<Image>();
        backdrop.color = Palette.Ink;
        Button anywhere = gameObject.AddComponent<Button>();
        anywhere.transition = Selectable.Transition.None;
        anywhere.onClick.AddListener(() => advance = true);

        // A government letterhead strip, top and bottom.
        UiKit.Panel("TopStripe", root, 0, 1060, 1920, 20, Palette.Reject).raycastTarget = false;
        UiKit.Panel("TopStripe2", root, 0, 1052, 1920, 8, Palette.Paper).raycastTarget = false;
        UiKit.Panel("BottomStripe", root, 0, 0, 1920, 20, Palette.RegistrationBlue).raycastTarget = false;
        UiKit.Label("Agency", root, "U.S. DEPARTMENT OF BORDER INSPECTION  ·  NEW HIRE ORIENTATION",
            20, Palette.WithAlpha(Palette.Paper, 0.55f), TextAlignmentOptions.Center, 0, 1005, 1920, 36, bold: true);

        pictureArea = UiKit.Rect("Picture", root);
        UiKit.Place(pictureArea, 560, 430, 800, 540);

        titleText = UiKit.Label("Title", root, "", 54, Palette.Warning, TextAlignmentOptions.Center, 120, 300, 1680, 90, bold: true);
        bodyText = UiKit.Label("Body", root, "", 34, Palette.Paper, TextAlignmentOptions.Top, 260, 110, 1400, 190);
        bodyText.enableAutoSizing = false;

        pageNumber = UiKit.Label("PageNumber", root, "", 20, Palette.WithAlpha(Palette.Paper, 0.5f), TextAlignmentOptions.Left, 40, 40, 400, 30);
        UiKit.Label("Hint", root, "CLICK  ·  SPACE  ·  ENTER", 20, Palette.WithAlpha(Palette.Paper, 0.5f),
            TextAlignmentOptions.Right, 1480, 40, 400, 30);

        Button skipButton = UiKit.Button("Skip", root, 1740, 960, 150, 50, Palette.Imprison, "SKIP »", 22, out _);
        skipButton.onClick.AddListener(() => skip = true);
        Sfx.HookButtons();
    }

    IEnumerator Run(List<Page> pages)
    {
        Sfx.Loop(Sound.Music, true, 0.35f);
        for (int i = 0; i < pages.Count && !skip; i++)
        {
            yield return ShowPage(pages[i], i, pages.Count);
            yield return WaitForAdvance();
        }
        Sfx.Loop(Sound.Music, false);

        for (float t = 0f; t < 0.35f; t += Time.deltaTime)
        {
            group.alpha = 1f - t / 0.35f;
            yield return null;
        }
    }

    IEnumerator ShowPage(Page page, int index, int count)
    {
        for (int i = pictureArea.childCount - 1; i >= 0; i--)
            Destroy(pictureArea.GetChild(i).gameObject);

        pageNumber.text = $"{index + 1} / {count}";
        titleText.text = page.title;
        Typewriter.On(bodyText).Type("", null);
        Sfx.Play(index == 0 ? Sound.Bell : Sound.Paper);

        RectTransform picture = DrawPicture(page.picture);

        // The picture drops in, a little bounce, then the words type out.
        float elapsed = 0f;
        const float seconds = 0.35f;
        while (elapsed < seconds && !skip)
        {
            elapsed += Time.deltaTime;
            float k = Mathf.Clamp01(elapsed / seconds);
            float bounce = 1f + Mathf.Sin(k * Mathf.PI) * 0.06f;
            picture.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, k) * bounce;
            picture.anchoredPosition = new Vector2(0f, Mathf.Lerp(40f, 0f, k));
            titleText.alpha = k;
            yield return null;
        }
        picture.localScale = Vector3.one;
        picture.anchoredPosition = Vector2.zero;
        titleText.alpha = 1f;

        if (page.picture == Picture.Passport) StartCoroutine(StampLater(picture));
        Typewriter.On(bodyText).Type(page.body, null);
        advance = false;
    }

    IEnumerator WaitForAdvance()
    {
        Typewriter typewriter = Typewriter.On(bodyText);
        while (!skip)
        {
            // Never leave a button "selected", or SPACE would press it a second time.
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            if (Pressed()) advance = true;
            if (advance)
            {
                advance = false;
                if (typewriter.IsTyping) typewriter.Finish();
                else yield break;
            }
            yield return null;
        }
    }

    static bool Pressed()
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame ||
                                    keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame);
    }

    // ---------- Pictures ----------

    RectTransform DrawPicture(Picture picture)
    {
        RectTransform holder = UiKit.Rect(picture.ToString(), pictureArea);
        UiKit.StretchFull(holder);
        holder.pivot = new Vector2(0.5f, 0.5f);

        switch (picture)
        {
            case Picture.Flag:
                Image flag = Full(holder, PixelArt.Flag(0));
                StartCoroutine(Wave(flag));
                break;
            case Picture.Badge:
                Full(holder, PixelArt.Badge(), 140, 20, 520, 500);
                UiKit.Label("Number", holder, "INSPECTOR\n#223891", 30, Palette.Paper, TextAlignmentOptions.Left, 630, 200, 240, 120, bold: true);
                break;
            case Picture.Booth:
                Full(holder, PixelArt.Booth());
                UiKit.Label("You", holder, "YOU", 30, Palette.Warning, TextAlignmentOptions.Center, 487, 345, 200, 40, bold: true);
                UiKit.Panel("Arrow", holder, 584, 300, 6, 42, Palette.Warning).raycastTarget = false;
                break;
            case Picture.Passport:
                Full(holder, PixelArt.Passport(), 220, 20, 360, 480);
                break;
            case Picture.Record:
                Full(holder, PixelArt.Record(), 200, 20, 400, 500);
                break;
            case Picture.Memo:
                Full(holder, PixelArt.Memo(), 160, 20, 480, 480);
                break;
            case Picture.Keys:
                DrawKeys(holder);
                break;
            case Picture.Home:
                Full(holder, PixelArt.Home());
                break;
        }
        return holder;
    }

    static Image Full(RectTransform holder, Sprite sprite, float x = 0, float y = 0, float w = 800, float h = 540) =>
        UiKit.Picture("Art", holder, sprite, x, y, w, h);

    IEnumerator Wave(Image flag)
    {
        int frame = 0;
        while (flag != null)
        {
            flag.sprite = PixelArt.Flag(frame);
            frame = (frame + 1) % PixelArt.FlagFrames;
            yield return new WaitForSeconds(0.11f);
        }
    }

    // APPROVED and REJECTED land on the passport, one after the other.
    IEnumerator StampLater(RectTransform picture)
    {
        yield return new WaitForSeconds(0.5f);
        if (picture == null) yield break;
        Stamp(picture, "APPROVED", Palette.Approve, 470, 300, -12f);
        Sfx.PlayVaried(Sound.Stamp);
        yield return new WaitForSeconds(0.6f);
        if (picture == null) yield break;
        Stamp(picture, "REJECTED", Palette.Reject, 30, 120, 9f);
        Sfx.PlayVaried(Sound.Stamp);
    }

    static void Stamp(RectTransform parent, string word, Color color, float x, float y, float angle)
    {
        Image box = UiKit.Panel(word, parent, x, y, 300, 90, Palette.WithAlpha(color, 0.15f));
        box.raycastTarget = false;
        Outline outline = box.gameObject.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(5, 5);
        outline.useGraphicAlpha = false;
        UiKit.Label("Word", box.transform, word, 46, color, TextAlignmentOptions.Center, 0, 0, 300, 90, bold: true);
        box.rectTransform.localEulerAngles = new Vector3(0, 0, angle);
    }

    static void DrawKeys(RectTransform holder)
    {
        var rows = new (string key, string what)[]
        {
            ("N", "Call the next car  (the NEXT megaphone)"),
            ("SPACE", "Hear what the driver says next"),
            ("A", "APPROVE"),
            ("R", "REJECT"),
            ("I", "IMPRISON  (once the X-ray arrives)"),
            ("TAB", "Open / close the RULES book"),
        };
        for (int i = 0; i < rows.Length; i++)
        {
            float y = 470 - i * 78;
            UiKit.Key(holder, rows[i].key, 120, y);
            UiKit.Label("What", holder, rows[i].what, 30, Palette.Paper, TextAlignmentOptions.Left, 290, y - 4, 560, 48);
        }
    }
}
