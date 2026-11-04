using UnityEngine;
using UnityEngine.Rendering.Universal;

// The day going by over a shift: the sky shifts from dawn pink to noon blue to an
// orange sunset, the sun climbs out from behind the hills and sinks back down at
// 17:00, and the Global Light 2D tints the whole world to match.
//     DayCycle cycle = DayCycle.Create();
//     cycle.Set(0f);   07:00, sunrise
//     cycle.Set(1f);   17:00, sunset (keeps going into dusk past 1)
// The painted backdrop (ArtBackdrop) has a transparent sky; this draws behind it.
public class DayCycle : MonoBehaviour
{
    // One moment of the day: where k is 0 at 07:00 and 1 at 17:00.
    struct Moment
    {
        public float k;
        public Color skyTop, skyHorizon, light;
        public float intensity;
        public Moment(float k, string top, string horizon, string light, float intensity)
        {
            this.k = k;
            skyTop = Hex(top);
            skyHorizon = Hex(horizon);
            this.light = Hex(light);
            this.intensity = intensity;
        }
    }

    static readonly Moment[] Day =
    {
        new Moment(0.00f, "56608F", "E8A48A", "FFD2BE", 0.80f), // sunrise
        new Moment(0.15f, "7F97B5", "EDC9A0", "FFEBD8", 0.92f),
        new Moment(0.45f, "8FA5B5", "E6D3A3", "FFFFFF", 1.00f), // midday: the backdrop's own colors
        new Moment(0.75f, "8A9AAE", "EFCB8E", "FFF1DC", 0.97f),
        new Moment(0.92f, "6E6A8E", "F0A066", "FFD0A0", 0.88f),
        new Moment(1.00f, "4E4677", "EE8455", "FFB890", 0.80f), // sunset, lane closes
        new Moment(1.25f, "272540", "8A4E58", "9C94C8", 0.62f), // dusk
    };

    const int Horizon = 52;       // the backdrop's horizon row, from the bottom
    const int SkyRows = 38;       // rows of sky above it
    const float PixelsPerUnit = 18f;

    SpriteRenderer sky;
    SpriteRenderer sun;
    Texture2D skyTexture;
    Light2D globalLight;
    Bounds backdrop;
    float lastK = float.NaN;
    Color lastTop, lastHorizon;

    public static DayCycle Create()
    {
        GameObject art = GameObject.Find("ArtBackdrop");
        SpriteRenderer backdropRenderer = art != null ? art.GetComponent<SpriteRenderer>() : null;
        if (backdropRenderer == null)
        {
            Debug.LogWarning("DayCycle: no 'ArtBackdrop' in the scene, so no sunrise. Run Generate And Apply Art.");
            return null;
        }

        var go = new GameObject("DayCycle");
        go.transform.SetParent(art.transform.parent, false);
        DayCycle cycle = go.AddComponent<DayCycle>();
        cycle.Build(backdropRenderer);
        cycle.Set(0f);
        return cycle;
    }

    void Build(SpriteRenderer backdropRenderer)
    {
        backdrop = backdropRenderer.bounds;
        int width = Mathf.RoundToInt(backdrop.size.x * PixelsPerUnit);
        Material unlit = UnlitMaterial();

        skyTexture = new Texture2D(width, SkyRows, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        sky = MakeRenderer("Sky", Sprite.Create(skyTexture, new Rect(0, 0, width, SkyRows), new Vector2(0.5f, 0f), PixelsPerUnit),
            backdropRenderer.sortingOrder - 10, unlit);
        sky.transform.position = new Vector3(backdrop.center.x, backdrop.min.y + Horizon / PixelsPerUnit - 1f / PixelsPerUnit, backdrop.center.z);

        // Between the sky and the painted hills, so it rises from behind them.
        sun = MakeRenderer("Sun", SunSprite(), backdropRenderer.sortingOrder - 5, unlit);

        foreach (Light2D light in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
            if (light.lightType == Light2D.LightType.Global) globalLight = light;
    }

    SpriteRenderer MakeRenderer(string name, Sprite sprite, int order, Material material)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        if (material != null) renderer.sharedMaterial = material; // the sky has its own colors; don't let the light tint it twice
        return renderer;
    }

    static Material UnlitMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        return shader != null ? new Material(shader) : null;
    }

    // ---------- Over the day ----------

    public void Set(float k)
    {
        k = Mathf.Clamp(k, 0f, Day[Day.Length - 1].k);
        if (Mathf.Abs(k - lastK) < 0.002f) return;
        lastK = k;

        Moment m = Evaluate(k);
        if (globalLight != null)
        {
            globalLight.color = m.light;
            globalLight.intensity = m.intensity;
        }
        // Repainting the dithered sky is the only costly part, so only when the colors really changed.
        if (ColorDistance(m.skyTop, lastTop) + ColorDistance(m.skyHorizon, lastHorizon) > 0.01f)
        {
            PaintSky(m.skyTop, m.skyHorizon);
            lastTop = m.skyTop;
            lastHorizon = m.skyHorizon;
        }
        MoveSun(k);
    }

    // Across the camera's view from left to right, up in an arc, and back down behind the hills.
    void MoveSun(float k)
    {
        Camera cam = Camera.main;
        float left = backdrop.min.x, right = backdrop.max.x;
        if (cam != null && cam.orthographic)
        {
            float half = cam.orthographicSize * cam.aspect;
            left = Mathf.Max(left, cam.transform.position.x - half * 0.85f);
            right = Mathf.Min(right, cam.transform.position.x + half * 0.85f);
        }

        float horizon = backdrop.min.y + Horizon / PixelsPerUnit;
        float peak = backdrop.max.y - 0.6f;
        float x = Mathf.Lerp(left, right, Mathf.Clamp01(k));
        float height = k <= 1f ? Mathf.Sin(Mathf.PI * Mathf.Lerp(0.04f, 1f, k)) : -0.4f * (k - 1f) * 4f;
        float y = Mathf.Lerp(horizon - 0.2f, peak, Mathf.Max(height, 0f)) + Mathf.Min(height, 0f);
        sun.transform.position = new Vector3(x, y, backdrop.center.z);

        // Pale at midday, deep orange near the horizon.
        float low = 1f - Mathf.Clamp01(height);
        sun.color = Color.Lerp(Hex("FFF4D6"), Hex("FF9A4A"), low * low);
    }

    void PaintSky(Color top, Color horizon)
    {
        int width = skyTexture.width;
        var pixels = new Color[width * SkyRows];
        for (int y = 0; y < SkyRows; y++)
            for (int x = 0; x < width; x++)
                pixels[y * width + x] = Dither(horizon, top, y / (float)(SkyRows - 1), x, y, 6);
        skyTexture.SetPixels(pixels);
        skyTexture.Apply();
    }

    static Moment Evaluate(float k)
    {
        for (int i = 1; i < Day.Length; i++)
        {
            if (k > Day[i].k) continue;
            Moment a = Day[i - 1], b = Day[i];
            float t = Mathf.InverseLerp(a.k, b.k, k);
            return new Moment
            {
                k = k,
                skyTop = Color.Lerp(a.skyTop, b.skyTop, t),
                skyHorizon = Color.Lerp(a.skyHorizon, b.skyHorizon, t),
                light = Color.Lerp(a.light, b.light, t),
                intensity = Mathf.Lerp(a.intensity, b.intensity, t),
            };
        }
        return Day[Day.Length - 1];
    }

    // ---------- Pixels ----------

    // The same banded, dithered look as the painted backdrop.
    static Color Dither(Color from, Color to, float t, int x, int y, int bands)
    {
        float scaled = Mathf.Clamp01(t) * (bands - 1);
        int band = Mathf.FloorToInt(scaled);
        if (scaled - band > Bayer(x, y)) band++;
        return Color.Lerp(from, to, Mathf.Min(band, bands - 1) / (float)(bands - 1));
    }

    static float Bayer(int x, int y)
    {
        int[] m = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
        return (m[(y & 3) * 4 + (x & 3)] + 0.5f) / 16f;
    }

    // A low, hazy sun: a solid disc with a dithered glow. White, so it can be tinted.
    static Sprite SunSprite()
    {
        const int size = 24;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f));
                bool on = d <= 6f || (d <= 11f && Bayer(x, y) < 0.5f * (1f - (d - 6f) / 5f));
                pixels[y * size + x] = on ? Color.white : Color.clear;
            }
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    static float ColorDistance(Color a, Color b) => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color color);
        return color;
    }
}
