using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Menu "Documents Por Favor > Generate Sounds".
// Builds every sound effect (and the guitar music) out of math: sine waves, noise,
// and simple filters, the same way old game consoles made sound. Saves them as WAV
// files in Assets/Resources/Sounds, where the Sfx script finds them by name.
// Safe to run again.
//
// The building blocks, if you want to tweak a sound:
//   Sine/Square/Saw  a pure tone, a buzzy tone, a brassy tone
//   Noise            hiss; filtered, it becomes paper, gravel, wind, a "t" or an "s"
//   Lowpass/Highpass keep only the low or only the high part of a sound
//   Bandpass         keep a narrow slice; two slices on a buzz make a vowel
//   Decay(t, x)      fades from 1 to 0; smaller x = shorter, snappier sound
//   Pluck            a guitar string (the Karplus-Strong trick: a short loop of noise
//                    that is averaged on every pass, so it rings and then dies out)
public static class SoundGenerator
{
    public const string Folder = "Assets/Resources/Sounds";
    const int Rate = 22050; // samples per second; plenty for retro sound, and small files
    const float TwoPi = Mathf.PI * 2f;

    static System.Random random;

    [MenuItem("Documents Por Favor/Generate Sounds")]
    public static void GenerateAll()
    {
        WriteAll(Folder);
        AssetDatabase.Refresh(); // SoundImportSettings (bottom of this file) sets each file's options
        Debug.Log($"Sounds generated in {Folder}.");
    }

    // ---------- Desk ----------

    static float[] Click()
    {
        float[] s = Buffer(0.035f);
        for (int i = 0; i < s.Length; i++)
        {
            float t = T(i);
            s[i] = Noise() * Decay(t, 0.003f) * 0.6f + Mathf.Sin(TwoPi * 2200f * t) * Decay(t, 0.006f);
        }
        return Highpass(s, 300f);
    }

    // A rubber stamp: a deep thump, a slap of paper, and a little wooden knock.
    static float[] Stamp()
    {
        float[] s = Buffer(0.35f);
        float phase = 0f;
        for (int i = 0; i < s.Length; i++)
        {
            float t = T(i);
            phase += TwoPi * Mathf.Lerp(110f, 45f, Mathf.Clamp01(t / 0.12f)) / Rate;
            s[i] = Mathf.Sin(phase) * Decay(t, 0.07f)
                   + Mathf.Sin(TwoPi * 320f * t) * Decay(t, 0.02f) * 0.4f;
        }
        float[] slap = Lowpass(NoiseBurst(0.35f, 0.025f), 1200f);
        return Add(s, slap, 0.8f);
    }

    // Paper sliding: hiss in the "paper" range, broken into little crinkles.
    static float[] Paper(float seconds, float center)
    {
        float[] s = Buffer(seconds);
        float crinkle = 1f;
        for (int i = 0; i < s.Length; i++)
        {
            if (i % (Rate / 120) == 0) crinkle = 0.3f + (float)random.NextDouble() * 0.7f;
            float t = T(i);
            float shape = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / seconds)); // swells up, then down
            s[i] = Noise() * shape * crinkle;
        }
        return Bandpass(s, center, 0.8f);
    }

    // Newspaper: two big swooshes.
    static float[] Rustle()
    {
        float[] a = Paper(0.45f, 1600f);
        float[] b = Paper(0.4f, 3200f);
        float[] s = Buffer(0.75f);
        MixAt(s, a, 0f, 1f);
        MixAt(s, b, 0.3f, 0.8f);
        return s;
    }

    static float[] Printer()
    {
        float[] s = Buffer(0.9f);
        for (int row = 0; row < 6; row++)
        {
            float start = row * 0.14f;
            for (float t = start; t < start + 0.11f; t += 1f / 170f)
                MixAt(s, NoiseBurst(0.006f, 0.0015f), t, 0.9f);
        }
        for (int i = 0; i < s.Length; i++)
            s[i] += Mathf.Sin(TwoPi * 310f * T(i)) * 0.08f; // the little motor
        return Highpass(s, 500f);
    }

    static float[] TypeKey()
    {
        float[] s = Buffer(0.07f);
        for (int i = 0; i < s.Length; i++)
        {
            float t = T(i);
            s[i] = Noise() * Decay(t, 0.004f)
                   + Mathf.Sin(TwoPi * 1600f * t) * Decay(t, 0.01f) * 0.5f
                   + Mathf.Sin(TwoPi * 180f * t) * Decay(t, 0.015f) * 0.6f;
        }
        return Highpass(s, 120f);
    }

    static float[] Tick()
    {
        float[] s = Buffer(0.03f);
        for (int i = 0; i < s.Length; i++)
        {
            float t = T(i);
            s[i] = Mathf.Sin(TwoPi * 3000f * t) * Decay(t, 0.003f) + Noise() * Decay(t, 0.001f) * 0.4f;
        }
        return s;
    }

    // The service bell on the counter.
    static float[] Bell() => BellTone(1.6f, 1760f, new[] { 1f, 2.32f, 4.25f, 6.63f }, new[] { 1f, 0.5f, 0.25f, 0.12f }, 0.6f);

    // An old desk phone: two short bursts of a bell hammered twenty times a second.
    static float[] Ring()
    {
        float[] s = Buffer(1.5f);
        for (int i = 0; i < s.Length; i++)
        {
            float t = T(i);
            float burst = Mathf.Max(Gate(t, 0f, 0.45f), Gate(t, 0.65f, 1.1f));
            float hammer = Mathf.Sin(TwoPi * 20f * t) > 0f ? 1f : 0.35f;
            float bell = Mathf.Sin(TwoPi * 1250f * t) * 0.6f + Mathf.Sin(TwoPi * 1570f * t) * 0.4f;
            s[i] = bell * hammer * burst;
        }
        return Lowpass(s, 3500f);
    }

    // 1 between start and end, with 10 ms soft edges so it doesn't click.
    static float Gate(float t, float start, float end) =>
        Mathf.Clamp01(Mathf.Min(t - start, end - t) / 0.01f);

    static float[] Cash()
    {
        float[] s = Buffer(0.9f);
        MixAt(s, Lowpass(NoiseBurst(0.1f, 0.02f), 500f), 0f, 0.8f);                     // drawer
        MixAt(s, BellTone(0.7f, 2093f, new[] { 1f, 2f, 3f }, new[] { 1f, 0.4f, 0.2f }, 0.25f), 0.04f, 0.8f); // ching
        for (int i = 0; i < 7; i++)                                                         // coins
        {
            float pitch = 3000f + (float)random.NextDouble() * 4000f;
            MixAt(s, Ping(pitch, 0.08f, 0.02f), 0.1f + (float)random.NextDouble() * 0.45f, 0.35f);
        }
        return s;
    }

    // ---------- Voices ----------

    // "¡Si-guien-te!" through a cheap megaphone: a buzzing "voice" shaped into
    // vowels with two band filters, then squashed and thinned out like a horn.
    static float[] Megaphone()
    {
        float[] s = Buffer(1f);
        MixAt(s, Highpass(NoiseBurst(0.09f, 0.05f), 4000f), 0.06f, 0.5f);          // s
        MixAt(s, Vowel(0.12f, 210f, 235f, 300f, 2300f, 300f, 2300f), 0.13f, 1f);    // i
        MixAt(s, Lowpass(NoiseBurst(0.025f, 0.008f), 800f), 0.26f, 0.6f);          // g
        MixAt(s, Vowel(0.2f, 240f, 220f, 300f, 2200f, 520f, 1850f), 0.28f, 1f);    // ien
        MixAt(s, Highpass(NoiseBurst(0.03f, 0.008f), 2500f), 0.5f, 0.7f);          // t
        MixAt(s, Vowel(0.32f, 230f, 150f, 480f, 1800f, 480f, 1750f), 0.53f, 1f);   // e!

        for (int i = 0; i < s.Length; i++)
        {
            float t = T(i);
            s[i] = (float)Math.Tanh(s[i] * 6f);                                     // overdriven horn
            s[i] += Mathf.Sin(TwoPi * 2800f * t) * 0.08f * Mathf.Clamp01(1f - t / 0.12f); // feedback squeal
        }
        return Lowpass(Highpass(Highpass(s, 450f), 450f), 3200f);
    }

    // A buzz (the vocal cords) gliding in pitch, with two formant filters gliding
    // from one vowel shape to another.
    static float[] Vowel(float seconds, float pitchFrom, float pitchTo, float f1From, float f2From, float f1To, float f2To)
    {
        float[] s = Buffer(seconds);
        var formant1 = new Biquad();
        var formant2 = new Biquad();
        float phase = 0f;
        for (int i = 0; i < s.Length; i++)
        {
            float k = (float)i / s.Length;
            phase += Mathf.Lerp(pitchFrom, pitchTo, k) / Rate;
            float buzz = (phase % 1f) * 2f - 1f;
            formant1.SetBandpass(Mathf.Lerp(f1From, f1To, k), 5f);
            formant2.SetBandpass(Mathf.Lerp(f2From, f2To, k), 8f);
            float envelope = Mathf.Clamp01(k / 0.08f) * Mathf.Clamp01((1f - k) / 0.15f);
            s[i] = (formant1.Process(buzz) + formant2.Process(buzz) * 0.6f) * envelope;
        }
        return s;
    }

    // The jefe grumbling: a low, angry "mrr-grr-mrr".
    static float[] Boss()
    {
        float[] s = Buffer(0.8f);
        var formant1 = new Biquad();
        var formant2 = new Biquad();
        formant1.SetBandpass(700f, 4f);
        formant2.SetBandpass(1150f, 6f);
        float phase = 0f;
        for (int i = 0; i < s.Length; i++)
        {
            float t = T(i);
            phase += (95f + 18f * Mathf.Sin(TwoPi * 3f * t) + (float)random.NextDouble() * 8f) / Rate;
            float buzz = (phase % 1f) * 2f - 1f;
            float syllables = Mathf.Pow(Mathf.Abs(Mathf.Sin(TwoPi * 2.6f * t)), 0.6f);
            float envelope = Mathf.Clamp01(t / 0.03f) * Mathf.Clamp01((0.8f - t) / 0.1f);
            s[i] = (float)Math.Tanh((formant1.Process(buzz) + formant2.Process(buzz) * 0.5f) * 3f) * syllables * envelope;
        }
        return Lowpass(s, 2500f);
    }

    // One talking "syllable", pitched up or down in the game for each character.
    static float[] Blip()
    {
        float[] s = Buffer(0.06f);
        for (int i = 0; i < s.Length; i++)
        {
            float t = T(i);
            s[i] = Square(480f * t) * Mathf.Clamp01(t / 0.004f) * Decay(t, 0.025f);
        }
        return Bandpass(Lowpass(s, 2200f), 900f, 1.2f);
    }

    // ---------- Cars and the checkpoint ----------

    // The "door open / key in" chime: three soft dings.
    static float[] KeyChime()
    {
        float[] s = Buffer(0.85f);
        for (int ding = 0; ding < 3; ding++)
            MixAt(s, Ping(740f, 0.25f, 0.07f, harmonics: true), ding * 0.27f, 1f);
        return Lowpass(s, 2500f);
    }

    // Turning the key: the starter cranks "rr-rr-rr", then the engine catches,
    // revs once and settles (fading out, because the car drives off right after).
    static float[] EngineStart()
    {
        float[] s = Buffer(1.7f);
        float[] grind = Lowpass(NoiseBuffer(1.7f), 900f);
        float phase = 0f;
        for (int i = 0; i < s.Length; i++)
        {
            float t = T(i);
            if (t < 0.65f)
            {
                float crank = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(TwoPi * 8f * t), 2f);
                s[i] = (Saw(170f * t) * 0.3f + grind[i] * 1.2f) * crank * 0.6f;
            }
            else
            {
                float k = t - 0.65f;
                float rpm = k < 0.25f ? Mathf.Lerp(30f, 75f, k / 0.25f) : Mathf.Lerp(75f, 36f, Mathf.Clamp01((k - 0.25f) / 0.5f));
                phase += rpm / Rate;
                float putt = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(TwoPi * phase), 3f);
                float tone = Mathf.Sin(TwoPi * phase) + Mathf.Sin(TwoPi * phase * 2f) * 0.5f + Mathf.Sin(TwoPi * phase * 3f) * 0.25f;
                float loudness = Mathf.Clamp01(k / 0.05f) * Mathf.Clamp01((1.05f - k) / 0.5f);
                s[i] = (tone * 0.5f + grind[i] * putt) * loudness;
            }
        }
        return s;
    }

    // The scanner passing over the car: a gentle rising whirr with soft ticks.
    // About as long as a scan. Not a hum.
    static float[] ScanSweep()
    {
        float[] s = Buffer(4f);
        float phase = 0f;
        for (int i = 0; i < s.Length; i++)
        {
            float t = T(i);
            phase += Mathf.Lerp(320f, 820f, t / 4f) / Rate;
            float envelope = Mathf.Clamp01(t / 0.3f) * Mathf.Clamp01((4f - t) / 0.4f);
            s[i] = Mathf.Sin(TwoPi * phase) * 0.25f * envelope;
        }
        for (float tick = 0.25f; tick < 3.8f; tick += 0.5f)
            MixAt(s, Ping(1800f, 0.04f, 0.008f), tick, 0.35f);
        return s;
    }

    // A car driving past: the engine note climbs, then drops as it goes by.
    static float[] CarPass()
    {
        float[] s = Buffer(1.8f);
        float[] rumble = Lowpass(NoiseBuffer(1.8f), 400f);
        float phase = 0f;
        for (int i = 0; i < s.Length; i++)
        {
            float t = T(i);
            float pitch = t < 0.8f ? Mathf.Lerp(0.8f, 1.25f, t / 0.8f) : Mathf.Lerp(1.25f, 0.85f, (t - 0.8f) / 1f);
            phase += 45f * pitch / Rate;
            float loudness = Mathf.Exp(-Mathf.Pow((t - 0.8f) / 0.45f, 2f));
            float tone = Mathf.Sin(TwoPi * phase) + Mathf.Sin(TwoPi * phase * 2f) * 0.5f + Saw(phase * 3f) * 0.2f;
            s[i] = (tone * 0.6f + rumble[i] * 2f) * loudness;
        }
        return s;
    }

    static float[] Brake()
    {
        float[] s = Buffer(0.7f);
        float[] tires = Lowpass(NoiseBuffer(0.7f), 500f);
        float phase = 0f;
        for (int i = 0; i < s.Length; i++)
        {
            float t = T(i);
            phase += (1900f + 70f * Mathf.Sin(TwoPi * 7f * t)) / Rate;
            float squeal = Mathf.Sin(TwoPi * phase) * Mathf.Clamp01(t / 0.08f) * Mathf.Clamp01((0.55f - t) / 0.2f);
            s[i] = squeal * 0.5f + tires[i] * Decay(t, 0.3f) * 1.5f;
        }
        return s;
    }

    static float[] Horn()
    {
        float[] s = Buffer(0.45f);
        for (int i = 0; i < s.Length; i++)
        {
            float t = T(i);
            float envelope = Mathf.Clamp01(t / 0.01f) * Mathf.Clamp01((0.45f - t) / 0.05f);
            s[i] = (Square(370f * t) + Square(465f * t)) * 0.5f * envelope;
        }
        return Lowpass(Lowpass(s, 1600f), 1600f);
    }

    static float[] TowBeep()
    {
        float[] s = Buffer(1.2f);
        for (int beep = 0; beep < 3; beep++)
            MixAt(s, Ping(1150f, 0.16f, 0.5f, harmonics: true), beep * 0.4f, 1f);
        return Lowpass(s, 3000f);
    }

    static float[] ScanDone()
    {
        float[] s = Buffer(0.45f);
        MixAt(s, Ping(880f, 0.2f, 0.06f), 0f, 0.8f);
        MixAt(s, Ping(1175f, 0.3f, 0.1f), 0.12f, 1f);
        return s;
    }

    // Two bright dings: you found something.
    static float[] Found()
    {
        float[] s = Buffer(0.8f);
        MixAt(s, BellTone(0.5f, 1318f, new[] { 1f, 2f, 3.01f }, new[] { 1f, 0.3f, 0.15f }, 0.15f), 0f, 0.8f);
        MixAt(s, BellTone(0.7f, 1760f, new[] { 1f, 2f, 3.01f }, new[] { 1f, 0.3f, 0.15f }, 0.25f), 0.09f, 1f);
        return s;
    }

    // A low buzzer: two tones a little out of tune, so they "wobble" against each other.
    static float[] Wrong()
    {
        float[] s = Buffer(0.45f);
        for (int i = 0; i < s.Length; i++)
        {
            float t = T(i);
            float envelope = Mathf.Clamp01(t / 0.01f) * Mathf.Clamp01((0.45f - t) / 0.06f);
            s[i] = (Square(98f * t) + Square(104f * t)) * 0.5f * envelope;
        }
        return Lowpass(s, 1200f);
    }

    static float[] Door()
    {
        float[] s = Buffer(0.55f);
        MixAt(s, Ping(2500f, 0.03f, 0.004f), 0f, 0.6f);                                  // latch
        MixAt(s, Highpass(NoiseBurst(0.02f, 0.004f), 1500f), 0f, 0.5f);
        float[] thud = Buffer(0.15f);
        for (int i = 0; i < thud.Length; i++)
            thud[i] = Mathf.Sin(TwoPi * 85f * T(i)) * Decay(T(i), 0.05f);
        MixAt(s, Add(thud, Lowpass(NoiseBurst(0.15f, 0.03f), 600f), 0.8f), 0.32f, 1f); // "chunk"
        return s;
    }

    // One footstep on gravel.
    static float[] Step()
    {
        float[] s = Lowpass(NoiseBurst(0.1f, 0.025f), 1300f);
        for (int i = 0; i < s.Length; i++)
            s[i] += Mathf.Sin(TwoPi * 75f * T(i)) * Decay(T(i), 0.02f) * 0.6f;
        for (int crunch = 0; crunch < 4; crunch++)
            MixAt(s, Highpass(NoiseBurst(0.004f, 0.001f), 2000f), (float)random.NextDouble() * 0.06f, 0.4f);
        return s;
    }

    // ---------- Shift and ambience ----------

    // End of the shift: a factory steam whistle.
    static float[] Whistle()
    {
        float[] s = Buffer(1.8f);
        float[] breath = Bandpass(NoiseBuffer(1.8f), 1800f, 1f);
        float phase1 = 0f, phase2 = 0f;
        for (int i = 0; i < s.Length; i++)
        {
            float t = T(i);
            float bend = Mathf.Lerp(0.9f, 1f, Mathf.Clamp01(t / 0.25f));
            phase1 += 392f * bend / Rate;
            phase2 += 494f * bend / Rate;
            float envelope = Mathf.Clamp01(t / 0.12f) * Mathf.Clamp01((1.8f - t) / 0.4f);
            s[i] = ((Saw(phase1) + Saw(phase2)) * 0.4f + breath[i] * 0.6f) * envelope;
        }
        return Lowpass(s, 2000f);
    }

    // Wind that rises, wanders and dies away (a one-shot, never a loop, so it
    // never repeats on a cycle). The "whoosh" comes from moving the filter's cutoff:
    // `strength` 0..1 pushes it higher and makes the wind louder and rougher.
    static float[] Wind(float seconds, float strength)
    {
        float[] noise = NoiseBuffer(seconds);
        float[] s = new float[noise.Length];
        float low = 0f, wander = 0f, wanderTarget = 0f;
        for (int i = 0; i < s.Length; i++)
        {
            float t = T(i);
            if (i % (Rate / 4) == 0) wanderTarget = (float)random.NextDouble();  // a new gusty "mood" 4x a second
            wander += (wanderTarget - wander) * 0.0002f;
            float swell = Mathf.Pow(Mathf.Sin(Mathf.PI * t / seconds), 1.5f);  // rises, then dies away
            float cutoff = 180f + (300f + 900f * strength) * swell * (0.5f + wander);
            low += OnePole(cutoff) * (noise[i] - low);
            s[i] = low * swell * (0.5f + 0.5f * wander);
        }
        return Highpass(s, 90f); // no rumble: wind should hiss and whoosh, not hum
    }

    // A tumbleweed bouncing down the road: a dry crackle that gets busier on
    // every bounce, with a soft thump where it lands. Lasts as long as its roll on screen.
    static float[] Tumbleweed()
    {
        const float seconds = 5f;
        float[] s = Buffer(seconds);
        float bounce = 0.15f;
        while (bounce < seconds - 0.3f)
        {
            MixAt(s, Lowpass(NoiseBurst(0.06f, 0.015f), 300f), bounce, 0.8f);                 // thump
            int twigs = 10 + random.Next(10);
            for (int i = 0; i < twigs; i++)                                                      // crackle
                MixAt(s, Highpass(NoiseBurst(0.004f, 0.0012f), 2500f), bounce + (float)random.NextDouble() * 0.35f, 0.5f);
            bounce += 0.35f + (float)random.NextDouble() * 0.3f;
        }
        float[] scrape = Bandpass(NoiseBuffer(seconds), 3000f, 1.5f);                            // rolling between bounces
        for (int i = 0; i < s.Length; i++)
            s[i] += scrape[i] * 0.08f * Mathf.Sin(Mathf.PI * T(i) / seconds);
        return s;
    }

    // ---------- Music (a sparse guitar loop) ----------

    // A slow ranchera in 3/4 in A minor: bass on beat 1, a strum on 2 and 3,
    // and a simple melody on top. 16 bars, then it loops.
    static float[] Music()
    {
        const float beat = 0.62f;
        const int bars = 16;
        float length = bars * 3 * beat;
        float[] s = Buffer(length + 3f); // the extra is the ringing at the end, folded back to the start

        // Each chord: two bass notes (bar and alternate bar), then three strum notes. MIDI numbers.
        var am = new[] { 45, 40, 57, 60, 64 };
        var e7 = new[] { 40, 47, 59, 62, 68 };
        var dm = new[] { 50, 45, 57, 62, 65 };
        int[][] chords = { am, am, e7, e7, e7, e7, am, am, dm, dm, am, am, e7, e7, am, am };
        int[,] melody =
        {
            { 76, -1, 72 }, { 69, -1, -1 }, { 71, -1, 68 }, { 64, -1, -1 },
            { 74, -1, 71 }, { 68, 69, 71 }, { 72, -1, 69 }, { 69, -1, -1 },
            { 77, -1, 74 }, { 69, -1, 74 }, { 76, -1, 72 }, { 69, -1, -1 },
            { 71, -1, 74 }, { 68, -1, 71 }, { 69, -1, -1 }, { -1, -1, -1 },
        };

        for (int bar = 0; bar < bars; bar++)
        {
            int[] chord = chords[bar];
            float barStart = bar * 3 * beat;
            MixAt(s, Pluck(Midi(chord[bar % 2]), 2.2f, 0.996f, 0.4f), barStart, 0.55f);
            for (int b = 1; b < 3; b++)
                for (int n = 0; n < 3; n++) // a strum: the strings ring one after another
                    MixAt(s, Pluck(Midi(chord[2 + n]), 0.9f, 0.992f, 0.7f), barStart + b * beat + n * 0.014f, 0.22f);
            for (int b = 0; b < 3; b++)
                if (melody[bar, b] > 0)
                    MixAt(s, Pluck(Midi(melody[bar, b]), 1.6f, 0.997f, 0.85f), barStart + b * beat, 0.4f);
        }

        // Fold the tail onto the start, so the last notes keep ringing as the loop restarts.
        int loopLength = Mathf.RoundToInt(length * Rate);
        for (int i = loopLength; i < s.Length; i++)
            s[i - loopLength] += s[i];
        Array.Resize(ref s, loopLength);
        return s;
    }

    // Losing: low strings that don't agree with each other.
    static float[] Lose()
    {
        float[] s = Buffer(3.5f);
        MixAt(s, Pluck(Midi(40), 3f, 0.998f, 0.3f), 0f, 0.8f);
        MixAt(s, Pluck(Midi(46), 3f, 0.998f, 0.3f), 0.6f, 0.7f);
        MixAt(s, Pluck(Midi(33), 3f, 0.999f, 0.25f), 1.3f, 1f);
        return Lowpass(s, 1500f);
    }

    // Winning: an A-major arpeggio, then the whole chord.
    static float[] Win()
    {
        float[] s = Buffer(3f);
        int[] notes = { 57, 61, 64, 69 };
        for (int i = 0; i < notes.Length; i++)
            MixAt(s, Pluck(Midi(notes[i]), 2.5f, 0.997f, 0.8f), i * 0.16f, 0.5f);
        for (int i = 0; i < notes.Length; i++)
            MixAt(s, Pluck(Midi(notes[i]), 2.2f, 0.997f, 0.8f), 0.75f + i * 0.015f, 0.35f);
        return s;
    }

    // ---------- Building blocks ----------

    // A guitar string. `decay` closer to 1 = rings longer. `brightness` 0..1.
    static float[] Pluck(float frequency, float seconds, float decay, float brightness)
    {
        float[] s = Buffer(seconds);
        int period = Mathf.Max(2, Mathf.RoundToInt(Rate / frequency));
        var loop = new float[period];
        float smooth = 0f;
        for (int i = 0; i < period; i++)
        {
            smooth += Mathf.Lerp(0.2f, 1f, brightness) * (Noise() - smooth); // softer pick = darker noise
            loop[i] = smooth;
        }

        for (int i = 0; i < s.Length; i++)
        {
            int here = i % period;
            int next = (i + 1) % period;
            s[i] = loop[here];
            loop[here] = (loop[here] + loop[next]) * 0.5f * decay;
        }
        return s;
    }

    // A bell: several tones (partials) at once, the high ones dying faster.
    static float[] BellTone(float seconds, float frequency, float[] ratios, float[] amps, float decay)
    {
        float[] s = Buffer(seconds);
        for (int i = 0; i < s.Length; i++)
        {
            float t = T(i);
            for (int p = 0; p < ratios.Length; p++)
                s[i] += Mathf.Sin(TwoPi * frequency * ratios[p] * t) * amps[p] * Decay(t, decay / (p + 1));
        }
        return s;
    }

    static float[] Ping(float frequency, float seconds, float decay, bool harmonics = false)
    {
        float[] s = Buffer(seconds);
        for (int i = 0; i < s.Length; i++)
        {
            float t = T(i);
            float tone = Mathf.Sin(TwoPi * frequency * t) + (harmonics ? Mathf.Sin(TwoPi * frequency * 3f * t) * 0.3f : 0f);
            s[i] = tone * Mathf.Clamp01(t / 0.003f) * Decay(t, decay);
        }
        return s;
    }

    static float[] NoiseBurst(float seconds, float decay)
    {
        float[] s = Buffer(seconds);
        for (int i = 0; i < s.Length; i++)
            s[i] = Noise() * Decay(T(i), decay);
        return s;
    }

    static float[] NoiseBuffer(float seconds)
    {
        float[] s = Buffer(seconds);
        for (int i = 0; i < s.Length; i++)
            s[i] = Noise();
        return s;
    }

    static float[] Lowpass(float[] input, float cutoff)
    {
        float[] s = new float[input.Length];
        float a = OnePole(cutoff), y = 0f;
        for (int i = 0; i < s.Length; i++)
            s[i] = y += a * (input[i] - y);
        return s;
    }

    static float[] Highpass(float[] input, float cutoff)
    {
        float[] low = Lowpass(input, cutoff);
        float[] s = new float[input.Length];
        for (int i = 0; i < s.Length; i++)
            s[i] = input[i] - low[i];
        return s;
    }

    static float[] Bandpass(float[] input, float center, float q)
    {
        var filter = new Biquad();
        filter.SetBandpass(center, q);
        float[] s = new float[input.Length];
        for (int i = 0; i < s.Length; i++)
            s[i] = filter.Process(input[i]);
        return s;
    }

    static float[] Add(float[] a, float[] b, float bVolume)
    {
        MixAt(a, b, 0f, bVolume);
        return a;
    }

    static void MixAt(float[] into, float[] sound, float atSeconds, float volume)
    {
        int start = Mathf.RoundToInt(atSeconds * Rate);
        for (int i = 0; i < sound.Length && start + i < into.Length; i++)
            if (start + i >= 0)
                into[start + i] += sound[i] * volume;
    }

    static float[] Buffer(float seconds) => new float[Mathf.CeilToInt(seconds * Rate)];
    static float T(int sample) => (float)sample / Rate;
    static float Noise() => (float)random.NextDouble() * 2f - 1f;
    static float Decay(float t, float time) => Mathf.Exp(-t / time);
    static float Square(float cycles) => (cycles % 1f) < 0.5f ? 1f : -1f;
    static float Saw(float cycles) => (cycles % 1f) * 2f - 1f;
    static float Midi(int note) => 440f * Mathf.Pow(2f, (note - 69) / 12f);
    static float OnePole(float cutoff) => 1f - Mathf.Exp(-TwoPi * cutoff / Rate);

    // A standard "biquad" band filter (from the Audio EQ Cookbook). Keeps the frequencies
    // near `center`; higher q = narrower slice.
    class Biquad
    {
        float b0, b2, a1, a2;
        float x1, x2, y1, y2;

        public void SetBandpass(float center, float q)
        {
            float w = TwoPi * center / Rate;
            float alpha = Mathf.Sin(w) / (2f * q);
            float a0 = 1f + alpha;
            b0 = alpha / a0;
            b2 = -alpha / a0;
            a1 = -2f * Mathf.Cos(w) / a0;
            a2 = (1f - alpha) / a0;
        }

        public float Process(float x)
        {
            float y = b0 * x + b2 * x2 - a1 * y1 - a2 * y2;
            x2 = x1; x1 = x;
            y2 = y1; y1 = y;
            return y;
        }
    }

    // ---------- Saving ----------

    // Writes every sound into `folder`. Plain file writing, no editor calls, so it
    // also works outside Unity. Busy sounds get several "takes" (Stamp, Stamp_2,
    // Stamp_3...): fresh noise, slightly different pitch. Sfx picks one at random.
    public static void WriteAll(string folder)
    {
        Directory.CreateDirectory(folder);
        random = new System.Random(1910); // same seed = same sounds every time

        Save(folder, Sound.Click, 3, Click, 0.5f);
        Save(folder, Sound.Stamp, 3, Stamp, 0.95f);
        Save(folder, Sound.Paper, 3, () => Paper(0.35f, 2600f), 0.45f);
        Save(folder, Sound.Rustle, 2, Rustle, 0.5f);
        Save(folder, Sound.Megaphone, 2, Megaphone, 0.9f);
        Save(folder, Sound.Door, 2, Door, 0.8f);
        Save(folder, Sound.Step, 4, Step, 0.5f);
        Save(folder, Sound.KeyChime, 2, KeyChime, 0.4f);
        Save(folder, Sound.EngineStart, 3, EngineStart, 0.6f);
        Save(folder, Sound.CarPass, 3, CarPass, 0.7f);
        Save(folder, Sound.Brake, 2, Brake, 0.45f);
        Save(folder, Sound.Horn, 3, Horn, 0.4f);
        Save(folder, Sound.TowBeep, 1, TowBeep, 0.45f);
        Save(folder, Sound.ScanSweep, 1, ScanSweep, 0.3f);
        Save(folder, Sound.ScanDone, 1, ScanDone, 0.55f);
        Save(folder, Sound.Found, 2, Found, 0.7f);
        Save(folder, Sound.Wrong, 2, Wrong, 0.4f);
        Save(folder, Sound.Boss, 4, Boss, 0.75f);
        Save(folder, Sound.Printer, 2, Printer, 0.5f);
        Save(folder, Sound.Cash, 2, Cash, 0.7f);
        Save(folder, Sound.Type, 4, TypeKey, 0.45f);
        Save(folder, Sound.Bell, 1, Bell, 0.6f);
        Save(folder, Sound.Blip, 3, Blip, 0.5f);
        Save(folder, Sound.Tick, 2, Tick, 0.35f);
        Save(folder, Sound.Whistle, 1, Whistle, 0.7f);
        Save(folder, Sound.Breeze, 3, () => Wind(7f + (float)random.NextDouble() * 4f, 0.2f), 0.35f);
        Save(folder, Sound.Gust, 3, () => Wind(2.5f + (float)random.NextDouble(), 1f), 0.5f);
        Save(folder, Sound.Tumbleweed, 2, Tumbleweed, 0.65f);
        Save(folder, Sound.Music, 1, Music, 0.6f, loop: true);
        Save(folder, Sound.Lose, 1, Lose, 0.7f);
        Save(folder, Sound.Win, 1, Win, 0.7f);
        Save(folder, Sound.Ring, 1, Ring, 0.5f); // new sounds go LAST, so the ones above come out the same
    }

    static readonly float[] TakePitch = { 1f, 0.93f, 1.07f, 0.97f, 1.04f };

    static void Save(string folder, Sound name, int takes, Func<float[]> make, float peak, bool loop = false)
    {
        for (int take = 0; take < takes; take++)
        {
            float[] samples = make();
            if (take > 0) samples = Resample(samples, TakePitch[take % TakePitch.Length]);
            string file = take == 0 ? $"{name}.wav" : $"{name}_{take + 1}.wav";
            WriteWav(Path.Combine(folder, file), samples, peak, loop);
        }
    }

    // Plays the sound faster (higher, shorter) or slower (lower, longer).
    static float[] Resample(float[] input, float speed)
    {
        float[] s = new float[Mathf.FloorToInt((input.Length - 1) / speed)];
        for (int i = 0; i < s.Length; i++)
        {
            float position = i * speed;
            int index = (int)position;
            s[i] = Mathf.Lerp(input[index], input[index + 1], position - index);
        }
        return s;
    }

    // Scales the sound so its loudest moment hits `peak` and writes a 16-bit WAV.
    static void WriteWav(string path, float[] samples, float peak, bool loop)
    {
        float max = 0.0001f;
        foreach (float v in samples) max = Mathf.Max(max, Mathf.Abs(v));
        float gain = peak / max;

        // A 2 ms fade at both ends prevents clicks (not for loops, which must join up).
        int edge = loop ? 0 : Mathf.Min(samples.Length / 4, Rate / 500);

        using (var writer = new BinaryWriter(File.Create(path)))
        {
            int dataBytes = samples.Length * 2;
            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + dataBytes);
            writer.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            writer.Write(16);           // format chunk size
            writer.Write((short)1);     // PCM
            writer.Write((short)1);     // mono
            writer.Write(Rate);
            writer.Write(Rate * 2);     // bytes per second
            writer.Write((short)2);     // bytes per sample
            writer.Write((short)16);    // bits per sample
            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(dataBytes);

            for (int i = 0; i < samples.Length; i++)
            {
                float fade = edge == 0 ? 1f : Mathf.Clamp01(Mathf.Min(i, samples.Length - 1 - i) / (float)edge);
                writer.Write((short)(Mathf.Clamp(samples[i] * gain * fade, -1f, 1f) * short.MaxValue));
            }
        }
    }
}

// Import options for every file in Assets/Resources/Sounds, however it got there:
// long sounds (music) stay compressed in memory, short ones load ready to play.
public class SoundImportSettings : AssetPostprocessor
{
    void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith(SoundGenerator.Folder)) return;

        var importer = (AudioImporter)assetImporter;
        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        bool longSound = assetPath.Contains("Music");
        settings.loadType = longSound ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = 0.7f;
        importer.defaultSampleSettings = settings;
        importer.forceToMono = true;
    }
}
