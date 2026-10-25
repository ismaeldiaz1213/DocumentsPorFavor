using UnityEngine;

// The one set of colors the whole game uses: dusty blue sky above,
// faded yellow sand below. Lonely, western, a little washed out.
public static class Palette
{
    // World
    public static readonly Color SkyTop = Hex("8FA5B5");
    public static readonly Color SkyHorizon = Hex("E6D3A3");
    public static readonly Color FarMountains = Hex("8D97A6");
    public static readonly Color Mesa = Hex("B58A66");
    public static readonly Color MesaShadow = Hex("946E54");
    public static readonly Color Sand = Hex("D6BC8A");
    public static readonly Color SandShadow = Hex("BFA273");
    public static readonly Color Road = Hex("8E8577");
    public static readonly Color RoadLine = Hex("E6D3A3");
    public static readonly Color Cactus = Hex("6D7E5C");
    public static readonly Color CactusDark = Hex("4E5D44");
    public static readonly Color Concrete = Hex("B5AE9D");
    public static readonly Color ConcreteShadow = Hex("8F897B");
    public static readonly Color Metal = Hex("5D6A73");
    public static readonly Color Glass = Hex("7E99A6");
    public static readonly Color Ink = Hex("26252E");

    // Desk
    public static readonly Color DeskWood = Hex("5A4636");
    public static readonly Color DeskWoodDark = Hex("3D3029");
    public static readonly Color Paper = Hex("EADFC4");
    public static readonly Color PaperShadow = Hex("CFC2A3");
    public static readonly Color PaperInk = Hex("33323C");
    public static readonly Color PassportGreen = Hex("4F6B5A");
    public static readonly Color RegistrationBlue = Hex("4D6178");
    public static readonly Color Plate = Hex("E9E4D4");
    public static readonly Color Speech = Hex("2E2B2C");

    // Decisions and feedback
    public static readonly Color Approve = Hex("6E8B57");
    public static readonly Color Reject = Hex("A8503F");
    public static readonly Color Imprison = Hex("3E4550");
    public static readonly Color Bribe = Hex("C99A3E");
    public static readonly Color Warning = Hex("E8C25A");

    public static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color color);
        return color;
    }
}
