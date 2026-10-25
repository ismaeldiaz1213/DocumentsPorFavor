using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Primo José's speech box. He walks you through your first traveler.
public class TutorialGuide : MonoBehaviour
{
    [SerializeField] GameObject root;
    [SerializeField] TMP_Text lineText;
    [SerializeField] Button okButton;

    bool acknowledged;

    // True while a line is on screen waiting for the player to press OK.
    public bool IsWaiting { get; private set; }

    void Awake()
    {
        okButton.onClick.AddListener(() => acknowledged = true);
        root.SetActive(false);
    }

    // Shows a line and keeps playing.
    public void Say(string line)
    {
        root.SetActive(true);
        lineText.text = line;
        okButton.gameObject.SetActive(false);
    }

    // Shows a line and waits for OK.
    public IEnumerator SayAndWait(string line)
    {
        root.SetActive(true);
        lineText.text = line;
        okButton.gameObject.SetActive(true);
        acknowledged = false;
        IsWaiting = true;
        while (!acknowledged)
            yield return null;
        IsWaiting = false;
        okButton.gameObject.SetActive(false);
    }

    public void Hide() => root.SetActive(false);
}
