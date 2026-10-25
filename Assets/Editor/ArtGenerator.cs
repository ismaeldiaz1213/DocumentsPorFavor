using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Menu "Documents Por Favor > Generate And Apply Art".
// Draws the game's pixel art in code (every color comes from Palette), saves it as
// PNGs in Assets/Art/Generated, and swaps the placeholder shapes for the new sprites.
// Safe to run again.
//
// Scale: 18 art pixels = 1 world unit. The camera sees 10 units (180 px) tall,
// and 1080 / 180 = 6, so every art pixel is a crisp 6x6 square on screen.
public static class ArtGenerator
{
    public const string Folder = "Assets/Art/Generated";
    const int PixelsPerUnit = 18;

    // A few extra colors that only the art needs (all sit inside the palette's mood).
    static readonly Color Glow = new Color32(0xBF, 0xE6, 0xF0, 0xFF);
    static readonly Color Sun = new Color32(0xF4, 0xE9, 0xC8, 0xFF);
    static readonly Color Skin = new Color32(0xC6, 0x9C, 0x72, 0xFF);
    static readonly Color Shirt = new Color32(0xD9, 0xCF, 0xB8, 0xFF);
    static readonly Color Hat = new Color32(0x5A, 0x46, 0x36, 0xFF);
    static readonly Color TruckYellow = new Color32(0xD6, 0xA8, 0x3E, 0xFF);
    static readonly Color TruckShadow = new Color32(0xA9, 0x81, 0x2C, 0xFF);

    [MenuItem("Documents Por Favor/Generate And Apply Art")]
    public static void GenerateAndApply()
    {
        Directory.CreateDirectory(Folder);
        AssetDatabase.DeleteAsset(Folder + "/car_body.png");    // replaced by per-vehicle sprites
        AssetDatabase.DeleteAsset(Folder + "/car_details.png");

        Sprite backdrop = Save("backdrop", DrawBackdrop(), new Vector2(0f, 0f));
        Sprite scanner = Save("scanner", DrawScanner(), new Vector2(0.5f, 0f));
        Sprite booth = Save("booth", DrawBooth(), new Vector2(0.5f, 0f));
        Sprite person = Save("person", DrawPerson(), new Vector2(0.5f, 0f));
        Sprite towTruck = Save("tow_truck", DrawTowTruck(), new Vector2(0.5f, 3.5f / 20f));
        Sprite deskWood = Save("desk_wood", DrawDeskWood(), new Vector2(0.5f, 0.5f));

        // Each vehicle: a grayscale body (tinted per traveler) and an untinted detail layer.
        // The pivot sits on the wheel centers, so every vehicle drives on the same line.
        Sprite sedanBody = SaveVehicle("Sedan", DrawSedan(false), DrawSedan(true), 17, out Sprite sedanDetails);
        SaveVehicle("Pickup", DrawPickup(false), DrawPickup(true), 18, out _);
        SaveVehicle("Van", DrawVan(false), DrawVan(true), 22, out _);
        XRayArt.Generate();

        ApplyToScene(backdrop, scanner, booth, towTruck, deskWood);
        ApplyToCarPrefab(sedanBody, sedanDetails, person);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Art generated and applied. Press Ctrl+S to save the scene.");
    }

    static Sprite SaveVehicle(string model, PixelCanvas body, PixelCanvas details, int height, out Sprite detailSprite)
    {
        var pivot = new Vector2(0.5f, 3.5f / height);
        Sprite bodySprite = Save(model.ToLower() + "_body", body, pivot);
        detailSprite = Save(model.ToLower() + "_details", details, pivot);

        VehicleModel asset = Day1StoryWriter.Vehicle(model);
        asset.bodySprite = bodySprite;
        asset.detailSprite = detailSprite;
        EditorUtility.SetDirty(asset);
        return bodySprite;
    }

    // ---------- Applying ----------

    static void ApplyToScene(Sprite backdrop, Sprite scanner, Sprite booth, Sprite towTruck, Sprite deskWood)
    {
        Transform world = FindInScene("World");
        if (world == null)
        {
            Debug.LogError("Generate Art: no 'World' object in the scene.");
            return;
        }

        foreach (string old in new[] { "Sky", "Sand", "Road", "Cactus1", "Cactus2",
                     "ScannerPillarL", "ScannerPillarR", "ScannerTop", "Booth", "BoothRoof", "BoothWindow" })
        {
            Transform t = FindInScene(old);
            if (t != null) t.gameObject.SetActive(false);
        }

        MakeSpriteObject("ArtBackdrop", world, backdrop, new Vector3(-160f / PixelsPerUnit, 0f, 0f), -20);
        MakeSpriteObject("ArtScanner", world, scanner, new Vector3(-2f, 1.0f, 0f), 10);
        MakeSpriteObject("ArtBooth", world, booth, new Vector3(1.6f, 1.95f, 0f), -5);

        Transform beam = FindInScene("ScanBeam");
        if (beam != null)
        {
            beam.GetComponent<SpriteRenderer>().color = Palette.WithAlpha(Glow, 0.6f);
            beam.localScale = new Vector3(0.06f, 1.5f, 1f);
        }

        Transform truck = FindInScene("TowTruck");
        if (truck != null)
        {
            for (int i = truck.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(truck.GetChild(i).gameObject);
            MakeSpriteObject("Sprite", truck, towTruck, Vector3.zero, 2);
        }

        CheckpointStage stage = Object.FindFirstObjectByType<CheckpointStage>();
        if (stage != null)
        {
            var so = new SerializedObject(stage);
            so.FindProperty("hitchDistance").floatValue = 2.75f; // the new sprites are a bit longer
            so.FindProperty("queueSpacing").floatValue = 3.1f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        Transform surface = FindInScene("DeskSurface");
        if (surface != null)
        {
            Image image = surface.GetComponent<Image>();
            image.sprite = deskWood;
            image.color = Color.white;
            foreach (Transform t in surface.parent)
                if (t.name == "Grain") t.gameObject.SetActive(false);
        }
    }

    static void ApplyToCarPrefab(Sprite body, Sprite details, Sprite person)
    {
        const string path = "Assets/Prefabs/Car.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);

        foreach (string old in new[] { "Cabin", "WheelBack", "WheelFront" })
        {
            Transform t = root.transform.Find(old);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }

        Transform bodyT = root.transform.Find("Body");
        bodyT.localPosition = Vector3.zero;
        bodyT.localScale = Vector3.one;
        bodyT.GetComponent<SpriteRenderer>().sprite = body;

        Transform detailsT = root.transform.Find("Details");
        if (detailsT == null)
            detailsT = MakeSpriteObject("Details", root.transform, details, Vector3.zero, 1).transform;
        else
            detailsT.GetComponent<SpriteRenderer>().sprite = details;

        var carView = new SerializedObject(root.GetComponent<CarView>());
        carView.FindProperty("details").objectReferenceValue = detailsT.GetComponent<SpriteRenderer>();
        carView.ApplyModifiedPropertiesWithoutUndo();

        Transform driver = root.transform.Find("Driver");
        driver.localPosition = new Vector3(0.35f, -3.5f / PixelsPerUnit, 0f);
        driver.localScale = Vector3.one;
        SpriteRenderer driverRenderer = driver.GetComponent<SpriteRenderer>();
        driverRenderer.sprite = person;
        driverRenderer.color = Color.white;

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static GameObject MakeSpriteObject(string name, Transform parent, Sprite sprite, Vector3 localPosition, int order)
    {
        Transform existing = parent.Find(name);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        return go;
    }

    // Finds objects even if they are switched off.
    static Transform FindInScene(string name)
    {
        foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
        return null;
    }

    internal static Sprite Save(string name, PixelCanvas canvas, Vector2 pivot)
    {
        string path = $"{Folder}/{name}.png";
        File.WriteAllBytes(path, canvas.ToTexture().EncodeToPNG());
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);

        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ---------- Drawing: world ----------

    static PixelCanvas DrawBackdrop()
    {
        const int width = 320, height = 90;
        const int horizon = 52, roadBottom = 14, roadTop = 43;
        var c = new PixelCanvas(width, height);
        var random = new System.Random(7);

        // Sky: dusty blue fading to pale yellow at the horizon.
        for (int y = horizon; y < height; y++)
            for (int x = 0; x < width; x++)
                c.Set(x, y, PixelCanvas.Dither(Palette.SkyHorizon, Palette.SkyTop, (y - horizon) / (float)(height - 1 - horizon), x, y, 6));

        // Low, hazy sun.
        for (int y = 58; y < 84; y++)
            for (int x = 240; x < 272; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(256f, 71f));
                if (d <= 6f) c.Set(x, y, Sun);
                else if (d <= 11f && PixelCanvas.Bayer(x, y) < 0.5f * (1f - (d - 6f) / 5f)) c.Set(x, y, Sun);
            }

        // Far mountains.
        for (int x = 0; x < width; x++)
        {
            int top = horizon + 4 + Mathf.RoundToInt(4f * Mathf.Sin(x * 0.045f) + 2.5f * Mathf.Sin(x * 0.11f + 1.3f) + 1.5f * Mathf.Sin(x * 0.23f + 0.5f));
            for (int y = horizon; y <= top; y++)
                c.Set(x, y, y >= top - 1 ? Color.Lerp(Palette.FarMountains, Palette.SkyTop, 0.3f) : Palette.FarMountains);
        }

        DrawMesa(c, 86, 134, horizon, 63);
        DrawMesa(c, 220, 252, horizon, 59);

        // Distant border fence along the horizon.
        Color fence = Color.Lerp(Palette.Metal, Palette.FarMountains, 0.4f);
        c.Fill(0, horizon + 2, width - 1, horizon + 2, fence);
        for (int x = 0; x < width; x += 4)
            c.Fill(x, horizon - 1, x, horizon + 3, fence);

        // Sand above and below the road.
        c.Fill(0, roadTop + 1, width - 1, horizon - 2, Palette.Sand);
        c.Fill(0, 0, width - 1, roadBottom - 1, Palette.Sand);
        Speckle(c, random, 0, 0, width - 1, horizon - 2, Palette.SandShadow, 0.07f);
        c.Fill(0, roadBottom - 2, width - 1, roadBottom - 1, Palette.SandShadow); // shoulder

        // Road with faded edge lines and a dashed center line.
        c.Fill(0, roadBottom, width - 1, roadTop, Palette.Road);
        Speckle(c, random, 0, roadBottom, width - 1, roadTop, Color.Lerp(Palette.Road, Palette.Ink, 0.18f), 0.08f);
        Color edge = Color.Lerp(Palette.Road, Palette.RoadLine, 0.55f);
        c.Fill(0, roadBottom + 1, width - 1, roadBottom + 1, edge);
        c.Fill(0, roadTop - 1, width - 1, roadTop - 1, edge);
        for (int x = 0; x < width; x++)
            if ((x / 7) % 2 == 0) c.Fill(x, 28, x, 29, Palette.RoadLine);

        // Telephone poles with sagging wire. Lonely.
        Color pole = Color.Lerp(Palette.MesaShadow, Palette.Ink, 0.45f);
        int[] poles = { 22, 150, 284 };
        foreach (int x in poles)
        {
            c.Fill(x, roadTop + 2, x, 74, pole);
            c.Fill(x - 3, 71, x + 3, 71, pole);
        }
        Color wire = Color.Lerp(pole, Palette.SkyTop, 0.35f);
        for (int i = 0; i < poles.Length - 1; i++)
            for (int x = poles[i]; x <= poles[i + 1]; x++)
            {
                float t = (x - poles[i]) / (float)(poles[i + 1] - poles[i]);
                c.Set(x, 71 - Mathf.RoundToInt(16f * t * (1f - t)), wire);
            }

        // Cacti behind the road, and a couple in front.
        DrawSaguaro(c, 64, roadTop + 1, 13);
        DrawSaguaro(c, 204, roadTop + 1, 9);
        DrawSaguaro(c, 302, roadTop + 2, 11);
        DrawSaguaro(c, 14, 1, 8);
        DrawSaguaro(c, 238, 2, 6);

        return c;
    }

    static void DrawMesa(PixelCanvas c, int x0, int x1, int baseY, int topY)
    {
        for (int y = baseY; y <= topY; y++)
        {
            int rise = y - baseY;
            int left = x0 + rise / 2;
            int right = x1 - rise / 3;
            int shadowStart = right - (right - left) / 3;
            c.Fill(left, y, shadowStart, y, Palette.Mesa);
            c.Fill(shadowStart + 1, y, right, y, Palette.MesaShadow);
        }
        c.Fill(x0 + (topY - baseY) / 2, topY, x1 - (topY - baseY) / 3, topY, Color.Lerp(Palette.Mesa, Palette.SkyHorizon, 0.35f));
    }

    static void DrawSaguaro(PixelCanvas c, int x, int baseY, int height)
    {
        c.Fill(x, baseY, x + 2, baseY + height, Palette.Cactus);
        c.Fill(x + 2, baseY, x + 2, baseY + height, Palette.CactusDark);
        c.Set(x + 1, baseY + height + 1, Palette.Cactus);

        int leftArm = baseY + height / 2;
        c.Fill(x - 2, leftArm, x - 1, leftArm, Palette.Cactus);
        c.Fill(x - 2, leftArm, x - 2, leftArm + 3, Palette.Cactus);

        int rightArm = baseY + height / 3;
        c.Fill(x + 3, rightArm, x + 4, rightArm, Palette.CactusDark);
        c.Fill(x + 4, rightArm, x + 4, rightArm + 4, Palette.CactusDark);
    }

    static void Speckle(PixelCanvas c, System.Random random, int x0, int y0, int x1, int y1, Color color, float density)
    {
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                if (random.NextDouble() < density) c.Set(x, y, color);
    }

    static PixelCanvas DrawScanner()
    {
        var c = new PixelCanvas(56, 34);
        Color metalLight = Color.Lerp(Palette.Metal, Palette.Paper, 0.25f);

        foreach (int x0 in new[] { 0, 52 })
        {
            c.Fill(x0, 0, x0 + 3, 29, Palette.Metal);
            c.Fill(x0, 0, x0, 29, metalLight);
            for (int y = 0; y <= 10; y++)
                for (int x = x0; x <= x0 + 3; x++)
                    c.Set(x, y, ((x + y) / 2) % 2 == 0 ? Palette.Warning : Palette.Ink);
        }

        c.Fill(0, 28, 55, 32, Palette.Metal);
        c.Fill(0, 32, 55, 32, metalLight);
        c.Fill(0, 28, 55, 28, Palette.Ink);
        for (int x = 6; x < 52; x += 6)
            c.Fill(x, 27, x + 1, 27, Glow);
        c.Fill(26, 33, 29, 33, Palette.Reject);
        return c;
    }

    static PixelCanvas DrawBooth()
    {
        var c = new PixelCanvas(36, 34);

        // Concrete island in the road.
        c.Fill(0, 0, 35, 2, Palette.ConcreteShadow);
        c.Fill(0, 2, 35, 2, Palette.Concrete);

        // Walls.
        c.Fill(5, 3, 30, 24, Palette.Concrete);
        c.Fill(27, 3, 30, 24, Palette.ConcreteShadow);

        // Window with a glint.
        c.Fill(8, 12, 27, 22, Palette.Ink);
        c.Fill(9, 13, 26, 21, Palette.Glass);
        for (int i = 0; i < 6; i++) c.Set(12 + i, 14 + i, Color.Lerp(Palette.Glass, Palette.Paper, 0.5f));

        // Door.
        c.Fill(9, 3, 14, 10, Palette.Metal);
        c.Set(13, 6, Palette.Warning);

        // Roof overhang with a shadow underneath.
        c.Fill(2, 25, 33, 28, Palette.Metal);
        c.Fill(2, 28, 33, 28, Color.Lerp(Palette.Metal, Palette.Paper, 0.3f));
        c.Fill(5, 24, 30, 24, Color.Lerp(Palette.Concrete, Palette.Ink, 0.4f));

        // Little sign and warning light.
        c.Fill(10, 29, 25, 32, Palette.Paper);
        c.Fill(10, 29, 25, 29, Palette.Reject);
        c.Fill(12, 31, 23, 31, Palette.PaperInk);
        c.Fill(31, 29, 31, 31, Palette.Warning);
        return c;
    }

    // ---------- Drawing: vehicles ----------
    // Every vehicle faces RIGHT. The body layer is grayscale so it can be tinted
    // with the traveler's car color; the details layer keeps its own colors.

    static Color Gray(float value) => new Color(value, value, value);

    // Shading bands from the rocker panel (dark) up to the shoulder (light).
    static Color BodyShade(int y, int top)
    {
        if (y <= 2) return Gray(0.58f);
        if (y <= 4) return Gray(0.72f);
        if (y >= top) return Color.white;
        if (y >= top - 1) return Gray(0.95f);
        return Gray(0.86f);
    }

    static void CutWheelArch(PixelCanvas c, float x, float radius) => c.Circle(x, 3f, radius, Color.clear);

    static void DrawWheel(PixelCanvas c, float x, float radius)
    {
        Color rimLight = Color.Lerp(Palette.Metal, Palette.Paper, 0.55f);
        int cx = Mathf.RoundToInt(x);
        c.Circle(x, 3f, radius, Palette.Ink);                                  // tire
        c.Circle(x, 3f, radius - 1.3f, rimLight);                              // rim
        c.Circle(x, 3f, radius - 2.2f, Palette.Metal);                         // hub well
        c.Set(cx, 3, Palette.Paper);                                           // hub cap
        c.Set(cx - 1, 3 + Mathf.RoundToInt(radius), Color.Lerp(Palette.Ink, Palette.Paper, 0.3f)); // tire shine
    }

    static void Chrome(PixelCanvas c, int x0, int y0, int x1, int y1)
    {
        c.Fill(x0, y0, x1, y1, Palette.Metal);
        c.Fill(x0, y1, x1, y1, Color.Lerp(Palette.Metal, Palette.Paper, 0.5f));
    }

    static void GlassRow(PixelCanvas c, int x0, int x1, int y, bool topRow)
    {
        c.Fill(x0, y, x1, y, topRow ? Color.Lerp(Palette.Glass, Palette.Paper, 0.3f) : Palette.Glass);
    }

    static PixelCanvas DrawSedan(bool details)
    {
        var c = new PixelCanvas(44, 17);
        const int shoulder = 8;
        float[] wheels = { 10f, 34f };

        // Greenhouse edges per row: rear window slopes in from the left, windshield from the right.
        int GreenLeft(int y) => 9 + (y - 9);
        int GreenRight(int y) => 33 - (y - 9);

        if (!details)
        {
            for (int y = 2; y <= shoulder; y++)
            {
                int left = y <= 3 ? 2 : 1;
                int right = y <= 3 ? 41 : 42;
                c.Fill(left, y, right, y, BodyShade(y, shoulder));
            }
            c.Fill(31, shoulder, 41, shoulder, Gray(0.97f));                  // hood
            for (int y = 9; y <= 13; y++)
                c.Fill(GreenLeft(y), y, GreenRight(y), y, Gray(0.9f));        // pillars and roof
            c.Fill(14, 14, 27, 14, Color.white);                              // roof highlight
            c.Fill(3, 6, 40, 6, Gray(0.78f));                                 // body crease
            c.Fill(11, 3, 11, shoulder, Gray(0.5f));                          // door seams
            c.Fill(21, 3, 21, 13, Gray(0.5f));
            c.Fill(31, 4, 31, shoulder, Gray(0.5f));

            foreach (float w in wheels) CutWheelArch(c, w, 4.6f);
            c.Outline(Gray(0.36f));
        }
        else
        {
            for (int y = 10; y <= 13; y++)
            {
                GlassRow(c, GreenLeft(y) + 1, 20, y, y == 13);                // rear side window
                GlassRow(c, 22, GreenRight(y) - 1, y, y == 13);               // front side window
            }
            c.Set(23, 12, Palette.Paper);                                     // glints
            c.Set(13, 11, Color.Lerp(Palette.Glass, Palette.Paper, 0.6f));

            Color chromeLight = Color.Lerp(Palette.Metal, Palette.Paper, 0.6f);
            c.Set(18, 7, chromeLight);                                        // door handles
            c.Set(29, 7, chromeLight);
            c.Fill(31, 9, 32, 10, Palette.Metal);                             // side mirror

            c.Fill(41, 6, 43, 7, Palette.Warning);                            // headlight
            c.Set(43, 5, Palette.Ink);                                        // grille
            Chrome(c, 40, 2, 43, 4);                                          // front bumper
            c.Fill(0, 6, 1, 7, Palette.Reject);                               // taillight
            Chrome(c, 0, 2, 3, 4);                                            // rear bumper
            c.Set(3, 1, Palette.Metal);                                       // exhaust
            c.Fill(6, 15, 6, 16, Palette.Metal);                              // antenna

            foreach (float w in wheels) DrawWheel(c, w, 3.5f);
        }
        return c;
    }

    static PixelCanvas DrawPickup(bool details)
    {
        var c = new PixelCanvas(48, 18);
        const int shoulder = 9;
        float[] wheels = { 10f, 38f };
        int CabRight(int y) => 40 - (y - 10) / 2; // windshield slope

        if (!details)
        {
            for (int y = 2; y <= shoulder; y++)
            {
                int left = y <= 3 ? 2 : 1;
                int right = y <= 3 || y >= 8 ? 45 : 46;
                c.Fill(left, y, right, y, BodyShade(y, shoulder));
            }
            c.Fill(39, shoulder, 45, shoulder, Gray(0.97f));                  // hood
            for (int y = 10; y <= 15; y++)
                c.Fill(28, y, CabRight(y), y, Gray(0.9f));                    // cab
            c.Fill(28, 15, 37, 15, Color.white);                              // cab roof
            c.Fill(1, shoulder, 26, shoulder, Color.white);                   // bed rail cap
            c.Fill(27, 3, 27, 15, Gray(0.42f));                               // gap between bed and cab
            c.Fill(2, 3, 2, 8, Gray(0.55f));                                  // tailgate seam
            c.Fill(38, 3, 38, shoulder, Gray(0.5f));                          // door seam
            c.Fill(3, 6, 44, 6, Gray(0.78f));                                 // body crease

            foreach (float w in wheels) CutWheelArch(c, w, 4.9f);
            c.Outline(Gray(0.36f));
        }
        else
        {
            for (int y = 11; y <= 14; y++)
                GlassRow(c, 30, CabRight(y) - 1, y, y == 14);                // cab window
            c.Set(32, 13, Palette.Paper);

            Color chromeLight = Color.Lerp(Palette.Metal, Palette.Paper, 0.6f);
            c.Set(35, 8, chromeLight);                                        // door handle
            c.Set(3, 7, chromeLight);                                         // tailgate handle
            c.Fill(41, 10, 42, 11, Palette.Metal);                            // side mirror
            c.Fill(30, 16, 31, 16, Palette.Warning);                          // roof marker lights
            c.Fill(34, 16, 35, 16, Palette.Warning);

            c.Fill(46, 6, 47, 7, Palette.Warning);                            // headlight
            Chrome(c, 44, 2, 47, 4);                                          // front bumper
            c.Fill(0, 6, 1, 8, Palette.Reject);                               // taillight
            Chrome(c, 0, 2, 3, 4);                                            // rear bumper
            c.Fill(15, 1, 16, 4, Palette.Ink);                                // mud flap

            foreach (float w in wheels) DrawWheel(c, w, 3.7f);
        }
        return c;
    }

    static PixelCanvas DrawVan(bool details)
    {
        var c = new PixelCanvas(48, 22);
        float[] wheels = { 10f, 37f };
        int WindshieldRight(int y) => 46 - (y - 12);

        if (!details)
        {
            for (int y = 2; y <= 20; y++)
            {
                Color shade = y <= 2 ? Gray(0.58f) : y <= 4 ? Gray(0.72f) : y >= 19 ? Color.white : Gray(0.88f - (20 - y) * 0.004f);
                c.Fill(y <= 3 ? 2 : 1, y, 40, y, shade);                      // cargo box
            }
            for (int y = 2; y <= 11; y++)
                c.Fill(41, y, y <= 3 ? 45 : 46, y, BodyShade(y, 11));         // nose
            for (int y = 12; y <= 18; y++)
                c.Fill(41, y, WindshieldRight(y), y, Gray(0.9f));             // cab pillar
            c.Fill(2, 3, 2, 19, Gray(0.6f));                                  // rear doors
            c.Fill(26, 3, 26, 17, Gray(0.6f));                                // sliding door
            c.Fill(34, 3, 34, 17, Gray(0.6f));
            c.Fill(3, 8, 39, 8, Gray(0.78f));                                 // body crease

            foreach (float w in wheels) CutWheelArch(c, w, 4.6f);
            c.Outline(Gray(0.36f));
        }
        else
        {
            for (int y = 12; y <= 17; y++)
            {
                GlassRow(c, 36, 39, y, y == 17);                              // cab side window
                GlassRow(c, 42, WindshieldRight(y) - 1, y, y == 17);          // windshield edge
            }
            c.Set(37, 16, Palette.Paper);

            // Mango logo with a leaf, over a green stripe: "MANGOS TREVINO".
            c.Fill(4, 7, 39, 7, Palette.Approve);
            c.Circle(16f, 13f, 3.2f, Palette.Warning);
            c.Set(15, 14, Color.Lerp(Palette.Warning, Palette.Paper, 0.6f));
            c.Fill(17, 16, 19, 17, Palette.Cactus);

            Color chromeLight = Color.Lerp(Palette.Metal, Palette.Paper, 0.6f);
            c.Set(25, 11, chromeLight);                                       // sliding door handle
            c.Set(4, 11, chromeLight);                                        // rear door handle
            c.Fill(41, 13, 42, 14, Palette.Metal);                            // side mirror

            c.Fill(46, 7, 47, 8, Palette.Warning);                            // headlight
            Chrome(c, 44, 2, 47, 4);                                          // front bumper
            c.Fill(0, 6, 1, 10, Palette.Reject);                              // tall taillight
            Chrome(c, 0, 2, 3, 4);                                            // rear bumper

            foreach (float w in wheels) DrawWheel(c, w, 3.5f);
        }
        return c;
    }

    // A little traveler in a cowboy hat.
    static PixelCanvas DrawPerson()
    {
        var c = new PixelCanvas(7, 14);
        c.Fill(2, 0, 2, 3, Palette.RegistrationBlue); // legs
        c.Fill(4, 0, 4, 3, Palette.RegistrationBlue);
        c.Fill(2, 3, 4, 3, Palette.RegistrationBlue);
        c.Fill(1, 4, 5, 8, Shirt);                    // shirt and arms
        c.Fill(0, 5, 0, 7, Shirt);
        c.Fill(6, 5, 6, 7, Shirt);
        c.Fill(2, 9, 4, 11, Skin);                    // head
        c.Fill(0, 11, 6, 11, Hat);                    // hat brim
        c.Fill(2, 12, 4, 13, Hat);                    // hat crown
        return c;
    }

    // Faces LEFT (it drives in from the right). The script flips it to tow.
    static PixelCanvas DrawTowTruck()
    {
        var c = new PixelCanvas(46, 20);
        c.Fill(0, 3, 2, 8, TruckYellow);              // hood
        c.Fill(2, 3, 14, 14, TruckYellow);            // cab
        c.Fill(4, 9, 11, 13, Palette.Glass);          // window
        c.Fill(15, 3, 43, 7, TruckShadow);            // bed
        c.Fill(15, 7, 43, 7, TruckYellow);
        c.Fill(7, 15, 8, 15, Palette.Warning);        // beacon
        c.Set(9, 15, Palette.Reject);

        // Boom and hook.
        for (int i = 0; i <= 12; i++)
            c.Fill(30 + i, 8 + (i * 8) / 12, 31 + i, 8 + (i * 8) / 12, Palette.Metal);
        c.Fill(43, 9, 43, 15, Palette.Ink);
        c.Fill(42, 9, 44, 9, Palette.Ink);

        c.Outline(Color.Lerp(TruckShadow, Palette.Ink, 0.5f));

        foreach (int x in new[] { 8, 36 })
        {
            c.Circle(x, 3f, 3.2f, Palette.Ink);
            c.Set(x, 3, Palette.Metal);
        }
        return c;
    }

    // ---------- Drawing: desk ----------

    static PixelCanvas DrawDeskWood()
    {
        var c = new PixelCanvas(320, 90);
        var random = new System.Random(11);
        Color grainDark = Color.Lerp(Palette.DeskWood, Palette.DeskWoodDark, 0.5f);
        Color grainLight = Color.Lerp(Palette.DeskWood, Palette.Paper, 0.08f);

        c.Fill(0, 0, 319, 89, Palette.DeskWood);

        // Planks.
        for (int plank = 0; plank < 4; plank++)
        {
            int y0 = plank * 22;
            c.Fill(0, y0, 319, y0, Palette.DeskWoodDark);
            // Long horizontal grain streaks.
            for (int s = 0; s < 14; s++)
            {
                int y = y0 + 2 + random.Next(19);
                int x = random.Next(320);
                int length = 20 + random.Next(70);
                Color color = random.NextDouble() < 0.6 ? grainDark : grainLight;
                for (int i = 0; i < length; i++)
                    c.Set((x + i) % 320, y, color);
            }
            // Plank joint.
            int joint = random.Next(40, 280);
            c.Fill(joint, y0, joint, y0 + 21, Palette.DeskWoodDark);
        }

        return c;
    }

    // ---------- Tiny drawing toolkit ----------

    internal class PixelCanvas
    {
        static readonly int[,] BayerMatrix = { { 0, 8, 2, 10 }, { 12, 4, 14, 6 }, { 3, 11, 1, 9 }, { 15, 7, 13, 5 } };

        readonly int width;
        readonly int height;
        readonly Color[] pixels;

        public PixelCanvas(int width, int height)
        {
            this.width = width;
            this.height = height;
            pixels = new Color[width * height];
        }

        public void Set(int x, int y, Color color)
        {
            if (x >= 0 && y >= 0 && x < width && y < height)
                pixels[y * width + x] = color;
        }

        Color Get(int x, int y) => pixels[y * width + x];

        public void Fill(int x0, int y0, int x1, int y1, Color color)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    Set(x, y, color);
        }

        public void Circle(float cx, float cy, float radius, Color color)
        {
            for (int y = Mathf.FloorToInt(cy - radius); y <= Mathf.CeilToInt(cy + radius); y++)
                for (int x = Mathf.FloorToInt(cx - radius); x <= Mathf.CeilToInt(cx + radius); x++)
                    if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= radius * radius)
                        Set(x, y, color);
        }

        // Paints a 1-pixel border around every opaque area.
        public void Outline(Color color)
        {
            var edges = new List<int>();
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    if (Get(x, y).a < 0.5f) continue;
                    bool edge = x == 0 || y == 0 || x == width - 1 || y == height - 1
                                || Get(x - 1, y).a < 0.5f || Get(x + 1, y).a < 0.5f
                                || Get(x, y - 1).a < 0.5f || Get(x, y + 1).a < 0.5f;
                    if (edge) edges.Add(y * width + x);
                }
            foreach (int i in edges) pixels[i] = color;
        }

        // 0..1 threshold from a 4x4 ordered-dither pattern.
        public static float Bayer(int x, int y) => (BayerMatrix[y & 3, x & 3] + 0.5f) / 16f;

        // A gradient with a few flat color bands, blended with an ordered-dither
        // pattern: the classic pixel-art look.
        public static Color Dither(Color from, Color to, float t, int x, int y, int bands)
        {
            float scaled = Mathf.Clamp01(t) * (bands - 1);
            int band = Mathf.FloorToInt(scaled);
            if (scaled - band > Bayer(x, y)) band++;
            return Color.Lerp(from, to, Mathf.Min(band, bands - 1) / (float)(bands - 1));
        }

        public Texture2D ToTexture()
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
