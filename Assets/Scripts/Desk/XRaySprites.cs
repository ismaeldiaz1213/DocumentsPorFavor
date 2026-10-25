using UnityEngine;

// Builds the round shapes the X-ray needs (a filled circle and a ring) in code,
// so we don't need any image files yet.
public static class XRaySprites
{
    static Sprite circle;
    static Sprite ring;

    public static Sprite Circle
    {
        get
        {
            if (circle == null) circle = Make(hollow: false);
            return circle;
        }
    }

    public static Sprite Ring
    {
        get
        {
            if (ring == null) ring = Make(hollow: true);
            return ring;
        }
    }

    static Sprite Make(bool hollow)
    {
        const int size = 64;
        const float ringThickness = 6f;
        float radius = size / 2f;

        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                float alpha = Mathf.Clamp01(radius - distance); // soft 1-pixel outer edge
                if (hollow)
                    alpha *= Mathf.Clamp01(distance - (radius - ringThickness));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
