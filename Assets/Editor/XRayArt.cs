using UnityEditor;
using UnityEngine;

// Draws the detailed X-ray pictures: each vehicle from the side and from above,
// plus luggage and contraband (people, crates, a toolbox, a tamal pot...).
//
// Instead of colors we paint DENSITY: how much stuff each pixel passes through.
// Overlapping parts add up and glow brighter, like a real X-ray. The game tints
// the result with the monitor's glow color.
//
// Vehicle pictures are 200 x 72 px, the same shape as the X-ray screens, and line up
// with the 0..1 positions used by the traveler data (the car always faces RIGHT).
public static class XRayArt
{
    const int Width = 200;
    const int Height = 72;

    public static void Generate()
    {
        SaveVehicle("Sedan", SedanSide(), SedanTop());
        SaveVehicle("Pickup", PickupSide(), PickupTop());
        SaveVehicle("Van", VanSide(), VanTop());

        SaveIcon("person", PersonSide(), PersonTop());
        SaveIcon("person_b", PersonSide(), PersonTopLying());
        SaveIcon("crate", CrateSide(), CrateTop());
        SaveIcon("toolbox", ToolboxSide(), ToolboxTop());
        SaveIcon("pot", PotSide(), PotTop());
        SaveIcon("gift", GiftSide(), GiftTop());
        SaveIcon("trumpet", TrumpetSide(), TrumpetTop());
        SaveIcon("guitar", GuitarSide(), GuitarTop());
    }

    static void SaveVehicle(string model, XRayCanvas side, XRayCanvas top)
    {
        VehicleModel asset = Day1StoryWriter.Vehicle(model);
        asset.xraySide = ArtGenerator.Save($"xray_{model.ToLower()}_side", side.ToPixels(), new Vector2(0.5f, 0.5f));
        asset.xrayTop = ArtGenerator.Save($"xray_{model.ToLower()}_top", top.ToPixels(), new Vector2(0.5f, 0.5f));
        EditorUtility.SetDirty(asset);
    }

    static void SaveIcon(string name, XRayCanvas side, XRayCanvas top)
    {
        ArtGenerator.Save($"xray_{name}_side", side.ToPixels(), new Vector2(0.5f, 0.5f));
        ArtGenerator.Save($"xray_{name}_top", top.ToPixels(), new Vector2(0.5f, 0.5f));
    }

    // ---------- Shared car parts ----------

    // A wheel seen from the side: tire, rim, brake disc, spokes, hub.
    static void Wheel(XRayCanvas c, float x, float y, float r)
    {
        c.Ring(x, y, r, 3f, 0.42f);
        c.Ring(x, y, r - 3.5f, 1.2f, 0.6f);
        c.Ring(x, y, r - 6f, 1f, 0.35f);
        for (int i = 0; i < 5; i++)
        {
            float a = i * Mathf.PI * 2f / 5f + 0.3f;
            c.Line(x, y, x + Mathf.Cos(a) * (r - 3.5f), y + Mathf.Sin(a) * (r - 3.5f), 0.4f);
        }
        c.Disc(x, y, 1.6f, 0.9f);
    }

    // A tire seen from above: a rectangle with tread lines.
    static void TopWheel(XRayCanvas c, int x0, int y0, int x1, int y1)
    {
        c.FillRect(x0, y0, x1, y1, 0.38f);
        for (int x = x0 + 1; x < x1; x += 3)
            c.Line(x, y0, x, y1, 0.18f);
        c.Rect(x0, y0, x1, y1, 0.3f);
    }

    static void Spring(XRayCanvas c, float x, float y0, float y1)
    {
        int turns = 5;
        float step = (y1 - y0) / (turns * 2);
        for (int i = 0; i < turns * 2; i++)
        {
            float xa = x + (i % 2 == 0 ? -3f : 3f);
            float xb = x + (i % 2 == 0 ? 3f : -3f);
            c.Line(xa, y0 + i * step, xb, y0 + (i + 1) * step, 0.5f);
        }
    }

    // Seat from the side: a cushion and a leaning backrest with a headrest.
    static void SideSeat(XRayCanvas c, int x0, int x1, int baseY, int backTopX, int backTopY)
    {
        c.FillRect(x0, baseY - 4, x1, baseY, 0.28f);
        c.Rect(x0, baseY - 4, x1, baseY, 0.2f);
        for (int i = 0; i < 4; i++)
            c.Line(x0 + i, baseY, backTopX + i, backTopY, 0.28f);
        c.FillRect(backTopX - 3, backTopY, backTopX + 3, backTopY + 5, 0.38f);
        c.Line(x0 + 2, baseY - 4, x0 + 2, baseY - 10, 0.5f); // seat rail
    }

    // Seat from above: cushion with a backrest bar.
    static void TopSeat(XRayCanvas c, int x0, int y0, int x1, int y1)
    {
        c.FillRect(x0, y0, x1, y1, 0.26f);
        c.Rect(x0, y0, x1, y1, 0.22f);
        c.FillRect(x0, y0, x0 + 3, y1, 0.3f);
        c.FillRect(x0 - 3, (y0 + y1) / 2 - 2, x0 - 1, (y0 + y1) / 2 + 2, 0.35f); // headrest
    }

    static void Engine(XRayCanvas c, int x0, int y0, int x1, int y1, int cylinders, bool top)
    {
        c.FillRect(x0, y0, x1, y1, 0.42f);
        c.Rect(x0, y0, x1, y1, 0.35f);
        float spacing = (top ? (y1 - y0) : (x1 - x0)) / (float)(cylinders + 1);
        for (int i = 1; i <= cylinders; i++)
        {
            if (top) c.Ring((x0 + x1) / 2f, y0 + i * spacing, 2.5f, 1f, 0.35f);
            else c.Ring(x0 + i * spacing, y1 - 4, 2.5f, 1f, 0.35f);
        }
        c.Line(x0, top ? y0 - 2 : y1 + 2, x1, top ? y0 - 2 : y1 + 2, 0.4f); // intake / valve cover
    }

    static void Radiator(XRayCanvas c, int x, int y0, int y1)
    {
        c.FillRect(x, y0, x + 1, y1, 0.45f);
        for (int y = y0; y <= y1; y += 2)
            c.Add(x + 2, y, 0.3f);
    }

    static void Noise(XRayCanvas c, int seed)
    {
        var random = new System.Random(seed);
        for (int y = 0; y < c.height; y++)
            for (int x = 0; x < c.width; x++)
                if (random.NextDouble() < 0.12) c.Add(x, y, 0.035f);
    }

    // A loose cable wandering through the car.
    static void Wire(XRayCanvas c, int seed, float x0, float y0, float x1, float y1)
    {
        var random = new System.Random(seed);
        float px = x0, py = y0;
        for (int i = 1; i <= 12; i++)
        {
            float t = i / 12f;
            float nx = Mathf.Lerp(x0, x1, t);
            float ny = Mathf.Lerp(y0, y1, t) + (float)(random.NextDouble() * 4 - 2);
            c.Line(px, py, nx, ny, 0.14f);
            px = nx;
            py = ny;
        }
    }

    // ---------- Sedan ----------

    static XRayCanvas SedanSide()
    {
        var c = new XRayCanvas(Width, Height);
        c.Polygon(new[] { V(8, 10), V(6, 36), V(40, 42), V(62, 60), V(128, 60), V(150, 42), V(192, 38), V(196, 26), V(194, 10) }, 0.07f, 0.5f);

        c.Line(20, 18, 180, 18, 0.3f);                    // floor pan
        c.Line(40, 42, 150, 42, 0.3f);                    // beltline
        c.Line(96, 18, 96, 60, 0.3f);                     // B-pillar
        c.Line(146, 42, 126, 58, 0.22f);                  // A-pillar
        c.Rect(62, 18, 96, 42, 0.18f);                    // doors
        c.Rect(96, 18, 140, 42, 0.18f);
        c.FillRect(88, 36, 92, 37, 0.5f);                 // handles
        c.FillRect(130, 36, 134, 37, 0.5f);
        c.Ring(76, 26, 3f, 1f, 0.22f);                    // door speakers
        c.Ring(118, 26, 3f, 1f, 0.22f);

        Engine(c, 164, 18, 186, 34, 4, top: false);
        Radiator(c, 189, 14, 34);
        c.FillRect(176, 36, 182, 39, 0.55f);              // battery

        c.Line(150, 40, 132, 28, 0.4f);                   // steering column
        c.Ellipse(147, 38, 2f, 5f, 1.2f, 0.5f);           // steering wheel
        c.Line(150, 18, 146, 24, 0.45f);                  // pedals
        c.Line(153, 18, 149, 24, 0.45f);

        SideSeat(c, 112, 130, 26, 104, 50);               // front seat
        SideSeat(c, 66, 88, 26, 60, 46);                  // back seat

        c.Ring(16, 30, 8f, 3f, 0.4f);                     // spare tire in the trunk
        c.Ring(16, 30, 3f, 1f, 0.5f);
        c.FillRect(40, 18, 60, 24, 0.33f);                // gas tank
        c.Line(10, 9, 150, 9, 0.33f);                     // exhaust
        c.FillRect(18, 7, 34, 11, 0.33f);                 // muffler
        c.Line(56, 13, 150, 13, 0.28f);                   // driveshaft

        Spring(c, 44, 20, 32);
        Spring(c, 156, 20, 32);
        Wheel(c, 44, 14, 11);
        Wheel(c, 156, 14, 11);

        Wire(c, 3, 150, 30, 20, 34);
        Noise(c, 1);
        return c;
    }

    static XRayCanvas SedanTop()
    {
        var c = new XRayCanvas(Width, Height);
        c.Polygon(new[] { V(8, 10), V(6, 36), V(8, 62), V(186, 64), V(196, 52), V(196, 20), V(186, 8) }, 0.07f, 0.5f);
        c.Rect(60, 12, 140, 60, 0.22f);                   // roof line

        TopWheel(c, 32, 1, 56, 11);
        TopWheel(c, 32, 61, 56, 71);
        TopWheel(c, 144, 1, 168, 11);
        TopWheel(c, 144, 61, 168, 71);
        c.Line(44, 11, 44, 61, 0.4f);                     // axles
        c.Line(156, 11, 156, 61, 0.4f);
        c.Disc(44, 36, 3f, 0.4f);                         // differential

        Engine(c, 164, 20, 188, 52, 4, top: true);
        Radiator(c, 190, 16, 56);

        TopSeat(c, 104, 14, 120, 30);                     // driver
        TopSeat(c, 104, 42, 120, 58);                     // passenger
        c.FillRect(62, 14, 80, 58, 0.26f);                // back bench
        c.Line(62, 36, 80, 36, 0.2f);
        c.Ring(126, 22, 5f, 1.5f, 0.5f);                  // steering wheel
        c.Line(126, 22, 150, 24, 0.35f);
        c.Line(132, 12, 132, 60, 0.3f);                   // dashboard
        c.Disc(118, 36, 1.5f, 0.6f);                      // gear shift

        c.Ring(18, 36, 9f, 3f, 0.4f);                     // spare tire
        c.Ring(18, 36, 3f, 1f, 0.5f);
        c.FillRect(40, 26, 58, 46, 0.33f);                // gas tank
        c.Line(48, 36, 160, 36, 0.28f);                   // driveshaft
        c.Line(10, 50, 160, 50, 0.3f);                    // exhaust
        c.FillRect(20, 47, 36, 53, 0.33f);
        c.FillRect(140, 4, 144, 7, 0.4f);                 // mirrors
        c.FillRect(140, 65, 144, 68, 0.4f);

        Wire(c, 5, 150, 40, 30, 44);
        Noise(c, 2);
        return c;
    }

    // ---------- Pickup ----------

    static XRayCanvas PickupSide()
    {
        var c = new XRayCanvas(Width, Height);
        c.Polygon(new[] { V(6, 10), V(6, 40), V(108, 40), V(108, 62), V(152, 62), V(164, 44), V(192, 40), V(196, 26), V(194, 10) }, 0.07f, 0.5f);

        c.Rect(8, 14, 106, 40, 0.22f);                    // bed walls
        c.Line(8, 26, 106, 26, 0.28f);                    // bed floor
        for (int x = 20; x < 106; x += 12) c.Line(x, 14, x, 40, 0.12f); // bed ribs
        c.Line(8, 14, 8, 40, 0.38f);                      // tailgate
        c.Line(108, 14, 108, 62, 0.33f);                  // cab back wall
        c.Rect(110, 18, 150, 42, 0.18f);                  // door
        c.Line(110, 42, 160, 42, 0.3f);
        c.FillRect(140, 36, 144, 37, 0.5f);

        Engine(c, 168, 18, 190, 34, 4, top: false);
        Radiator(c, 191, 14, 36);
        c.FillRect(180, 36, 186, 39, 0.55f);

        c.Line(152, 40, 136, 28, 0.4f);
        c.Ellipse(150, 38, 2f, 5f, 1.2f, 0.5f);
        SideSeat(c, 118, 140, 26, 112, 50);               // bench seat

        c.Ring(66, 16, 6f, 2f, 0.4f);                     // spare under the bed
        c.FillRect(84, 15, 104, 22, 0.33f);               // gas tank
        c.Line(10, 8, 160, 8, 0.33f);                     // exhaust
        c.FillRect(40, 6, 56, 10, 0.33f);
        c.Line(28, 20, 56, 20, 0.4f);                     // leaf springs
        c.Line(30, 22, 54, 22, 0.35f);
        c.Line(56, 13, 150, 13, 0.28f);                   // driveshaft

        Spring(c, 160, 20, 32);
        Wheel(c, 42, 14, 12);
        Wheel(c, 160, 14, 12);

        Wire(c, 7, 160, 30, 112, 36);
        Noise(c, 3);
        return c;
    }

    static XRayCanvas PickupTop()
    {
        var c = new XRayCanvas(Width, Height);
        c.Polygon(new[] { V(6, 8), V(6, 64), V(184, 64), V(196, 52), V(196, 20), V(184, 8) }, 0.07f, 0.5f);

        c.Rect(8, 10, 106, 62, 0.28f);                    // bed
        for (int y = 14; y < 60; y += 6) c.Line(10, y, 104, y, 0.13f); // corrugated floor
        c.Rect(110, 10, 160, 62, 0.22f);                  // cab

        TopWheel(c, 30, 1, 54, 11);
        TopWheel(c, 30, 61, 54, 71);
        TopWheel(c, 148, 1, 172, 11);
        TopWheel(c, 148, 61, 172, 71);
        c.Line(42, 11, 42, 61, 0.4f);
        c.Line(160, 11, 160, 61, 0.4f);
        c.Disc(42, 36, 3f, 0.4f);

        Engine(c, 166, 20, 190, 52, 4, top: true);
        Radiator(c, 192, 16, 56);
        c.FillRect(126, 14, 140, 58, 0.28f);              // bench seat
        c.FillRect(126, 14, 129, 58, 0.3f);
        c.Ring(146, 22, 5f, 1.5f, 0.5f);
        c.Line(150, 12, 150, 60, 0.3f);

        c.Ring(66, 36, 8f, 3f, 0.3f);                     // spare under the bed
        c.FillRect(84, 24, 104, 40, 0.3f);                // gas tank
        c.Line(54, 36, 160, 36, 0.28f);
        c.Line(10, 50, 160, 50, 0.3f);
        c.FillRect(40, 47, 56, 53, 0.33f);

        Noise(c, 4);
        return c;
    }

    // ---------- Van ----------

    static XRayCanvas VanSide()
    {
        var c = new XRayCanvas(Width, Height);
        c.Polygon(new[] { V(8, 8), V(8, 66), V(174, 66), V(184, 48), V(194, 40), V(196, 24), V(194, 8) }, 0.07f, 0.5f);

        for (int x = 20; x < 146; x += 14) c.Line(x, 10, x, 64, 0.11f); // cargo wall ribs
        c.Line(20, 49, 100, 49, 0.35f);                   // shelf
        c.Line(20, 49, 20, 44, 0.3f);
        c.Line(100, 49, 100, 44, 0.3f);
        c.Rect(104, 10, 136, 62, 0.18f);                  // sliding door
        c.Line(10, 8, 10, 66, 0.3f);                      // rear doors
        c.Line(146, 8, 146, 66, 0.25f);                   // partition behind the seats

        Engine(c, 180, 14, 194, 30, 3, top: false);
        c.FillRect(176, 32, 181, 35, 0.5f);
        c.Line(176, 36, 166, 28, 0.4f);
        c.Ellipse(174, 34, 2f, 5f, 1.2f, 0.5f);
        SideSeat(c, 152, 166, 26, 148, 46);               // driver seat

        c.Ring(14, 22, 6f, 2f, 0.4f);                     // spare
        c.FillRect(60, 12, 80, 18, 0.3f);                 // gas tank
        c.Line(10, 6, 150, 6, 0.33f);                     // exhaust
        c.Line(54, 10, 150, 10, 0.28f);                   // driveshaft

        Spring(c, 154, 16, 26);
        Wheel(c, 42, 11, 10);
        Wheel(c, 154, 11, 10);

        Wire(c, 9, 170, 40, 14, 58);
        Noise(c, 5);
        return c;
    }

    static XRayCanvas VanTop()
    {
        var c = new XRayCanvas(Width, Height);
        c.Polygon(new[] { V(8, 8), V(8, 64), V(176, 64), V(192, 58), V(196, 44), V(196, 28), V(192, 14), V(176, 8) }, 0.07f, 0.5f);

        for (int y = 14; y < 60; y += 6) c.Line(12, y, 140, y, 0.1f); // cargo floor ribs
        c.FillRect(20, 7, 100, 10, 0.33f);                // shelf along the wall
        c.Line(146, 8, 146, 64, 0.28f);                   // partition

        TopWheel(c, 30, 1, 54, 11);
        TopWheel(c, 30, 61, 54, 71);
        TopWheel(c, 142, 1, 166, 11);
        TopWheel(c, 142, 61, 166, 71);
        c.Line(42, 11, 42, 61, 0.4f);
        c.Line(154, 11, 154, 61, 0.4f);

        Engine(c, 180, 22, 194, 50, 3, top: true);
        TopSeat(c, 154, 16, 166, 28);
        TopSeat(c, 154, 44, 166, 56);
        c.Ring(172, 22, 5f, 1.5f, 0.5f);

        c.Ring(14, 36, 8f, 3f, 0.4f);
        c.FillRect(60, 26, 80, 46, 0.3f);
        c.Line(54, 36, 156, 36, 0.28f);
        c.Line(10, 52, 150, 52, 0.3f);

        Noise(c, 6);
        return c;
    }

    // ---------- Contraband: people ----------
    // On an X-ray, a body is a soft blob with faint bones inside: findable, not obvious.

    // Curled up on their side, knees to chest.
    static XRayCanvas PersonSide()
    {
        var c = new XRayCanvas(20, 24);
        c.FillEllipse(10, 10, 6.5f, 7.5f, 0.16f);         // torso, curled
        c.FillEllipse(14, 18, 4f, 4.2f, 0.16f);           // head
        c.FillEllipse(16, 8, 3f, 4.5f, 0.1f);             // knees and shins
        c.Ellipse(14, 18, 3.6f, 3.8f, 1f, 0.3f);          // skull
        for (int i = 0; i < 7; i++)                       // vertebrae along a curved back
        {
            float t = i / 6f;
            c.Disc(Mathf.Lerp(12f, 6.5f, t) - Mathf.Sin(t * Mathf.PI) * 1.5f, Mathf.Lerp(14.5f, 5f, t), 0.7f, 0.28f);
        }
        for (int i = 0; i < 3; i++)                       // ribs
            c.Line(10 - i, 12.5f - i * 2, 13.5f - i, 11 - i * 2, 0.16f);
        c.Ellipse(7, 5, 2.2f, 1.8f, 0.8f, 0.25f);         // pelvis
        c.Line(8, 5.5f, 16, 9.5f, 0.32f);                 // thigh
        c.Line(16, 9.5f, 15.5f, 2, 0.3f);                 // shin
        c.Line(13, 13, 17, 10, 0.22f);                    // arm around the knees
        return c;
    }

    // Seen from above: hunched over, head down on the knees.
    static XRayCanvas PersonTop()
    {
        var c = new XRayCanvas(20, 20);
        c.FillEllipse(10, 8, 7f, 6f, 0.15f);              // back and shoulders
        c.FillEllipse(10, 13.5f, 3.8f, 3.6f, 0.17f);      // bowed head
        c.FillEllipse(5, 5, 2f, 4f, 0.1f);                // arms around the knees
        c.FillEllipse(15, 5, 2f, 4f, 0.1f);
        c.Ellipse(10, 13.5f, 3.4f, 3.2f, 0.9f, 0.28f);    // skull
        for (int i = 0; i < 6; i++)                       // vertebrae
            c.Disc(10, 11 - i * 1.6f, 0.6f, 0.26f);
        c.Ellipse(6.5f, 9, 2f, 1.4f, 0.7f, 0.2f);         // shoulder blades
        c.Ellipse(13.5f, 9, 2f, 1.4f, 0.7f, 0.2f);
        c.Line(4, 9, 5, 2, 0.2f);                         // arm bones
        c.Line(16, 9, 15, 2, 0.2f);
        return c;
    }

    // A second pose so two passengers don't look like copies: lying curled on one side.
    static XRayCanvas PersonTopLying()
    {
        var c = new XRayCanvas(20, 20);
        c.FillEllipse(10, 9, 6f, 5f, 0.15f);              // body
        c.FillEllipse(4.5f, 13, 3.6f, 3.4f, 0.17f);       // head tucked in
        c.FillEllipse(15, 6, 3.5f, 3f, 0.11f);            // legs folded
        c.Ellipse(4.5f, 13, 3.2f, 3f, 0.9f, 0.28f);       // skull
        for (int i = 0; i < 6; i++)                       // spine curling around
        {
            float a = Mathf.Lerp(2.4f, 0.4f, i / 5f);
            c.Disc(10 + Mathf.Cos(a) * 5f, 8 + Mathf.Sin(a) * 5f, 0.6f, 0.26f);
        }
        c.Ellipse(14, 9, 1.8f, 1.6f, 0.7f, 0.22f);        // pelvis
        c.Line(14, 8, 18, 5, 0.25f);                      // thigh
        c.Line(18, 5, 13, 3, 0.22f);                      // shin
        return c;
    }

    // ---------- Luggage ----------

    static XRayCanvas CrateSide()
    {
        var c = new XRayCanvas(16, 12);
        c.Rect(0, 0, 15, 11, 0.5f);
        c.Line(0, 4, 15, 4, 0.35f);
        c.Line(0, 8, 15, 8, 0.35f);
        for (int x = 3; x < 15; x += 4)
        {
            c.Disc(x, 2, 1.6f, 0.3f);
            c.Disc(x + 1, 6, 1.6f, 0.3f);
            c.Disc(x, 10, 1.6f, 0.3f);
        }
        return c;
    }

    static XRayCanvas CrateTop()
    {
        var c = new XRayCanvas(16, 16);
        c.Rect(0, 0, 15, 15, 0.5f);
        c.Line(5, 0, 5, 15, 0.25f);
        c.Line(10, 0, 10, 15, 0.25f);
        for (int y = 3; y < 15; y += 4)
            for (int x = 3; x < 15; x += 4)
                c.Disc(x, y, 1.7f, 0.32f);
        return c;
    }

    static XRayCanvas ToolboxSide()
    {
        var c = new XRayCanvas(16, 10);
        c.FillRect(1, 0, 14, 6, 0.5f);
        c.Rect(1, 0, 14, 6, 0.8f);
        c.Line(5, 7, 10, 7, 0.8f);                        // handle
        c.Line(5, 6, 5, 7, 0.8f);
        c.Line(10, 6, 10, 7, 0.8f);
        c.Line(3, 2, 12, 4, 0.95f);                       // wrench
        c.Ring(12.5f, 4, 1.4f, 0.8f, 0.9f);
        return c;
    }

    static XRayCanvas ToolboxTop()
    {
        var c = new XRayCanvas(12, 16);
        c.FillRect(0, 0, 11, 15, 0.5f);
        c.Rect(0, 0, 11, 15, 0.8f);
        c.Line(2, 2, 9, 12, 0.95f);                       // wrench
        c.Ring(9.5f, 12.5f, 1.4f, 0.8f, 0.9f);
        c.Line(8, 2, 8, 9, 0.9f);                         // screwdriver
        c.FillRect(7, 9, 9, 12, 0.6f);
        return c;
    }

    static XRayCanvas PotSide()
    {
        var c = new XRayCanvas(14, 14);
        c.FillRect(2, 1, 11, 10, 0.32f);
        c.Rect(2, 1, 11, 10, 0.7f);
        c.Line(1, 11, 12, 11, 0.7f);                      // lid
        c.Disc(6.5f, 12.5f, 1f, 0.7f);                    // knob
        c.FillRect(0, 7, 1, 8, 0.7f);                     // handles
        c.FillRect(12, 7, 13, 8, 0.7f);
        for (int y = 3; y <= 8; y += 2)                   // stacked tamales
            c.Line(3, y, 10, y, 0.25f);
        return c;
    }

    static XRayCanvas PotTop()
    {
        var c = new XRayCanvas(14, 14);
        c.Disc(7, 7, 6f, 0.25f);
        c.Ring(7, 7, 6f, 1.5f, 0.7f);
        for (int i = 0; i < 6; i++)                       // tamales standing in a circle
        {
            float a = i * Mathf.PI / 3f;
            c.Disc(7 + Mathf.Cos(a) * 3.2f, 7 + Mathf.Sin(a) * 3.2f, 1.1f, 0.3f);
        }
        c.FillRect(0, 6, 0, 8, 0.7f);
        c.FillRect(13, 6, 13, 8, 0.7f);
        return c;
    }

    static XRayCanvas GiftSide()
    {
        var c = new XRayCanvas(12, 12);
        c.FillRect(0, 0, 11, 8, 0.28f);
        c.Rect(0, 0, 11, 8, 0.5f);
        c.Line(6, 0, 6, 8, 0.5f);                         // ribbon
        c.Line(0, 4, 11, 4, 0.5f);
        c.Ring(4.5f, 10, 1.6f, 0.8f, 0.5f);               // bow
        c.Ring(7.5f, 10, 1.6f, 0.8f, 0.5f);
        return c;
    }

    static XRayCanvas GiftTop()
    {
        var c = new XRayCanvas(12, 12);
        c.FillRect(0, 0, 11, 11, 0.28f);
        c.Rect(0, 0, 11, 11, 0.5f);
        c.Line(6, 0, 6, 11, 0.5f);
        c.Line(0, 6, 11, 6, 0.5f);
        c.Disc(6, 6, 1.5f, 0.6f);
        return c;
    }

    static XRayCanvas TrumpetSide()
    {
        var c = new XRayCanvas(18, 8);
        c.Polygon(new[] { V(13, 2), V(17, 0), V(17, 7), V(13, 5) }, 0.6f, 0.9f); // bell
        c.Rect(3, 2, 12, 5, 0.85f);                       // tubing loop
        c.Line(0, 3, 3, 3, 0.8f);                         // mouthpiece
        for (int x = 6; x <= 10; x += 2) c.Line(x, 5, x, 7, 0.9f); // valves
        return c;
    }

    static XRayCanvas TrumpetTop()
    {
        var c = new XRayCanvas(18, 6);
        c.Polygon(new[] { V(13, 2), V(17, 0), V(17, 5), V(13, 3) }, 0.6f, 0.9f);
        c.Rect(3, 1, 12, 4, 0.85f);
        c.Line(0, 2, 3, 2, 0.8f);
        return c;
    }

    static XRayCanvas GuitarSide()
    {
        var c = new XRayCanvas(24, 16);
        c.FillEllipse(9, 8, 8f, 7f, 0.2f);
        c.Ellipse(9, 8, 8f, 7f, 1.5f, 0.55f);             // body
        c.Ring(10, 8, 2.5f, 1f, 0.45f);                   // sound hole
        c.Line(16, 8, 23, 9, 0.6f);                       // neck
        c.Line(16, 9, 23, 10, 0.6f);
        for (int i = 0; i < 3; i++) c.Line(4, 7 + i, 23, 8 + i, 0.15f); // strings
        return c;
    }

    static XRayCanvas GuitarTop()
    {
        var c = new XRayCanvas(24, 18);
        c.FillEllipse(9, 9, 8f, 7f, 0.2f);
        c.Ellipse(9, 9, 8f, 7f, 1.5f, 0.55f);
        c.Line(16, 9, 23, 9, 0.6f);
        c.Line(16, 10, 23, 10, 0.6f);
        return c;
    }

    static Vector2 V(float x, float y) => new Vector2(x, y);

    // ---------- Density canvas ----------

    class XRayCanvas
    {
        public readonly int width;
        public readonly int height;
        readonly float[] density;

        public XRayCanvas(int width, int height)
        {
            this.width = width;
            this.height = height;
            density = new float[width * height];
        }

        // Stuff stacks up: overlapping parts glow brighter.
        public void Add(int x, int y, float amount)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return;
            int i = y * width + x;
            density[i] = Mathf.Min(1f, density[i] + amount);
        }

        public void Line(float x0, float y0, float x1, float y1, float amount)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0))));
            int lastX = int.MinValue, lastY = int.MinValue;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
                if (x == lastX && y == lastY) continue; // don't double-count a pixel
                Add(x, y, amount);
                lastX = x;
                lastY = y;
            }
        }

        public void Rect(int x0, int y0, int x1, int y1, float amount)
        {
            Line(x0, y0, x1, y0, amount);
            Line(x0, y1, x1, y1, amount);
            Line(x0, y0 + 1, x0, y1 - 1, amount);
            Line(x1, y0 + 1, x1, y1 - 1, amount);
        }

        public void FillRect(int x0, int y0, int x1, int y1, float amount)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    Add(x, y, amount);
        }

        public void Disc(float cx, float cy, float r, float amount) => FillEllipse(cx, cy, r, r, amount);

        public void Ring(float cx, float cy, float r, float thickness, float amount) => Ellipse(cx, cy, r, r, thickness, amount);

        public void FillEllipse(float cx, float cy, float rx, float ry, float amount)
        {
            for (int y = Mathf.FloorToInt(cy - ry); y <= Mathf.CeilToInt(cy + ry); y++)
                for (int x = Mathf.FloorToInt(cx - rx); x <= Mathf.CeilToInt(cx + rx); x++)
                {
                    float dx = (x - cx) / rx, dy = (y - cy) / ry;
                    if (dx * dx + dy * dy <= 1f) Add(x, y, amount);
                }
        }

        public void Ellipse(float cx, float cy, float rx, float ry, float thickness, float amount)
        {
            for (int y = Mathf.FloorToInt(cy - ry); y <= Mathf.CeilToInt(cy + ry); y++)
                for (int x = Mathf.FloorToInt(cx - rx); x <= Mathf.CeilToInt(cx + rx); x++)
                {
                    float dx = (x - cx) / rx, dy = (y - cy) / ry;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float inner = 1f - thickness / Mathf.Min(rx, ry);
                    if (d <= 1f && d >= inner) Add(x, y, amount);
                }
        }

        // Fills the inside faintly and traces the outline more strongly.
        public void Polygon(Vector2[] points, float fill, float edge)
        {
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (Inside(points, x + 0.5f, y + 0.5f)) Add(x, y, fill);
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 a = points[i], b = points[(i + 1) % points.Length];
                Line(a.x, a.y, b.x, b.y, edge);
            }
        }

        static bool Inside(Vector2[] points, float x, float y)
        {
            bool inside = false;
            for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
            {
                Vector2 a = points[i], b = points[j];
                if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }
            return inside;
        }

        // White pixels whose transparency is the density. The game tints them.
        // A blurred copy is added on top so dense parts bloom like a real scan.
        public ArtGenerator.PixelCanvas ToPixels()
        {
            float[] glow = Blur(Blur(density));
            var pixels = new ArtGenerator.PixelCanvas(width, height);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int i = y * width + x;
                    pixels.Set(x, y, new Color(1f, 1f, 1f, Mathf.Min(1f, density[i] * 0.85f + glow[i] * 0.45f)));
                }
            return pixels;
        }

        // 3x3 box blur.
        float[] Blur(float[] source)
        {
            var result = new float[source.Length];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float sum = 0f;
                    int count = 0;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                            sum += source[ny * width + nx];
                            count++;
                        }
                    result[y * width + x] = sum / count;
                }
            return result;
        }
    }
}
