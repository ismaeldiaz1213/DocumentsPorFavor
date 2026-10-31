using System.Collections;
using UnityEngine;

// A tumbleweed that bounces across the road once, then disappears.
// It draws its own little pixel sprite, so there's nothing to set up:
//     Tumbleweed.Roll(fromPosition, toPosition);
public class Tumbleweed : MonoBehaviour
{
    const int PixelsPerUnit = 18;       // same scale as the rest of the art
    const float Seconds = 5f;           // matches the length of the Tumbleweed sound
    static readonly Color Twig = new Color32(0x9C, 0x7E, 0x52, 0xFF);
    static readonly Color DarkTwig = new Color32(0x6E, 0x58, 0x3A, 0xFF);

    static Sprite sprite;

    public static void Roll(Vector3 from, Vector3 to)
    {
        var go = new GameObject("Tumbleweed");
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite != null ? sprite : sprite = Draw();
        renderer.sortingOrder = -1; // in front of the backdrop and booth, behind the cars
        go.AddComponent<Tumbleweed>().StartCoroutine(RollRoutine(go.transform, from, to));
        Sfx.Play(Sound.Tumbleweed, 0.8f);
    }

    static IEnumerator RollRoutine(Transform weed, Vector3 from, Vector3 to)
    {
        float spin = from.x < to.x ? -1f : 1f; // rolls the way it travels
        float elapsed = 0f;
        float hopPhase = 0f;
        while (elapsed < Seconds)
        {
            elapsed += Time.deltaTime;
            hopPhase += Time.deltaTime * Random.Range(1.6f, 2.4f); // uneven hops look windblown
            Vector3 ground = Vector3.Lerp(from, to, elapsed / Seconds);
            float hop = Mathf.Abs(Mathf.Sin(hopPhase * Mathf.PI)) * 0.45f;
            weed.position = ground + Vector3.up * hop;
            weed.Rotate(0f, 0f, spin * 360f * Time.deltaTime);
            yield return null;
        }
        Destroy(weed.gameObject);
    }

    // A scribbly ball of twigs: random little lines inside a circle.
    static Sprite Draw()
    {
        const int size = 14;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        var pixels = new Color[size * size];
        var random = new System.Random(7);
        float center = (size - 1) / 2f;

        for (int line = 0; line < 22; line++)
        {
            float angle = (float)random.NextDouble() * Mathf.PI * 2f;
            float radius = 2f + (float)random.NextDouble() * 4.5f;
            Color color = random.Next(3) == 0 ? DarkTwig : Twig;
            for (float a = 0f; a < 1.4f; a += 0.12f) // a short curved twig
            {
                int x = Mathf.RoundToInt(center + Mathf.Cos(angle + a) * radius);
                int y = Mathf.RoundToInt(center + Mathf.Sin(angle + a) * radius);
                if (x >= 0 && x < size && y >= 0 && y < size)
                    pixels[y * size + x] = color;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }
}
