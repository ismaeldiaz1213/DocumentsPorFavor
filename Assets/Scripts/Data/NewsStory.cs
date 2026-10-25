using UnityEngine;

// A newspaper story triggered by a decision.
[System.Serializable]
public class NewsStory
{
    public string headline;
    [TextArea(2, 6)] public string body;
    [Tooltip("0 = in tonight's paper. 1 = tomorrow night's paper, and so on.")]
    public int daysLater;
    [Tooltip("Big story at the top of the front page.")]
    public bool frontPage;
}
