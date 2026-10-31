using UnityEngine;

public enum Hair { Bald, Short, Long, Bun, Curly, Gray }
public enum Headwear { None, Cowboy, Cap, LuchaMask, GuardCap }

// What someone looks like. PortraitArt draws it at the booth window and in the
// passport photo; the same Face also colors their little body walking around.
[System.Serializable]
public class Face
{
    [Tooltip("0 = lightest, 4 = darkest.")]
    [Range(0, 4)] public int skin = 2;
    public Hair hair = Hair.Short;
    public Color hairColor = new Color(0.17f, 0.13f, 0.11f);
    public Headwear headwear = Headwear.None;
    [Tooltip("Hat or mask color.")]
    public Color headwearColor = new Color(0.35f, 0.27f, 0.21f);
    public bool mustache;
    public bool beard;
    public bool glasses;
    public bool lipstick;
    [Tooltip("Older: a few lines on the face.")]
    public bool wrinkles;
    public Color shirt = new Color(0.85f, 0.81f, 0.72f);

    public Face Copy() => (Face)MemberwiseClone();

    // A stable number for this exact look, so each face is only drawn once.
    public int Key()
    {
        unchecked
        {
            int key = skin;
            key = key * 31 + (int)hair;
            key = key * 31 + hairColor.GetHashCode();
            key = key * 31 + (int)headwear;
            key = key * 31 + headwearColor.GetHashCode();
            key = key * 31 + (mustache ? 1 : 0) + (beard ? 2 : 0) + (glasses ? 4 : 0) + (lipstick ? 8 : 0) + (wrinkles ? 16 : 0);
            key = key * 31 + shirt.GetHashCode();
            return key;
        }
    }
}
