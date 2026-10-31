using System.Collections.Generic;
using UnityEngine;

// Draws people as pixel art while the game runs, from a Face:
//   Portrait(face)  head and shoulders, for the booth window and the passport photo
//   Body(face)      the little walking figure in the world (same size as person.png)
// Every look is drawn once and remembered, so asking twice is free.
public static class PortraitArt
{
    public const int PortraitWidth = 20;
    public const int PortraitHeight = 24;
    const int PixelsPerUnit = 18; // world scale, same as the rest of the art

    static readonly Color[] SkinTones =
    {
        Hex("F2D3B3"), Hex("DDB48C"), Hex("C69C72"), Hex("A0724E"), Hex("6E4B33"),
    };
    static readonly Color Pants = Hex("4D6178");
    static readonly Color Navy = Hex("2F3A4F");
    static readonly Color Badge = Hex("E8C25A");
    static readonly Color Lips = Hex("A8503F");
    static readonly Color GrayHair = Hex("B9B4AA");

    static readonly Dictionary<int, Sprite> portraits = new Dictionary<int, Sprite>();
    static readonly Dictionary<int, Sprite> bodies = new Dictionary<int, Sprite>();

    // The look of Primo José in his security uniform.
    public static Face Guard() => new Face
    {
        skin = 2, hair = Hair.Short, headwear = Headwear.GuardCap, headwearColor = Navy,
        mustache = true, shirt = Navy,
    };

    public static Sprite Portrait(Face face)
    {
        if (face == null) return null;
        if (portraits.TryGetValue(face.Key(), out Sprite cached)) return cached;
        Sprite sprite = ToSprite(DrawPortrait(face), PortraitWidth, PortraitHeight, new Vector2(0.5f, 0f));
        portraits[face.Key()] = sprite;
        return sprite;
    }

    public static Sprite Body(Face face)
    {
        if (face == null) return null;
        if (bodies.TryGetValue(face.Key(), out Sprite cached)) return cached;
        Sprite sprite = ToSprite(DrawBody(face), 7, 14, new Vector2(0.5f, 0f));
        bodies[face.Key()] = sprite;
        return sprite;
    }

    // ---------- Drawing ----------

    static Color[] DrawPortrait(Face f)
    {
        var p = new Color[PortraitWidth * PortraitHeight];
        Color skin = SkinTones[Mathf.Clamp(f.skin, 0, SkinTones.Length - 1)];
        Color shade = Color.Lerp(skin, Color.black, 0.18f);
        Color hair = f.hair == Hair.Gray ? GrayHair : f.hairColor;
        Color ink = Hex("26252E");

        // Shoulders, neck, head
        Fill(p, 2, 0, 17, 4, f.shirt);
        Fill(p, 3, 5, 16, 5, f.shirt);
        Fill(p, 9, 4, 10, 5, skin);                 // open collar
        Fill(p, 8, 6, 11, 7, shade);                // neck
        Fill(p, 5, 8, 14, 19, skin);                // head
        foreach (var corner in new[] { (5, 8), (14, 8), (5, 19), (14, 19) })
            Set(p, corner.Item1, corner.Item2, Color.clear);
        Fill(p, 4, 12, 4, 14, shade);               // ears
        Fill(p, 15, 12, 15, 14, shade);

        // Hair (under any hat)
        switch (f.hair)
        {
            case Hair.Short:
            case Hair.Gray:
                Fill(p, 5, 18, 14, 20, hair);
                Fill(p, 4, 15, 5, 18, hair);
                Fill(p, 14, 15, 15, 18, hair);
                break;
            case Hair.Long:
                Fill(p, 5, 18, 14, 20, hair);
                Fill(p, 3, 7, 5, 18, hair);
                Fill(p, 14, 7, 16, 18, hair);
                break;
            case Hair.Bun:
                Fill(p, 5, 18, 14, 20, hair);
                Fill(p, 4, 15, 5, 18, hair);
                Fill(p, 14, 15, 15, 18, hair);
                Fill(p, 8, 21, 11, 22, hair);
                break;
            case Hair.Curly:
                Fill(p, 4, 17, 15, 21, hair);
                Fill(p, 3, 13, 5, 18, hair);
                Fill(p, 14, 13, 16, 18, hair);
                for (int x = 4; x <= 15; x += 2) Set(p, x, 22, hair);
                break;
            case Hair.Bald:
                Fill(p, 4, 14, 4, 16, hair);
                Fill(p, 15, 14, 15, 16, hair);
                break;
        }

        // Face
        Fill(p, 7, 16, 8, 16, hair);                // eyebrows
        Fill(p, 11, 16, 12, 16, hair);
        Set(p, 7, 14, ink);                         // eyes
        Set(p, 12, 14, ink);
        Fill(p, 9, 12, 10, 12, shade);              // nose
        Fill(p, 8, 10, 11, 10, f.lipstick ? Lips : shade);
        if (f.wrinkles)
        {
            Set(p, 6, 11, shade); Set(p, 13, 11, shade);
            Fill(p, 8, 17, 11, 17, shade);
        }
        if (f.beard)
        {
            Fill(p, 5, 8, 14, 11, hair);
            Fill(p, 8, 10, 11, 10, f.lipstick ? Lips : shade);
        }
        if (f.mustache)
            Fill(p, 7, 11, 12, 11, hair);
        if (f.glasses)
        {
            Outline(p, 6, 13, 8, 15, ink);
            Outline(p, 11, 13, 13, 15, ink);
            Fill(p, 9, 14, 10, 14, ink);
        }

        // Headwear (on top of everything)
        switch (f.headwear)
        {
            case Headwear.Cowboy:
                Fill(p, 1, 19, 18, 19, f.headwearColor);
                Fill(p, 5, 20, 14, 23, f.headwearColor);
                Fill(p, 5, 20, 14, 20, Color.Lerp(f.headwearColor, Color.black, 0.3f)); // band
                break;
            case Headwear.Cap:
                Fill(p, 5, 19, 14, 22, f.headwearColor);
                Fill(p, 4, 18, 15, 18, Color.Lerp(f.headwearColor, Color.black, 0.3f));
                break;
            case Headwear.GuardCap:
                Fill(p, 4, 19, 15, 22, f.headwearColor);
                Fill(p, 4, 18, 15, 18, ink);
                Fill(p, 9, 20, 10, 21, Badge);
                Set(p, 4, 3, Badge); Set(p, 5, 3, Badge); // shoulder badge
                break;
            case Headwear.LuchaMask:
                Color trim = Badge;
                Fill(p, 5, 9, 14, 21, f.headwearColor);
                Fill(p, 6, 22, 13, 22, f.headwearColor);
                Fill(p, 9, 15, 10, 21, trim);               // center stripe
                Outline(p, 6, 13, 8, 15, trim);             // eye holes
                Outline(p, 11, 13, 13, 15, trim);
                Set(p, 7, 14, ink); Set(p, 12, 14, ink);
                Fill(p, 8, 10, 11, 10, skin);               // mouth opening
                Fill(p, 8, 9, 11, 9, skin);
                break;
        }

        return p;
    }

    static Color[] DrawBody(Face f)
    {
        var p = new Color[7 * 14];
        Color skin = SkinTones[Mathf.Clamp(f.skin, 0, SkinTones.Length - 1)];
        Color hair = f.hair == Hair.Gray ? GrayHair : f.hairColor;
        Color legs = f.headwear == Headwear.GuardCap ? Navy : Pants;

        Fill(p, 2, 0, 2, 3, legs, 7);
        Fill(p, 4, 0, 4, 3, legs, 7);
        Fill(p, 2, 3, 4, 3, legs, 7);
        Fill(p, 1, 4, 5, 8, f.shirt, 7);
        Fill(p, 0, 5, 0, 7, f.shirt, 7);
        Fill(p, 6, 5, 6, 7, f.shirt, 7);
        Fill(p, 2, 9, 4, 11, skin, 7);
        if (f.hair == Hair.Long) Fill(p, 1, 8, 1, 11, hair, 7);

        switch (f.headwear)
        {
            case Headwear.Cowboy:
                Fill(p, 0, 11, 6, 11, f.headwearColor, 7);
                Fill(p, 2, 12, 4, 13, f.headwearColor, 7);
                break;
            case Headwear.Cap:
            case Headwear.GuardCap:
                Fill(p, 2, 12, 4, 12, f.headwearColor, 7);
                Fill(p, 1, 11, 5, 11, f.headwearColor, 7);
                break;
            case Headwear.LuchaMask:
                Fill(p, 2, 9, 4, 12, f.headwearColor, 7);
                Set(p, 3, 10, Badge, 7);
                break;
            default:
                if (f.hair != Hair.Bald) Fill(p, 2, 12, 4, 12, hair, 7);
                break;
        }
        return p;
    }

    // ---------- Pixel helpers ----------

    static void Set(Color[] p, int x, int y, Color c, int width = PortraitWidth)
    {
        int height = p.Length / width;
        if (x >= 0 && x < width && y >= 0 && y < height) p[y * width + x] = c;
    }

    static void Fill(Color[] p, int x0, int y0, int x1, int y1, Color c, int width = PortraitWidth)
    {
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                Set(p, x, y, c, width);
    }

    static void Outline(Color[] p, int x0, int y0, int x1, int y1, Color c)
    {
        for (int x = x0; x <= x1; x++) { Set(p, x, y0, c); Set(p, x, y1, c); }
        for (int y = y0; y <= y1; y++) { Set(p, x0, y, c); Set(p, x1, y, c); }
    }

    static Sprite ToSprite(Color[] pixels, int width, int height, Vector2 pivot)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), pivot, PixelsPerUnit);
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color color);
        return color;
    }
}
