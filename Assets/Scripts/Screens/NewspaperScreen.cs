using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The evening paper: one big front-page story and up to three smaller ones.
public class NewspaperScreen : MonoBehaviour
{
    [SerializeField] GameObject root;
    [SerializeField] TMP_Text dateText;
    [SerializeField] TMP_Text mainHeadline;
    [SerializeField] TMP_Text mainBody;
    [SerializeField] TMP_Text[] sideHeadlines;
    [SerializeField] TMP_Text[] sideBodies;
    [SerializeField] Button continueButton;
    [SerializeField] TMP_Text continueLabel;

    bool closed;

    void Awake()
    {
        continueButton.onClick.AddListener(() => closed = true);
        root.SetActive(false);
    }

    // stories[0] is the front-page story; the rest fill the side columns.
    public IEnumerator Show(string date, List<NewsStory> stories, string buttonLabel)
    {
        dateText.text = date;
        continueLabel.text = buttonLabel;

        NewsStory main = stories.Count > 0 ? stories[0] : null;
        mainHeadline.text = main != null ? main.headline : "";
        mainBody.text = main != null ? main.body : "";

        for (int i = 0; i < sideHeadlines.Length; i++)
        {
            NewsStory story = i + 1 < stories.Count ? stories[i + 1] : null;
            sideHeadlines[i].text = story != null ? story.headline : "";
            sideBodies[i].text = story != null ? story.body : "";
        }

        closed = false;
        root.SetActive(true);
        while (!closed)
            yield return null;
        root.SetActive(false);
    }
}
