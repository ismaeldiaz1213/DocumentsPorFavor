using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The last page: how your job ended, your money, and your family.
// Shown when you finish the last day, or earlier if you're fired or lose your home.
public class EndingScreen : MonoBehaviour
{
    [SerializeField] GameObject root;
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text bodyText;
    [SerializeField] Button playAgainButton;

    void Awake()
    {
        playAgainButton.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().name));
        root.SetActive(false);
    }

    public void Show(string title, string body, bool kept)
    {
        titleText.text = title;
        titleText.color = kept ? Palette.PaperInk : Palette.Reject;
        bodyText.text = body;
        root.SetActive(true);
        Sfx.Play(kept ? Sound.Win : Sound.Lose);
    }
}
