using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// The title card with the content note. Waits for START.
public class TitleScreen : MonoBehaviour
{
    [SerializeField] GameObject root;
    [SerializeField] Button startButton;

    bool started;

    void Awake()
    {
        startButton.onClick.AddListener(() => started = true);
    }

    public IEnumerator Show()
    {
        started = false;
        root.SetActive(true);
        while (!started)
            yield return null;
        root.SetActive(false);
    }
}
