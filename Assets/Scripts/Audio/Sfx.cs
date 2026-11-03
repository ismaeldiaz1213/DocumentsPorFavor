using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Every sound in the game. Each name matches a file in Assets/Resources/Sounds
// (plus extra takes like Stamp_2, Stamp_3), made by the menu "Documents Por Favor > Generate Sounds".
public enum Sound
{
    Click, Stamp, Paper, Megaphone, Door, Step, KeyChime, EngineStart, CarPass, Brake, Horn, TowBeep,
    ScanSweep, ScanDone, Found, Wrong, Boss, Printer, Cash, Type, Rustle, Bell, Blip,
    Tick, Whistle, Breeze, Gust, Tumbleweed, Music, Lose, Win, Ring,
}

// The game's sound player. It creates itself when the game starts, so there is
// nothing to put in the scene. From any script:
//     Sfx.Play(Sound.Stamp);              one-shot (a random take, a little different every time)
//     Sfx.Loop(Sound.Music, true);        start a loop (false fades it out)
//     Sfx.Voice("Lupita");                one talking blip, pitched for that speaker
// Every Button in the scene clicks automatically.
public class Sfx : MonoBehaviour
{
    const int Voices = 16;        // how many one-shots can overlap
    const float FadeSpeed = 2f;   // loops fade in/out over about half a second

    static Sfx instance;

    readonly Dictionary<Sound, List<AudioClip>> clips = new Dictionary<Sound, List<AudioClip>>();
    readonly Dictionary<Sound, int> lastTake = new Dictionary<Sound, int>();
    readonly Dictionary<Sound, AudioSource> loops = new Dictionary<Sound, AudioSource>();
    readonly Dictionary<Sound, float> loopTargets = new Dictionary<Sound, float>();
    AudioSource[] voices;
    int nextVoice;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        var go = new GameObject("Sfx");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<Sfx>();
    }

    void Awake()
    {
        foreach (AudioClip clip in Resources.LoadAll<AudioClip>("Sounds"))
        {
            // "Stamp_2" is the second take of Stamp.
            if (!Enum.TryParse(clip.name.Split('_')[0], out Sound sound)) continue;
            if (!clips.ContainsKey(sound)) clips[sound] = new List<AudioClip>();
            clips[sound].Add(clip);
        }

        if (clips.Count == 0)
            Debug.LogWarning("No sounds found. Run the menu Documents Por Favor > Generate Sounds.");

        voices = new AudioSource[Voices];
        for (int i = 0; i < Voices; i++)
        {
            voices[i] = gameObject.AddComponent<AudioSource>();
            voices[i].playOnAwake = false;
        }

        // The scene is already loaded the first time; "Play again" reloads it later.
        HookButtons();
        SceneManager.sceneLoaded += (_, __) => { StopAllLoops(); HookButtons(); };
    }

    void Update()
    {
        // Ease each loop toward its target volume; stop it once it is silent.
        foreach (KeyValuePair<Sound, AudioSource> pair in loops)
        {
            AudioSource source = pair.Value;
            float target = loopTargets[pair.Key];
            source.volume = Mathf.MoveTowards(source.volume, target, FadeSpeed * Time.unscaledDeltaTime);
            if (target == 0f && source.volume == 0f && source.isPlaying)
                source.Stop();
        }
    }

    // ---------- What other scripts call ----------

    public static void Play(Sound sound, float volume = 1f, float pitch = 1f)
    {
        AudioClip clip = instance != null ? instance.PickTake(sound) : null;
        if (clip == null) return;

        // Never exactly the same twice: a hair of random pitch and loudness.
        pitch *= 1f + UnityEngine.Random.Range(-0.03f, 0.03f);
        volume *= UnityEngine.Random.Range(0.9f, 1f);

        AudioSource source = instance.voices[instance.nextVoice];
        instance.nextVoice = (instance.nextVoice + 1) % Voices;
        source.pitch = pitch;
        source.PlayOneShot(clip, volume);
    }

    // Same sound, slightly different each time (footsteps, typing...), so it doesn't sound robotic.
    public static void PlayVaried(Sound sound, float volume = 1f, float spread = 0.08f) =>
        Play(sound, volume * UnityEngine.Random.Range(0.85f, 1f), 1f + UnityEngine.Random.Range(-spread, spread));

    public static void Loop(Sound sound, bool on, float volume = 1f)
    {
        AudioClip clip = instance != null ? instance.PickTake(sound) : null;
        if (clip == null) return;

        if (!instance.loops.TryGetValue(sound, out AudioSource source))
        {
            source = instance.gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.volume = 0f;
            instance.loops[sound] = source;
        }

        instance.loopTargets[sound] = on ? volume : 0f;
        if (on && !source.isPlaying)
            source.Play();
    }

    // One syllable of "talking". Each speaker gets their own pitch from their name.
    public static void Voice(string speaker)
    {
        int hash = 0;
        foreach (char c in speaker ?? "")
            hash = hash * 31 + c;
        float basePitch = 0.75f + (Mathf.Abs(hash) % 100) / 100f * 0.7f; // 0.75 to 1.45
        Play(Sound.Blip, 0.5f, basePitch + UnityEngine.Random.Range(-0.06f, 0.06f));
    }

    // A random take, but never the same one twice in a row.
    AudioClip PickTake(Sound sound)
    {
        if (!clips.TryGetValue(sound, out List<AudioClip> takes)) return null;
        lastTake.TryGetValue(sound, out int last);
        int take = UnityEngine.Random.Range(0, takes.Count);
        if (takes.Count > 1 && take == last) take = (take + 1) % takes.Count;
        lastTake[sound] = take;
        return takes[take];
    }

    void StopAllLoops()
    {
        foreach (AudioSource source in loops.Values) source.Stop();
        foreach (Sound sound in new List<Sound>(loopTargets.Keys)) loopTargets[sound] = 0f;
    }

    // ---------- Buttons ----------

    static void HookButtons()
    {
        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            button.onClick.RemoveListener(ClickSound); // never hook the same button twice
            button.onClick.AddListener(ClickSound);
        }
    }

    static void ClickSound() => Play(Sound.Click, 0.7f);
}
