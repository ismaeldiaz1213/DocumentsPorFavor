using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The last page: your record, your money, your family, and what became of everyone.
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

    public void Show(string title, string body)
    {
        titleText.text = title;
        bodyText.text = body;
        root.SetActive(true);
    }
}
