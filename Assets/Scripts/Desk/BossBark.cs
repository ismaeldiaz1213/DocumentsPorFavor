using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// The boss yelling at you for wasting time. Goes on a TextMeshPro text object.
public class BossBark : MonoBehaviour
{
    [SerializeField] float showSeconds = 2.5f;
    [TextArea]
    [SerializeField] List<string> lines = new List<string>
    {
        "That's a SPARE TIRE, genius. -1.",
        "Are you scanning cars or playing Where's Waldo? Oh wait. -1.",
        "That's the engine. Every car has one. -1.",
        "Stop clicking on tamales. -1.",
        "I'm docking you for that. And for your face. Mostly that. -1.",
    };

    TMP_Text text;
    int lastLine = -1;

    void Awake()
    {
        text = GetComponent<TMP_Text>();
        if (text == null)
        {
            Debug.LogError("BossBark needs to be on a 'Text - TextMeshPro' object.", this);
            enabled = false;
            return;
        }
        text.text = "";
    }

    public void Yell()
    {
        if (text == null || lines.Count == 0) return;

        // Pick a random line, but never the same one twice in a row.
        int index;
        do index = Random.Range(0, lines.Count);
        while (lines.Count > 1 && index == lastLine);
        lastLine = index;

        text.text = "BOSS: " + lines[index];
        Sfx.Play(Sound.Boss);
        StopAllCoroutines();
        StartCoroutine(ClearAfterDelay());
    }

    IEnumerator ClearAfterDelay()
    {
        yield return new WaitForSeconds(showSeconds);
        text.text = "";
    }
}
