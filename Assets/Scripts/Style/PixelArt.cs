using System.Collections.Generic;
using UnityEngine;

// Little pixel-art pictures drawn in code for the intro cutscene, the rulebook and
// the verdict stamp, so they need no image files. Each one is drawn once and kept.
//     PixelArt.Flag(frame)   a waving US flag (frames 0..FlagFrames-1)
//     PixelArt.Badge()       the inspector's badge
//     PixelArt.Booth()       the checkpoint with a line of cars
//     PixelArt.Passport()    a green passport, closed
//     PixelArt.Record()      a clipboard with a citation on it
//     PixelArt.Memo()        a stack of memos
//     PixelArt.Home()        a little house at night, porch light on
//     PixelArt.Check() / PixelArt.Cross()   the verdict marks
public static class PixelArt
{
    public const int FlagFrames = 8;

    static readonly Color Red = Hex("B5473A");
    static readonly Color White = Hex("EFE8D8");
    static readonly Color Blue = Hex("2F3F66");
    static readonly Color Gold = Hex("E8C25A");
    static readonly Color GoldDark = Hex("B08A35");
    static readonly Color Night = Hex("1F2333");
    static readonly Color Clear = new Color(0, 0, 0, 0);

    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    // ---------- The pictures ----------

    public static Sprite Flag(int frame) => Cached("flag" + frame, () =>
    {
        const int w = 76, h = 48, flagW = 66, flagH = 39;
        var c = new Canvas(w, h);
        float phase = frame / (float)FlagFrames * Mathf.PI * 2f;
        // pole
        c.Rect(2, 0, 2, h, Palette.Metal);
        c.Rect(1, h - 3, 4, 3, Gold);
        for (int x = 0; x < flagW; x++)
        {
            // the cloth ripples more the farther it is from the pole
            float wave = Mathf.Sin(x * 0.16f - phase) * (1f + x * 0.035f);
            int dy = Mathf.RoundToInt(wave);
            float shade = 1f + Mathf.Cos(x * 0.16f - phase) * 0.09f;
            for (int y = 0; y < flagH; y++)
            {
                int stripe = y * 13 / flagH; // 0 = bottom stripe
                Color color = stripe % 2 == 0 ? Red : White;
                bool canton = x < flagW * 0.4f && stripe >= 6;
                if (canton)
                {
                    color = Blue;
                    int sx = x - 2, sy = y - 21;
                    if (sx >= 0 && sy >= 0 && sx % 5 == 2 && sy % 4 == ((sx / 5) % 2 == 0 ? 1 : 3)) color = White; // stars
                }
                c.Set(4 + x, h - 3 - flagH + y + dy, Shade(color, shade));
            }
        }
        return c;
    });

    public static Sprite Badge() => Cached("badge", () =>
    {
        const int w = 48, h = 56;
        var c = new Canvas(w, h);
        // shield: flat top with notches, curving down to a point
        for (int y = 0; y < h; y++)
        {
            float t = y / (float)(h - 1); // 0 bottom, 1 top
            float half = t > 0.42f ? 21f : Mathf.Lerp(1f, 21f, Mathf.Sqrt(t / 0.42f));
            if (t > 0.9f) half -= (t - 0.9f) * 40f * (1f - Mathf.Abs(Mathf.Sin(t * 30f)) * 0.3f);
            for (int x = 0; x < w; x++)
            {
                float d = Mathf.Abs(x - (w - 1) / 2f);
                if (d > half) continue;
                bool edge = d > half - 2.2f;
                c.Set(x, y, edge ? GoldDark : Color.Lerp(Gold, White, (x + y) % 9 == 0 ? 0.35f : 0f));
            }
        }
        c.Star(w / 2f - 0.5f, h * 0.52f, 13f, 5.5f, Blue);
        c.Star(w / 2f - 0.5f, h * 0.52f, 7f, 3f, White);
        c.Rect(10, 8, 28, 5, Blue); // the name plate
        return c;
    });

    public static Sprite Booth() => Cached("booth", () =>
    {
        const int w = 120, h = 64;
        var c = new Canvas(w, h);
        for (int y = 0; y < h; y++)
            c.Rect(0, y, w, 1, Color.Lerp(Palette.SkyHorizon, Palette.SkyTop, y / (float)h));
        // mountains
        for (int x = 0; x < w; x++)
        {
            int top = 30 + Mathf.RoundToInt(Mathf.Sin(x * 0.09f) * 4f + Mathf.Sin(x * 0.23f) * 2f);
            c.Rect(x, 20, 1, top - 20, Palette.FarMountains);
        }
        c.Rect(0, 0, w, 22, Palette.Sand);
        c.Rect(0, 6, w, 10, Palette.Road);
        for (int x = 2; x < w; x += 10) c.Rect(x, 10, 5, 1, Palette.RoadLine);
        // canopy over the lanes
        c.Rect(60, 34, 58, 4, Palette.Concrete);
        c.Rect(62, 16, 3, 18, Palette.ConcreteShadow);
        c.Rect(112, 16, 3, 18, Palette.ConcreteShadow);
        c.Rect(70, 39, 22, 4, Blue); // sign
        c.Rect(73, 40, 16, 2, White);
        // booth 3
        c.Rect(80, 15, 16, 18, Palette.Concrete);
        c.Rect(83, 22, 10, 7, Palette.Glass);
        c.Rect(80, 32, 16, 2, Palette.Metal);
        // a little flag on the canopy
        c.Rect(100, 38, 1, 10, Palette.Metal);
        c.Rect(101, 43, 7, 5, Red);
        c.Rect(101, 44, 7, 1, White);
        c.Rect(101, 46, 7, 1, White);
        c.Rect(101, 45, 3, 3, Blue);
        // the line of cars coming from the left
        Color[] cars = { Hex("A8503F"), Hex("6E8B57"), Hex("4D6178"), Hex("C99A3E"), Hex("7E99A6") };
        for (int i = 0; i < cars.Length; i++)
            Car(c, 66 - i * 15, 9, cars[i]);
        return c;
    });

    // The title screen: the port of entry at sunset, a line of cars waiting, the fence running off into the hills.
    public static Sprite TitleBackdrop() => Cached("title", () =>
    {
        const int w = 192, h = 108;
        var c = new Canvas(w, h);
        Color top = Hex("2E2747"), mid = Hex("8C4A5A"), low = Hex("E0874F"), glow = Hex("F4C070");
        for (int y = 0; y < h; y++)
        {
            float k = y / (float)h;
            Color sky = k < 0.45f ? Color.Lerp(glow, low, k / 0.45f)
                      : k < 0.7f ? Color.Lerp(low, mid, (k - 0.45f) / 0.25f)
                      : Color.Lerp(mid, top, (k - 0.7f) / 0.3f);
            c.Rect(0, y, w, 1, sky);
        }
        // a few early stars
        foreach (Vector2Int s in new[] { new Vector2Int(14, 100), new Vector2Int(40, 94), new Vector2Int(77, 104), new Vector2Int(130, 98), new Vector2Int(170, 102), new Vector2Int(103, 92) })
            c.Set(s.x, s.y, Palette.WithAlpha(White, 0.8f));
        // the sun, half down behind the hills
        c.Disc(120, 44, 13f, Hex("FBE3A0"));
        c.Disc(120, 44, 10f, Hex("FFF1C8"));
        // far hills, then mesas, in silhouette
        for (int x = 0; x < w; x++)
        {
            int far = 44 + Mathf.RoundToInt(Mathf.Sin(x * 0.05f) * 5f + Mathf.Sin(x * 0.17f + 1f) * 2f);
            c.Rect(x, 30, 1, far - 30, Hex("7A4A5E"));
            int mesa = x < 40 ? 50 : x < 52 ? 50 - (x - 40) : 36 + Mathf.RoundToInt(Mathf.Sin(x * 0.11f) * 2f);
            if (x > 150 && x < 178) mesa = 47;
            if (x >= 178) mesa = 47 - (x - 178);
            c.Rect(x, 30, 1, Mathf.Max(0, mesa - 30), Hex("553447"));
        }
        // the desert floor
        for (int y = 0; y < 31; y++)
            c.Rect(0, y, w, 1, Color.Lerp(Hex("6E4A44"), Hex("A0674E"), y / 31f));
        // the border fence, posts marching off to the right
        c.Rect(0, 33, w, 1, Hex("3A2A35"));
        for (int x = 2; x < w; x += 4) c.Rect(x, 30, 1, 6, Hex("3A2A35"));
        // the road
        c.Rect(0, 8, w, 14, Hex("5A4448"));
        for (int x = 3; x < w; x += 12) c.Rect(x, 14, 6, 1, Hex("E6B87A"));
        // the canopy and booths
        c.Rect(98, 46, 80, 5, Hex("3E3340"));
        c.Rect(98, 51, 80, 1, Hex("F4C070"));
        for (int x = 102; x <= 172; x += 35) c.Rect(x, 21, 3, 25, Hex("3E3340"));
        c.Rect(116, 20, 18, 20, Hex("4A3E48"));
        c.Rect(119, 28, 12, 8, Hex("F0D080")); // the lit window of Booth 3
        c.Rect(124, 28, 1, 8, Hex("4A3E48"));
        c.Rect(118, 40, 14, 2, Hex("3E3340"));
        // sign on the canopy
        c.Rect(108, 52, 30, 7, Hex("2F3F66"));
        c.Rect(111, 54, 24, 2, Hex("EFE8D8"));
        // the line of cars, headlights on
        Color[] cars = { Hex("7A3A34"), Hex("4E5D44"), Hex("3A4A60"), Hex("8A6A30"), Hex("5A6670"), Hex("6A3A50") };
        for (int i = 0; i < cars.Length; i++)
        {
            int x = 98 - i * 17;
            Car(c, x, 12, cars[i]);
            c.Set(x + 13, 13, Hex("FFF1C8"));
            c.Set(x + 14, 13, Palette.WithAlpha(Hex("FFF1C8"), 0.5f));
        }
        return c;
    });

    public static Sprite Passport() => Cached("passport", () =>
    {
        const int w = 40, h = 52;
        var c = new Canvas(w, h);
        c.Rect(1, 0, w - 1, h - 1, Palette.WithAlpha(Palette.Ink, 0.5f)); // shadow
        c.Rect(0, 1, w - 1, h - 1, Palette.PassportGreen);
        c.Rect(0, 1, 3, h - 1, Shade(Palette.PassportGreen, 0.8f)); // spine
        c.Ring(w / 2f, h * 0.58f, 9f, 2f, Gold);
        c.Star(w / 2f, h * 0.58f, 5f, 2.2f, Gold);
        c.Rect(10, 12, 20, 2, Gold);
        c.Rect(13, 8, 14, 1, Gold);
        return c;
    });

    public static Sprite Record() => Cached("record", () =>
    {
        const int w = 44, h = 56;
        var c = new Canvas(w, h);
        c.Rect(2, 0, 40, 54, Palette.DeskWood);
        c.Rect(5, 3, 34, 47, Palette.Paper);
        c.Rect(15, 50, 14, 5, Palette.Metal); // clip
        for (int y = 40; y > 8; y -= 5)
            c.Rect(8, y, y % 10 == 0 ? 22 : 26, 1, Palette.PaperShadow);
        // red tally of citations
        c.Rect(8, 12, 28, 9, Palette.WithAlpha(Red, 0.9f));
        c.Rect(10, 16, 4, 1, White);
        c.Rect(17, 14, 1, 5, White);
        c.Rect(21, 14, 1, 5, White);
        c.Rect(25, 14, 1, 5, White);
        return c;
    });

    public static Sprite Memo() => Cached("memo", () =>
    {
        const int w = 52, h = 52;
        var c = new Canvas(w, h);
        c.Rect(10, 2, 38, 44, Palette.PaperShadow);
        c.Rect(7, 4, 38, 44, Hex("DCCFB0"));
        c.Rect(4, 7, 38, 44, Palette.Paper);
        c.Rect(4, 43, 38, 8, Palette.Reject); // the "MEMO" header bar
        c.Rect(9, 46, 14, 2, Palette.Paper);
        for (int y = 36; y > 12; y -= 4)
            c.Rect(8, y, y == 20 ? 18 : 30, 1, Shade(Palette.PaperInk, 0.8f));
        c.Rect(28, 10, 10, 6, Palette.WithAlpha(Palette.Reject, 0.7f)); // stamp
        return c;
    });

    public static Sprite Home() => Cached("home", () =>
    {
        const int w = 96, h = 56;
        var c = new Canvas(w, h);
        for (int y = 0; y < h; y++)
            c.Rect(0, y, w, 1, Color.Lerp(Hex("3A3550"), Night, y / (float)h));
        foreach (Vector2Int star in new[] { new Vector2Int(8, 48), new Vector2Int(20, 52), new Vector2Int(70, 50), new Vector2Int(88, 44), new Vector2Int(55, 53) })
            c.Set(star.x, star.y, White);
        c.Ring(80, 46, 4f, 4f, Hex("E6D3A3")); // moon
        c.Rect(0, 0, w, 10, Hex("4A3F3A"));
        // the house
        c.Rect(28, 10, 40, 22, Hex("8F7A66"));
        for (int i = 0; i < 12; i++) c.Rect(26 + i * 1, 32 + i, 44 - i * 2, 1, Hex("5A4636"));
        c.Rect(36, 18, 8, 7, Hex("F0D080")); // lit window
        c.Rect(39, 18, 1, 7, Hex("5A4636"));
        c.Rect(36, 21, 8, 1, Hex("5A4636"));
        c.Rect(52, 10, 8, 14, Hex("4A3530")); // door
        c.Rect(54, 25, 4, 2, Hex("FFE9A0")); // porch light
        c.Ring(56, 26, 5f, 2f, Palette.WithAlpha(Hex("FFE9A0"), 0.25f));
        // a cactus
        c.Rect(12, 10, 3, 12, Palette.CactusDark);
        c.Rect(9, 14, 3, 1, Palette.CactusDark);
        c.Rect(9, 14, 1, 4, Palette.CactusDark);
        return c;
    });

    // Small icons for the rulebook's HOW TO page.
    public static Sprite MegaphoneIcon() => Cached("megaphone", () =>
    {
        var c = new Canvas(24, 24);
        c.Disc(12, 12, 11f, Gold);
        c.Rect(5, 9, 4, 6, Palette.Ink);
        for (int i = 0; i < 7; i++) c.Rect(9 + i, 9 - i / 2, 1, 6 + i, Palette.Ink);
        c.Rect(8, 5, 2, 4, Palette.Ink);
        c.Rect(18, 11, 1, 2, Palette.Ink);
        c.Rect(20, 9, 1, 6, Palette.Ink);
        return c;
    });

    public static Sprite SpeechIcon() => Cached("speech", () =>
    {
        var c = new Canvas(24, 24);
        c.Rect(2, 8, 20, 13, Palette.Speech);
        c.Rect(5, 5, 4, 3, Palette.Speech);
        c.Rect(4, 3, 2, 2, Palette.Speech);
        c.Rect(5, 16, 14, 2, Palette.Paper);
        c.Rect(5, 12, 10, 2, Palette.Paper);
        return c;
    });

    public static Sprite BookIcon() => Cached("book", () =>
    {
        var c = new Canvas(24, 24);
        c.Rect(3, 2, 18, 20, Palette.PassportGreen);
        c.Rect(3, 2, 3, 20, Shade(Palette.PassportGreen, 0.75f));
        c.Rect(19, 3, 2, 18, Palette.Paper);
        c.Rect(9, 14, 8, 2, Gold);
        c.Rect(9, 10, 8, 1, Gold);
        return c;
    });

    public static Sprite CuffsIcon() => Cached("cuffs", () =>
    {
        var c = new Canvas(24, 24);
        c.Ring(7, 11, 6f, 2f, Palette.Metal);
        c.Ring(17, 11, 6f, 2f, Palette.Metal);
        c.Rect(11, 15, 2, 2, Palette.Metal);
        return c;
    });

    public static Sprite Check() => Cached("check", () =>
    {
        var c = new Canvas(48, 48);
        c.Ring(24, 24, 22f, 3.5f, Palette.Approve);
        c.Line(12, 25, 20, 15, 4f, Palette.Approve);
        c.Line(20, 15, 37, 34, 4f, Palette.Approve);
        return c;
    });

    public static Sprite Cross() => Cached("cross", () =>
    {
        var c = new Canvas(48, 48);
        c.Ring(24, 24, 22f, 3.5f, Palette.Reject);
        c.Line(14, 14, 34, 34, 4f, Palette.Reject);
        c.Line(14, 34, 34, 14, 4f, Palette.Reject);
        return c;
    });

    // ---------- Drawing ----------

    static void Car(Canvas c, int x, int y, Color body)
    {
        c.Rect(x, y, 13, 4, body);
        c.Rect(x + 3, y + 4, 7, 3, Shade(body, 0.9f));
        c.Rect(x + 4, y + 4, 2, 2, Palette.Glass);
        c.Rect(x + 7, y + 4, 2, 2, Palette.Glass);
        c.Rect(x + 1, y - 1, 3, 2, Palette.Ink);
        c.Rect(x + 9, y - 1, 3, 2, Palette.Ink);
    }

    class Canvas
    {
        public readonly int width, height;
        public readonly Color[] pixels;

        public Canvas(int width, int height)
        {
            this.width = width;
            this.height = height;
            pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Clear;
        }

        public void Set(int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return;
            float alpha = Mathf.Clamp01(color.a);
            Color under = pixels[y * width + x];
            Color mixed = under.a <= 0f ? color : Color.Lerp(under, color, alpha);
            mixed.a = Mathf.Max(under.a, alpha);
            pixels[y * width + x] = mixed;
        }

        public void Rect(int x, int y, int w, int h, Color color)
        {
            for (int j = y; j < y + h; j++)
                for (int i = x; i < x + w; i++)
                    Set(i, j, color);
        }

        public void Ring(float cx, float cy, float radius, float thickness, Color color)
        {
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    if (d <= radius && d >= radius - thickness) Set(x, y, color);
                }
        }

        public void Disc(float cx, float cy, float radius, Color color) => Ring(cx, cy, radius, radius + 1f, color);

        public void Line(float x0, float y0, float x1, float y1, float thickness, Color color)
        {
            var a = new Vector2(x0, y0);
            var b = new Vector2(x1, y1);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, b - a) / (b - a).sqrMagnitude);
                    if (Vector2.Distance(p, a + t * (b - a)) <= thickness / 2f) Set(x, y, color);
                }
        }

        // A five-pointed star (outer and inner radius).
        public void Star(float cx, float cy, float outer, float inner, Color color)
        {
            var points = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float angle = Mathf.PI / 2f + i * Mathf.PI / 5f;
                float r = i % 2 == 0 ? outer : inner;
                points[i] = new Vector2(cx + Mathf.Cos(angle) * r, cy + Mathf.Sin(angle) * r);
            }
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (Inside(points, new Vector2(x + 0.5f, y + 0.5f))) Set(x, y, color);
        }

        static bool Inside(Vector2[] polygon, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
                if ((polygon[i].y > p.y) != (polygon[j].y > p.y) &&
                    p.x < (polygon[j].x - polygon[i].x) * (p.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
                    inside = !inside;
            return inside;
        }
    }

    static Sprite Cached(string key, System.Func<Canvas> draw)
    {
        if (cache.TryGetValue(key, out Sprite sprite) && sprite != null) return sprite;
        Canvas c = draw();
        var texture = new Texture2D(c.width, c.height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        texture.SetPixels(c.pixels);
        texture.Apply();
        sprite = Sprite.Create(texture, new Rect(0, 0, c.width, c.height), new Vector2(0.5f, 0.5f), 18f);
        cache[key] = sprite;
        return sprite;
    }

    // Darker or lighter, same opacity.
    static Color Shade(Color color, float k) => new Color(color.r * k, color.g * k, color.b * k, color.a);

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color color);
        return color;
    }
}
