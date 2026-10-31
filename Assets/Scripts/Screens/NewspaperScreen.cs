using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The evening paper: one big front-page story and up to three smaller ones per page.
// A busy night gets more pages.
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

    // stories[0] is the front-page story; the rest fill the side columns, then the next pages.
    public IEnumerator Show(string date, List<NewsStory> stories, string buttonLabel)
    {
        int perPage = 1 + sideHeadlines.Length;
        int pages = Mathf.Max(1, Mathf.CeilToInt(stories.Count / (float)perPage));

        root.SetActive(true);
        for (int page = 0; page < pages; page++)
        {
            bool lastPage = page == pages - 1;
            dateText.text = pages > 1 ? $"{date}  ·  PAGE {page + 1}" : date;
            continueLabel.text = lastPage ? buttonLabel : "TURN PAGE";
            ShowPage(stories, page * perPage);

            closed = false;
            while (!closed)
                yield return null;
        }
        root.SetActive(false);
    }

    void ShowPage(List<NewsStory> stories, int first)
    {
        NewsStory main = first < stories.Count ? stories[first] : null;
        mainHeadline.text = main != null ? main.headline : "";
        mainBody.text = main != null ? main.body : "";

        for (int i = 0; i < sideHeadlines.Length; i++)
        {
            NewsStory story = first + i + 1 < stories.Count ? stories[first + i + 1] : null;
            sideHeadlines[i].text = story != null ? story.headline : "";
            sideBodies[i].text = story != null ? story.body : "";
        }
    }
}
