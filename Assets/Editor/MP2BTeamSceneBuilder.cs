using System.IO;
using ArcaneWorkshop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MP2BTeamSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/ArcaneWorkshop.unity";
    private const string RootName = "MP2B Team Expansion";
    private const string MaterialFolder = "Assets/Materials";
    private const string KayKitFolder = "Assets/ThirdParty/KayKitDungeon/Models";
    private const string NatureFolder = "Assets/ThirdParty/KenneyNature/Models";

    [MenuItem("MP2B/Build Team Expansion Scene")]
    public static void BuildTeamExpansionScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            Debug.LogWarning("Exit Play Mode and wait for compilation before building the MP2B team scene.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (scene.isDirty)
            {
                Debug.LogError("Save or discard the current scene changes before opening the MP2B workshop scene.");
                return;
            }
            if (!File.Exists(ScenePath))
            {
                Debug.LogError("The Arcane Workshop scene does not exist yet.");
                return;
            }
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        ArcaneGameManager manager = Object.FindAnyObjectByType<ArcaneGameManager>();
        if (manager == null)
        {
            Debug.LogError("ArcaneGameManager is missing from the workshop scene.");
            return;
        }

        BuildIntoScene(manager);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Selection.activeObject = GameObject.Find(RootName);
        Debug.Log("MP2B team expansion scene built: Conservatory, Astral Foundry, and Convergence Nexus are now authored scene objects.");
    }

    [MenuItem("MP2B/Capture Team Expansion Preview")]
    public static void CaptureTeamExpansionPreview()
    {
        Camera camera = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
        if (camera == null)
        {
            Debug.LogError("No camera is available for the MP2B team preview.");
            return;
        }

        const int width = 1600;
        const int height = 900;
        Vector3 oldPosition = camera.transform.position;
        Quaternion oldRotation = camera.transform.rotation;
        float oldFieldOfView = camera.fieldOfView;
        RenderTexture oldTarget = camera.targetTexture;
        RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            camera.transform.position = new Vector3(0f, 15.5f, -20.5f);
            camera.transform.LookAt(new Vector3(0f, 0.65f, 0.8f));
            camera.fieldOfView = 58f;
            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture.active = renderTexture;
            image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            image.Apply();

            Directory.CreateDirectory("Documentation");
            File.WriteAllBytes("Documentation/MP2B_TEAM_PREVIEW.png", image.EncodeToPNG());
            Debug.Log("MP2B team expansion preview captured at Documentation/MP2B_TEAM_PREVIEW.png");
        }
        finally
        {
            RenderTexture.active = null;
            camera.targetTexture = oldTarget;
            camera.transform.SetPositionAndRotation(oldPosition, oldRotation);
            camera.fieldOfView = oldFieldOfView;
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(renderTexture);
        }
    }

    public static void BuildIntoScene(ArcaneGameManager manager)
    {
        GameObject existing = GameObject.Find(RootName);
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        Material rune = LoadMaterial("Rune");
        Material herb = LoadMaterial("MoonHerb");
        Material crystal = LoadMaterial("Crystal");
        Material metal = LoadMaterial("DarkMetal");
        Material mana = LoadMaterial("Mana");
        Material floor = LoadMaterial("Floor");
        Material flow = LoadMaterial("AnimatedArcaneFlow");

        GameObject root = new GameObject(RootName);
        CreateWorldIdentity(root.transform, rune, metal);
        CreateRunePaths(root.transform, rune, metal);
        CreateConservatory(root.transform, manager, herb, mana, metal, floor);
        CreateFoundry(root.transform, manager, crystal, rune, metal, flow);
        CreateNexus(root.transform, manager, rune, crystal, mana, metal, flow);
    }

    private static void CreateWorldIdentity(Transform parent, Material rune, Material metal)
    {
        GameObject titleRoot = new GameObject("MP2B World Title");
        titleRoot.transform.SetParent(parent, false);
        titleRoot.transform.localPosition = new Vector3(0f, 4.6f, 8.05f);

        CreatePrimitive("Title Backplate", PrimitiveType.Cube, titleRoot.transform, Vector3.zero,
            new Vector3(8.8f, 1.05f, 0.16f), metal);
        CreatePrimitive("Title Rune Left", PrimitiveType.Cube, titleRoot.transform, new Vector3(-4.55f, 0f, -0.12f),
            new Vector3(0.18f, 1.25f, 0.18f), rune);
        CreatePrimitive("Title Rune Right", PrimitiveType.Cube, titleRoot.transform, new Vector3(4.55f, 0f, -0.12f),
            new Vector3(0.18f, 1.25f, 0.18f), rune);

        TextMesh title = CreateLabel(titleRoot.transform,
            "MP2B — ARCANE WORKSHOP: REAWAKENING\nTHREE DISCIPLINES • ONE RESTORATION",
            new Vector3(0f, 0.12f, -0.13f), 0.055f, 62, TextAnchor.MiddleCenter);
        title.color = new Color(0.78f, 0.94f, 1f);
    }

    private static void CreateRunePaths(Transform parent, Material rune, Material metal)
    {
        GameObject paths = new GameObject("Three Discipline Rune Paths");
        paths.transform.SetParent(parent, false);
        CreatePath(paths.transform, new Vector3(-3.6f, -0.04f, -2.45f), new Vector3(0.45f, 0.04f, 7.8f), 55f, rune, metal);
        CreatePath(paths.transform, new Vector3(3.6f, -0.04f, -2.45f), new Vector3(0.45f, 0.04f, 7.8f), -55f, rune, metal);
        CreatePath(paths.transform, new Vector3(0f, -0.04f, 3.25f), new Vector3(0.48f, 0.04f, 5.4f), 0f, rune, metal);
    }

    private static void CreatePath(Transform parent, Vector3 position, Vector3 scale, float yaw, Material rune, Material metal)
    {
        GameObject basePath = CreatePrimitive("Discipline Path", PrimitiveType.Cube, parent, position, scale, metal);
        basePath.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        GameObject glow = CreatePrimitive("Flowing Rune Inlay", PrimitiveType.Cube, basePath.transform,
            new Vector3(0f, 0.56f, 0f), new Vector3(0.24f, 0.08f, 0.96f), rune);
        glow.transform.localRotation = Quaternion.identity;
    }

    private static void CreateConservatory(
        Transform parent,
        ArcaneGameManager manager,
        Material herb,
        Material mana,
        Material metal,
        Material floor)
    {
        GameObject zone = new GameObject("Justin — Arcane Conservatory District");
        zone.transform.SetParent(parent, false);
        zone.transform.localPosition = new Vector3(-7.15f, 0f, -4.65f);

        CreatePrimitive("Conservatory Platform", PrimitiveType.Cylinder, zone.transform, new Vector3(0f, -0.18f, 0f),
            new Vector3(4.2f, 0.18f, 4.2f), metal);
        CreatePrimitive("Living Soil", PrimitiveType.Cylinder, zone.transform, new Vector3(0f, 0.01f, 0f),
            new Vector3(3.55f, 0.05f, 3.55f), floor);

        for (int i = -1; i <= 1; i++)
        {
            CreatePrimitive("Greenhouse Front Post " + i, PrimitiveType.Cube, zone.transform,
                new Vector3(i * 1.35f, 1.15f, -1.55f), new Vector3(0.1f, 2.3f, 0.1f), metal);
            CreatePrimitive("Greenhouse Back Post " + i, PrimitiveType.Cube, zone.transform,
                new Vector3(i * 1.35f, 1.15f, 1.55f), new Vector3(0.1f, 2.3f, 0.1f), metal);
            CreatePrimitive("Greenhouse Roof Rib " + i, PrimitiveType.Cube, zone.transform,
                new Vector3(i * 1.35f, 2.3f, 0f), new Vector3(0.1f, 0.1f, 3.2f), mana);
        }
        CreatePrimitive("Greenhouse Crown Front", PrimitiveType.Cube, zone.transform, new Vector3(0f, 2.3f, -1.55f),
            new Vector3(2.8f, 0.1f, 0.1f), mana);
        CreatePrimitive("Greenhouse Crown Back", PrimitiveType.Cube, zone.transform, new Vector3(0f, 2.3f, 1.55f),
            new Vector3(2.8f, 0.1f, 0.1f), mana);

        Vector3[] plantPositions =
        {
            new Vector3(-1.05f, 0.08f, -0.85f), new Vector3(0f, 0.08f, -1.05f),
            new Vector3(1.05f, 0.08f, -0.85f), new Vector3(-0.75f, 0.08f, 0.65f),
            new Vector3(0.75f, 0.08f, 0.65f)
        };
        for (int i = 0; i < plantPositions.Length; i++)
        {
            string asset = i % 2 == 0 ? "/plant_bush.fbx" : "/flower_yellowA.fbx";
            CreateImported("Conservatory Plant " + i, NatureFolder + asset, zone.transform, plantPositions[i],
                new Vector3(0f, i * 41f, 0f), Vector3.one * (0.9f + i * 0.06f));
        }

        GameObject ecologyCore = CreatePrimitive("Bloom Ecology Core", PrimitiveType.Sphere, zone.transform,
            new Vector3(0f, 1.05f, 0f), Vector3.one * 0.52f, herb);
        GameObject familiarOrbit = new GameObject("Pollinating Familiar Orbit");
        familiarOrbit.transform.SetParent(zone.transform, false);
        familiarOrbit.transform.localPosition = new Vector3(0f, 1.45f, 0f);
        CreatePrimitive("Familiar Body", PrimitiveType.Sphere, familiarOrbit.transform, new Vector3(1.15f, 0f, 0f),
            Vector3.one * 0.24f, mana);
        CreatePrimitive("Familiar Wing Left", PrimitiveType.Cube, familiarOrbit.transform, new Vector3(1.15f, 0.08f, -0.22f),
            new Vector3(0.18f, 0.04f, 0.34f), herb);
        CreatePrimitive("Familiar Wing Right", PrimitiveType.Cube, familiarOrbit.transform, new Vector3(1.15f, 0.08f, 0.22f),
            new Vector3(0.18f, 0.04f, 0.34f), herb);

        Light light = CreateLight("Conservatory Life Light", zone.transform, new Vector3(0f, 2.1f, 0f),
            new Color(0.25f, 1f, 0.42f), 2.1f, 6f);
        TextMesh label = CreateLabel(zone.transform, "JUSTIN — ARCANE CONSERVATORY", new Vector3(0f, 3.25f, 0f),
            0.032f, 46, TextAnchor.MiddleCenter);
        BoxCollider collider = zone.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 1.2f, 0f);
        collider.size = new Vector3(4.2f, 2.8f, 4.2f);

        TeamExpansionStation station = zone.AddComponent<TeamExpansionStation>();
        station.Configure(TeamStationKind.ArcaneConservatory, manager, label, familiarOrbit.transform,
            ecologyCore.GetComponent<Renderer>(), light);
    }

    private static void CreateFoundry(
        Transform parent,
        ArcaneGameManager manager,
        Material crystal,
        Material rune,
        Material metal,
        Material flow)
    {
        GameObject zone = new GameObject("Lucas — Astral Foundry District");
        zone.transform.SetParent(parent, false);
        zone.transform.localPosition = new Vector3(7.15f, 0f, -4.65f);

        CreatePrimitive("Foundry Platform", PrimitiveType.Cylinder, zone.transform, new Vector3(0f, -0.18f, 0f),
            new Vector3(4.2f, 0.18f, 4.2f), metal);
        CreatePrimitive("Forge Basin", PrimitiveType.Cylinder, zone.transform, new Vector3(0f, 0.38f, 0f),
            new Vector3(1.55f, 0.42f, 1.55f), metal);
        GameObject astralCore = CreatePrimitive("Astral Forge Core", PrimitiveType.Sphere, zone.transform,
            new Vector3(0f, 1.12f, 0f), Vector3.one * 0.58f, crystal);

        GameObject runeWheel = new GameObject("Rune Logistics Wheel");
        runeWheel.transform.SetParent(zone.transform, false);
        runeWheel.transform.localPosition = new Vector3(0f, 1.08f, 0f);
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI * 2f / 8f;
            GameObject glyph = CreatePrimitive("Rune Node " + i, PrimitiveType.Cube, runeWheel.transform,
                new Vector3(Mathf.Cos(angle) * 1.35f, 0f, Mathf.Sin(angle) * 1.35f),
                new Vector3(0.22f, 0.18f, 0.42f), rune);
            glyph.transform.localRotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 18f);
        }

        for (int i = 0; i < 3; i++)
        {
            GameObject shard = CreatePrimitive("Captured Star Fragment " + i, PrimitiveType.Cube, zone.transform,
                new Vector3(-1.15f + i * 1.15f, 0.72f, -1.35f), new Vector3(0.28f, 0.72f, 0.28f), crystal);
            shard.transform.localRotation = Quaternion.Euler(18f + i * 7f, 45f + i * 22f, 12f);
        }

        CreatePrimitive("Shield Pylon Left", PrimitiveType.Cylinder, zone.transform, new Vector3(-1.65f, 0.75f, 0.6f),
            new Vector3(0.22f, 0.75f, 0.22f), rune);
        CreatePrimitive("Shield Pylon Right", PrimitiveType.Cylinder, zone.transform, new Vector3(1.65f, 0.75f, 0.6f),
            new Vector3(0.22f, 0.75f, 0.22f), rune);
        CreateImported("Foundry Supply Crates", KayKitFolder + "/crates_stacked.fbx", zone.transform,
            new Vector3(1.6f, 0f, 1.4f), new Vector3(0f, -25f, 0f), Vector3.one * 0.78f);

        Light light = CreateLight("Astral Forge Light", zone.transform, new Vector3(0f, 2.15f, 0f),
            new Color(0.86f, 0.3f, 1f), 2.3f, 6f);
        TextMesh label = CreateLabel(zone.transform, "LUCAS — ASTRAL FOUNDRY", new Vector3(0f, 3.25f, 0f),
            0.032f, 46, TextAnchor.MiddleCenter);
        BoxCollider collider = zone.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 1.2f, 0f);
        collider.size = new Vector3(4.2f, 2.8f, 4.2f);

        TeamExpansionStation station = zone.AddComponent<TeamExpansionStation>();
        station.Configure(TeamStationKind.AstralFoundry, manager, label, runeWheel.transform,
            astralCore.GetComponent<Renderer>(), light);
    }

    private static void CreateNexus(
        Transform parent,
        ArcaneGameManager manager,
        Material rune,
        Material crystal,
        Material mana,
        Material metal,
        Material flow)
    {
        GameObject zone = new GameObject("Michael — Convergence Nexus District");
        zone.transform.SetParent(parent, false);
        zone.transform.localPosition = new Vector3(0f, 0f, 6.15f);

        CreatePrimitive("Convergence Dais", PrimitiveType.Cylinder, zone.transform, new Vector3(0f, -0.12f, 0f),
            new Vector3(3.8f, 0.22f, 3.8f), metal);
        CreatePrimitive("Convergence Rune Disc", PrimitiveType.Cylinder, zone.transform, new Vector3(0f, 0.12f, 0f),
            new Vector3(3.1f, 0.06f, 3.1f), rune);

        GameObject triad = new GameObject("Three Discipline Convergence Orbit");
        triad.transform.SetParent(zone.transform, false);
        triad.transform.localPosition = new Vector3(0f, 1.35f, 0f);
        GameObject core = CreatePrimitive("Convergence Core", PrimitiveType.Sphere, triad.transform, Vector3.zero,
            Vector3.one * 0.72f, flow != null ? flow : mana);
        Material[] triadMaterials = { mana, crystal, rune };
        for (int i = 0; i < 3; i++)
        {
            float angle = i * Mathf.PI * 2f / 3f;
            CreatePrimitive("Discipline Orb " + i, PrimitiveType.Sphere, triad.transform,
                new Vector3(Mathf.Cos(angle) * 1.25f, 0f, Mathf.Sin(angle) * 1.25f),
                Vector3.one * 0.32f, triadMaterials[i]);
        }

        for (int i = 0; i < 3; i++)
        {
            float angle = i * Mathf.PI * 2f / 3f;
            CreatePrimitive("Ritual Pillar " + i, PrimitiveType.Cylinder, zone.transform,
                new Vector3(Mathf.Cos(angle) * 1.75f, 0.72f, Mathf.Sin(angle) * 1.75f),
                new Vector3(0.24f, 0.72f, 0.24f), triadMaterials[i]);
        }

        Light light = CreateLight("Convergence Light", zone.transform, new Vector3(0f, 2.25f, 0f),
            new Color(0.3f, 0.82f, 1f), 2.5f, 7f);
        TextMesh label = CreateLabel(zone.transform, "MICHAEL — CONVERGENCE NEXUS", new Vector3(0f, 3.25f, 0f),
            0.032f, 46, TextAnchor.MiddleCenter);
        BoxCollider collider = zone.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 1.35f, 0f);
        collider.size = new Vector3(3.8f, 3f, 3.8f);

        TeamExpansionStation station = zone.AddComponent<TeamExpansionStation>();
        station.Configure(TeamStationKind.ConvergenceNexus, manager, label, triad.transform,
            core.GetComponent<Renderer>(), light);
    }

    private static Material LoadMaterial(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/" + name + ".mat");
    }

    private static GameObject CreatePrimitive(
        string name,
        PrimitiveType type,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Material material)
    {
        GameObject primitive = GameObject.CreatePrimitive(type);
        primitive.name = name;
        primitive.transform.SetParent(parent, false);
        primitive.transform.localPosition = localPosition;
        primitive.transform.localScale = localScale;
        Renderer renderer = primitive.GetComponent<Renderer>();
        if (renderer != null && material != null)
        {
            renderer.sharedMaterial = material;
        }
        return primitive;
    }

    private static GameObject CreateImported(
        string name,
        string path,
        Transform parent,
        Vector3 localPosition,
        Vector3 euler,
        Vector3 scale)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null)
        {
            return null;
        }
        GameObject instance = PrefabUtility.InstantiatePrefab(asset) as GameObject;
        if (instance == null)
        {
            return null;
        }
        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = Quaternion.Euler(euler);
        instance.transform.localScale = scale;
        return instance;
    }

    private static TextMesh CreateLabel(
        Transform parent,
        string text,
        Vector3 localPosition,
        float characterSize,
        int fontSize,
        TextAnchor anchor)
    {
        GameObject labelObject = new GameObject("Status Label");
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.localPosition = localPosition;
        TextMesh label = labelObject.AddComponent<TextMesh>();
        label.text = text;
        label.anchor = anchor;
        label.alignment = TextAlignment.Center;
        label.fontSize = fontSize;
        label.characterSize = characterSize;
        label.color = new Color(0.86f, 0.96f, 1f);
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
        {
            label.font = font;
            // Dynamic built-in font atlases are not reliably serialized into a separate material.
            // Use the font's own material for authored district signs so cloned projects remain readable.
            labelObject.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }
        labelObject.AddComponent<WorldLabelBillboard>();
        return label;
    }

    private static Light CreateLight(
        string name,
        Transform parent,
        Vector3 localPosition,
        Color color,
        float intensity,
        float range)
    {
        GameObject lightObject = new GameObject(name);
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = localPosition;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.Soft;
        return light;
    }
}
