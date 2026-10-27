using System.IO;
using ArcaneWorkshop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class MichaelIslandSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/ArcaneWorkshop.unity";
    private const string RootName = "Michael Island";
    private const string MaterialFolder = "Assets/Materials";
    private const string FantasyTownFolder = "Assets/ThirdParty/KenneyFantasyTown/Models";
    private const string NatureFolder = "Assets/ThirdParty/KenneyNature/Models";
    private const string DungeonFolder = "Assets/ThirdParty/KayKitDungeon/Models";

    private static readonly Vector2[] WalkablePolygon =
    {
        new Vector2(-10.6f, -7.1f),
        new Vector2(10.6f, -7.1f),
        new Vector2(0f, 10.3f)
    };

    [MenuItem("MP2B/Rebuild Michael Island")]
    public static void RebuildMichaelIsland()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            Debug.LogWarning("Exit Play Mode and wait for compilation before rebuilding Michael Island.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (scene.isDirty)
            {
                Debug.LogError("Save or discard the active scene changes before opening ArcaneWorkshop.");
                return;
            }
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        RemoveLegacyWorld();
        BuildIsland();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Selection.activeObject = GameObject.Find(RootName);
        Debug.Log("Michael Island rebuilt: triangular atelier shell, imported skyline, anchors, and walkable contract are authored in the main scene.");
    }

    private static void RemoveLegacyWorld()
    {
        string[] legacyRoots =
        {
            RootName,
            "Floating Workshop Island",
            "Arcane Workshop Environment",
            "MP2B Team Expansion"
        };
        for (int i = 0; i < legacyRoots.Length; i++)
        {
            GameObject existing = GameObject.Find(legacyRoots[i]);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
        }
    }

    private static void BuildIsland()
    {
        Material stone = CreateMaterial("AtelierStone", new Color(0.56f, 0.63f, 0.76f), 0.08f, 0.48f);
        Material stoneDark = CreateMaterial("AtelierVoidRock", new Color(0.055f, 0.075f, 0.14f), 0.02f, 0.72f);
        Material gold = CreateMaterial("AtelierGold", new Color(0.86f, 0.56f, 0.12f), 0.34f, 0.42f);
        Material blueGlass = CreateMaterial("AtelierBlueGlass", new Color(0.08f, 0.48f, 0.95f), 0.58f, 0.25f);
        Material lucasGreen = CreateMaterial("AtelierLucasGate", new Color(0.18f, 0.92f, 0.58f), 0.72f, 0.5f);
        Material justinMagenta = CreateMaterial("AtelierJustinGate", new Color(0.92f, 0.18f, 0.86f), 0.72f, 0.5f);
        Material floor = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Floor.mat");
        Material rune = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Rune.mat");

        GameObject root = new GameObject(RootName);
        MichaelIslandLayout layout = root.AddComponent<MichaelIslandLayout>();

        CreateTriangleSurface(root.transform, floor != null ? floor : stone);
        CreateFloatingUnderside(root.transform, stoneDark);
        CreateEdgeRim(root.transform, stone, gold);
        CreateConstructionCourt(root.transform, stone, rune, gold, blueGlass);
        CreateRearAtelier(root.transform, stone, gold, blueGlass);
        CreateWorkshopDressing(root.transform, stone, gold);
        CreateImportedDressing(root.transform, stone, gold);

        Transform grid = CreateAnchor(root.transform, "Build Grid Anchor", new Vector3(0f, 0f, 0.7f), Quaternion.identity);
        Transform spawn = CreateAnchor(root.transform, "Player Spawn Anchor", new Vector3(0f, 0f, -4.8f), Quaternion.LookRotation(Vector3.forward));
        Transform lucas = CreateAnchor(root.transform, "Lucas Gate Anchor", new Vector3(-7.9f, 0f, -5.65f), Quaternion.Euler(0f, -34f, 0f));
        Transform justin = CreateAnchor(root.transform, "Justin Gate Anchor", new Vector3(7.9f, 0f, -5.65f), Quaternion.Euler(0f, 34f, 0f));
        Transform nexus = CreateAnchor(root.transform, "Nexus Indicator Anchor", new Vector3(0f, 0f, -11.6f), Quaternion.identity);
        layout.Configure(grid, spawn, lucas, justin, nexus, WalkablePolygon);
        CreateFutureGate(lucas, "Lucas Island", "Lucas", lucasGreen, stone, gold);
        CreateFutureGate(justin, "Justin Island", "Justin", justinMagenta, stone, gold);
        CreateDeliveryReceiver(root.transform, "Justin", WorkshopResourceKind.ConservatoryBloom,
            new Vector3(-6.6f, 0f, 2.6f), lucasGreen, stone, gold);
        CreateDeliveryReceiver(root.transform, "Lucas", WorkshopResourceKind.AstralCore,
            new Vector3(6.35f, 0f, 0.85f), justinMagenta, stone, gold);

        RepositionExistingSceneObjects();
    }

    private static void CreateDeliveryReceiver(
        Transform parent,
        string source,
        WorkshopResourceKind resource,
        Vector3 position,
        Material accent,
        Material stone,
        Material gold)
    {
        GameObject receiverObject = new GameObject(source + " " + resource + " Delivery Receiver");
        receiverObject.transform.SetParent(parent, false);
        receiverObject.transform.localPosition = position;
        receiverObject.transform.localRotation = Quaternion.LookRotation(-position.normalized, Vector3.up);
        TeammateDeliveryReceiver receiver = receiverObject.AddComponent<TeammateDeliveryReceiver>();

        BoxCollider interactionCollider = receiverObject.AddComponent<BoxCollider>();
        interactionCollider.center = new Vector3(0f, 0.85f, 0f);
        interactionCollider.size = new Vector3(2.35f, 1.7f, 1.8f);

        CreateImported(source + " Receiver Plinth", FantasyTownFolder + "/fountain-curved.fbx", receiverObject.transform,
            Vector3.zero, Vector3.zero, Vector3.one * 0.82f, stone);
        CreatePrimitive(source + " Receiver Accent", PrimitiveType.Cylinder, receiverObject.transform,
            new Vector3(0f, 0.28f, 0f), new Vector3(1.3f, 0.07f, 1.3f), accent).GetComponent<Collider>().enabled = false;

        GameObject payloadAnchorObject = new GameObject("Delivered Model Anchor");
        payloadAnchorObject.transform.SetParent(receiverObject.transform, false);
        payloadAnchorObject.transform.localPosition = new Vector3(0f, 0.82f, 0f);
        Transform payloadAnchor = payloadAnchorObject.transform;

        Mesh ringMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes/RuneRing.asset");
        if (ringMesh != null)
        {
            CreateMeshObject(source + " Receiver Rune", ringMesh, payloadAnchor, Vector3.zero,
                new Vector3(90f, 0f, 0f), Vector3.one * 0.72f, gold);
        }
        CreatePrimitive(source + " Receiver Beacon", PrimitiveType.Sphere, payloadAnchor,
            new Vector3(0f, 0.42f, 0f), Vector3.one * 0.27f, accent).GetComponent<Collider>().enabled = false;

        TextMesh label = CreateLabel(receiverObject.transform, string.Empty,
            new Vector3(0f, 2.15f, 0f), 0.028f, 52, TextAnchor.MiddleCenter);
        label.color = accent.color;
        receiver.Configure(resource, source, payloadAnchor, label);
        CreatePointLight(source + " Receiver Light", receiverObject.transform,
            new Vector3(0f, 1.15f, -0.25f), accent.color, 0.9f, 3.5f);
    }

    private static void CreateFutureGate(Transform anchor, string destination, string owner, Material accent, Material stone, Material gold)
    {
        GameObject gate = new GameObject(destination + " Reserved Link Gate");
        gate.transform.SetParent(anchor, false);
        FutureIslandGate futureGate = gate.AddComponent<FutureIslandGate>();
        futureGate.Configure(destination, owner, false);

        BoxCollider safetySeal = gate.AddComponent<BoxCollider>();
        safetySeal.center = new Vector3(0f, 1.35f, 0.16f);
        safetySeal.size = new Vector3(3.45f, 2.7f, 0.58f);

        CreateImported(destination + " Gate Arch", FantasyTownFolder + "/wall-doorway-round.fbx", gate.transform,
            new Vector3(0f, 0f, 0.15f), Vector3.zero, Vector3.one * 2.75f, stone);
        CreateImported(destination + " Gate Crown", FantasyTownFolder + "/wall-arch-top-detail.fbx", gate.transform,
            new Vector3(0f, 3.72f, 0.15f), Vector3.zero, Vector3.one * 2.75f, gold);
        CreateImported(destination + " Gate Left Pillar", FantasyTownFolder + "/pillar-stone.fbx", gate.transform,
            new Vector3(-1.78f, 0f, 0.15f), Vector3.zero, Vector3.one * 2.65f, stone);
        CreateImported(destination + " Gate Right Pillar", FantasyTownFolder + "/pillar-stone.fbx", gate.transform,
            new Vector3(1.78f, 0f, 0.15f), Vector3.zero, Vector3.one * 2.65f, stone);

        CreateBeam(destination + " Safety Rail", gate.transform, new Vector3(-1.45f, 0.72f, -0.08f),
            new Vector3(1.45f, 0.72f, -0.08f), 0.13f, 0.13f, accent);
        CreatePrimitive(destination + " Link Seal", PrimitiveType.Sphere, gate.transform,
            new Vector3(0f, 1.72f, 0.08f), Vector3.one * 0.46f, accent).GetComponent<Collider>().enabled = false;

        Mesh ringMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes/RuneRing.asset");
        if (ringMesh != null)
        {
            CreateMeshObject(destination + " Link Ring", ringMesh, gate.transform, new Vector3(0f, 1.72f, 0.08f),
                new Vector3(90f, 0f, 0f), Vector3.one * 1.18f, accent);
        }

        TextMesh title = CreateLabel(gate.transform,
            destination.ToUpperInvariant() + " LINK\nRESERVED  •  SAFE",
            new Vector3(0f, 4.45f, 0f), 0.043f, 58, TextAnchor.MiddleCenter);
        title.color = accent.color;
        CreatePointLight(destination + " Gate Light", gate.transform,
            new Vector3(0f, 2f, -0.4f), accent.color, 1.3f, 5.5f);
    }

    private static void CreateTriangleSurface(Transform parent, Material material)
    {
        GameObject surface = new GameObject("Michael Island Walkable Surface");
        surface.transform.SetParent(parent, false);
        Mesh mesh = new Mesh { name = "Michael Island Triangle Surface" };
        mesh.vertices = new[]
        {
            new Vector3(WalkablePolygon[0].x, -0.08f, WalkablePolygon[0].y),
            new Vector3(WalkablePolygon[1].x, -0.08f, WalkablePolygon[1].y),
            new Vector3(WalkablePolygon[2].x, -0.08f, WalkablePolygon[2].y)
        };
        mesh.triangles = new[] { 0, 2, 1 };
        mesh.uv = new[] { Vector2.zero, Vector2.right, new Vector2(0.5f, 1f) };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        MeshFilter filter = surface.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = surface.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.On;
        MeshCollider collider = surface.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;
    }

    private static void CreateFloatingUnderside(Transform parent, Material material)
    {
        Vector3 a = new Vector3(WalkablePolygon[0].x, -0.12f, WalkablePolygon[0].y);
        Vector3 b = new Vector3(WalkablePolygon[1].x, -0.12f, WalkablePolygon[1].y);
        Vector3 c = new Vector3(WalkablePolygon[2].x, -0.12f, WalkablePolygon[2].y);
        Vector3 a2 = Vector3.Lerp(a, Vector3.zero, 0.16f) + Vector3.down * 1.45f;
        Vector3 b2 = Vector3.Lerp(b, Vector3.zero, 0.16f) + Vector3.down * 1.45f;
        Vector3 c2 = Vector3.Lerp(c, Vector3.zero, 0.16f) + Vector3.down * 1.45f;
        Vector3 tip = new Vector3(0f, -5.2f, 0.3f);

        Mesh mesh = new Mesh { name = "Michael Island Floating Underside" };
        mesh.vertices = new[] { a, b, c, a2, b2, c2, tip };
        mesh.triangles = new[]
        {
            0, 1, 4, 0, 4, 3,
            1, 2, 5, 1, 5, 4,
            2, 0, 3, 2, 3, 5,
            3, 4, 6, 4, 5, 6, 5, 3, 6
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject underside = new GameObject("Layered Floating Rock Underside");
        underside.transform.SetParent(parent, false);
        MeshFilter filter = underside.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = underside.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;

        for (int i = 0; i < 18; i++)
        {
            float t = (i + 0.5f) / 18f;
            Vector3 edge;
            float yaw;
            if (t < 1f / 3f)
            {
                edge = Vector3.Lerp(a, b, t * 3f);
                yaw = 0f;
            }
            else if (t < 2f / 3f)
            {
                edge = Vector3.Lerp(b, c, (t - 1f / 3f) * 3f);
                yaw = -58f;
            }
            else
            {
                edge = Vector3.Lerp(c, a, (t - 2f / 3f) * 3f);
                yaw = 58f;
            }
            string asset = i % 3 == 0 ? "rock-large.fbx" : "rock-wide.fbx";
            CreateImported("Island Edge Rock " + i, FantasyTownFolder + "/" + asset, parent,
                edge + new Vector3(0f, -0.55f - (i % 2) * 0.22f, 0f),
                new Vector3(8f + i * 3f, yaw, (i % 3 - 1) * 7f),
                Vector3.one * (1.2f + (i % 4) * 0.16f), material);
        }
    }

    private static void CreateEdgeRim(Transform parent, Material stone, Material gold)
    {
        Vector3 a = new Vector3(WalkablePolygon[0].x, 0.04f, WalkablePolygon[0].y);
        Vector3 b = new Vector3(WalkablePolygon[1].x, 0.04f, WalkablePolygon[1].y);
        Vector3 c = new Vector3(WalkablePolygon[2].x, 0.04f, WalkablePolygon[2].y);
        CreateBeam("Front Stone Rim", parent, a, b, 0.34f, 0.24f, stone);
        CreateBeam("Right Stone Rim", parent, b, c, 0.34f, 0.24f, stone);
        CreateBeam("Left Stone Rim", parent, c, a, 0.34f, 0.24f, stone);
        CreateBeam("Front Gold Inlay", parent, a + Vector3.up * 0.14f, b + Vector3.up * 0.14f, 0.06f, 0.04f, gold);
        CreateBeam("Right Gold Inlay", parent, b + Vector3.up * 0.14f, c + Vector3.up * 0.14f, 0.06f, 0.04f, gold);
        CreateBeam("Left Gold Inlay", parent, c + Vector3.up * 0.14f, a + Vector3.up * 0.14f, 0.06f, 0.04f, gold);
    }

    private static void CreateConstructionCourt(Transform parent, Material stone, Material rune, Material gold, Material blueGlass)
    {
        GameObject court = CreatePrimitive("Convergence Construction Court", PrimitiveType.Cylinder, parent,
            new Vector3(0f, 0.02f, 0.8f), new Vector3(5.15f, 0.09f, 5.15f), stone);
        court.GetComponent<Collider>().enabled = false;
        CreatePrimitive("Court Rune Disc", PrimitiveType.Cylinder, parent, new Vector3(0f, 0.115f, 0.8f),
            new Vector3(4.62f, 0.02f, 4.62f), rune != null ? rune : blueGlass).GetComponent<Collider>().enabled = false;
        CreatePrimitive("Court Inner Stone", PrimitiveType.Cylinder, parent, new Vector3(0f, 0.14f, 0.8f),
            new Vector3(4.15f, 0.025f, 4.15f), stone).GetComponent<Collider>().enabled = false;

        for (int i = 0; i < 12; i++)
        {
            float angle = i * Mathf.PI * 2f / 12f;
            Vector3 position = new Vector3(Mathf.Cos(angle) * 4.45f, 0.2f, 0.8f + Mathf.Sin(angle) * 4.45f);
            GameObject marker = CreatePrimitive("Court Gold Rune " + i, PrimitiveType.Cube, parent, position,
                new Vector3(0.08f, 0.025f, 0.55f), gold);
            marker.transform.rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg + 90f, 0f);
            marker.GetComponent<Collider>().enabled = false;
        }
    }

    private static void CreateRearAtelier(Transform parent, Material stone, Material gold, Material blueGlass)
    {
        GameObject skyline = new GameObject("Convergence Atelier Skyline");
        skyline.transform.SetParent(parent, false);

        CreatePrimitive("Atelier Raised Terrace", PrimitiveType.Cylinder, skyline.transform,
            new Vector3(0f, 0.18f, 7.05f), new Vector3(8.4f, 0.42f, 4.9f), stone)
            .GetComponent<Collider>().enabled = false;
        CreatePrimitive("Atelier Terrace Rune", PrimitiveType.Cylinder, skyline.transform,
            new Vector3(0f, 0.42f, 7.05f), new Vector3(7.65f, 0.035f, 4.15f), blueGlass)
            .GetComponent<Collider>().enabled = false;
        CreatePrimitive("Atelier Terrace Floor", PrimitiveType.Cylinder, skyline.transform,
            new Vector3(0f, 0.47f, 7.05f), new Vector3(7.18f, 0.035f, 3.72f), stone)
            .GetComponent<Collider>().enabled = false;

        CreateImported("Grand Atelier Arch", FantasyTownFolder + "/wall-doorway-round.fbx", skyline.transform,
            new Vector3(0f, 0.48f, 7.55f), Vector3.zero, Vector3.one * 3.25f, stone);
        CreateImported("Grand Arch Crown", FantasyTownFolder + "/wall-arch-top-detail.fbx", skyline.transform,
            new Vector3(0f, 4.92f, 7.55f), Vector3.zero, Vector3.one * 3.25f, gold);

        for (int wing = -2; wing <= 2; wing++)
        {
            if (wing == 0)
            {
                continue;
            }
            CreateImported("Atelier Wing Wall " + wing, FantasyTownFolder + "/wall-arch.fbx", skyline.transform,
                new Vector3(wing * 1.85f, 0.48f, 7.72f), Vector3.zero, Vector3.one * 2.45f, stone);
            CreateImported("Atelier Wing Crown " + wing, FantasyTownFolder + "/wall-arch-top-detail.fbx", skyline.transform,
                new Vector3(wing * 1.85f, 3.82f, 7.72f), Vector3.zero, Vector3.one * 2.45f, gold);
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * 5.55f;
            CreateImported("Atelier Tower Window " + side, FantasyTownFolder + "/wall-window-glass.fbx", skyline.transform,
                new Vector3(x, 0.48f, 7.2f), Vector3.zero, Vector3.one * 3.15f, stone);
            CreateImported("Atelier Tower Pillar " + side, FantasyTownFolder + "/pillar-stone.fbx", skyline.transform,
                new Vector3(x + side * 1.62f, 0.48f, 7.2f), Vector3.zero, Vector3.one * 3.35f, stone);
            CreateImported("Atelier Tower Inner Pillar " + side, FantasyTownFolder + "/pillar-stone.fbx", skyline.transform,
                new Vector3(x - side * 1.62f, 0.48f, 7.2f), Vector3.zero, Vector3.one * 3.35f, stone);
            CreateImported("Atelier Tower Roof " + side, FantasyTownFolder + "/roof-high-point.fbx", skyline.transform,
                new Vector3(x, 5.45f, 7.2f), Vector3.zero, Vector3.one * 3.18f, blueGlass);
            CreateImported("Atelier Balcony " + side, FantasyTownFolder + "/balcony-wall-fence.fbx", skyline.transform,
                new Vector3(x, 2.05f, 5.5f), new Vector3(0f, 180f, 0f), Vector3.one * 2.5f, gold);
            CreateImported("Atelier Lantern " + side, FantasyTownFolder + "/lantern.fbx", skyline.transform,
                new Vector3(x * 0.55f, 2.25f, 5.95f), Vector3.zero, Vector3.one * 2.1f, gold);
            CreatePointLight("Tower Mana Light " + side, skyline.transform,
                new Vector3(x, 3.25f, 5.9f), new Color(0.2f, 0.65f, 1f), 1.45f, 7f);
        }

        GameObject observatory = new GameObject("Arcane Observatory Crown");
        observatory.transform.SetParent(skyline.transform, false);
        observatory.transform.localPosition = new Vector3(0f, 6.55f, 7.15f);
        Mesh ringMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes/RuneRing.asset");
        if (ringMesh != null)
        {
            CreateMeshObject("Observatory Ring A", ringMesh, observatory.transform, Vector3.zero,
                new Vector3(72f, 0f, 0f), Vector3.one * 2.15f, gold);
            CreateMeshObject("Observatory Ring B", ringMesh, observatory.transform, Vector3.zero,
                new Vector3(0f, 55f, 35f), Vector3.one * 1.72f, blueGlass);
        }
        CreatePrimitive("Observatory Mana Core", PrimitiveType.Sphere, observatory.transform, Vector3.zero,
            Vector3.one * 0.78f, blueGlass).GetComponent<Collider>().enabled = false;

        TextMesh title = CreateLabel(skyline.transform, "MICHAEL  •  CONVERGENCE ATELIER\nALCHEMY  •  RESEARCH  •  SYNTHESIS",
            new Vector3(0f, 8.25f, 6.6f), 0.058f, 64, TextAnchor.MiddleCenter);
        title.color = new Color(0.78f, 0.93f, 1f);
    }

    private static void CreateImportedDressing(Transform parent, Material stone, Material gold)
    {
        Vector3[] treePositions =
        {
            new Vector3(-7.7f, 0f, 1.8f), new Vector3(7.7f, 0f, 1.8f),
            new Vector3(-5.9f, 0f, 5.4f), new Vector3(5.9f, 0f, 5.4f)
        };
        for (int i = 0; i < treePositions.Length; i++)
        {
            string tree = i % 2 == 0 ? "tree_oak.fbx" : "tree_default.fbx";
            CreateImported("Atelier Tree " + i, NatureFolder + "/" + tree, parent, treePositions[i],
                new Vector3(0f, i * 73f, 0f), Vector3.one * (1.15f + i * 0.08f), null);
        }

        CreateImported("Left Court Stairs", FantasyTownFolder + "/stairs-wide-stone-handrail.fbx", parent,
            new Vector3(-7.55f, 0f, -4.95f), new Vector3(0f, -34f, 0f), Vector3.one * 1.25f, stone);
        CreateImported("Right Court Stairs", FantasyTownFolder + "/stairs-wide-stone-handrail.fbx", parent,
            new Vector3(7.55f, 0f, -4.95f), new Vector3(0f, 34f, 0f), Vector3.one * 1.25f, stone);
        CreateImported("Atelier Fountain", FantasyTownFolder + "/fountain-round-detail.fbx", parent,
            new Vector3(0f, 0.15f, 5.15f), Vector3.zero, Vector3.one * 1.25f, stone);
        CreateImported("Fountain Arcane Focus", FantasyTownFolder + "/fountain-center.fbx", parent,
            new Vector3(0f, 0.15f, 5.15f), Vector3.zero, Vector3.one * 1.25f, gold);
    }

    private static void CreateWorkshopDressing(Transform parent, Material stone, Material gold)
    {
        GameObject workshop = new GameObject("Michael Alchemy Workshop Dressing");
        workshop.transform.SetParent(parent, false);

        string[] backWalls = { "wall_window_open.fbx", "wall_arched.fbx", "wall_window_open.fbx" };
        for (int i = 0; i < backWalls.Length; i++)
        {
            CreateImported("Atelier Interior Wall " + i, DungeonFolder + "/" + backWalls[i], workshop.transform,
                new Vector3((i - 1) * 3.2f, 0.46f, 8.35f), Vector3.zero, Vector3.one * 1.42f, stone);
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * 4.75f;
            CreateImported("Carved Research Pillar " + side, DungeonFolder + "/pillar_decorated.fbx", workshop.transform,
                new Vector3(x, 0.45f, 7.85f), Vector3.zero, Vector3.one * 1.35f, stone);
            CreateImported("Blue Atelier Banner " + side, DungeonFolder + "/banner_patternA_blue.fbx", workshop.transform,
                new Vector3(side * 2.35f, 2.75f, 7.72f), new Vector3(0f, 180f, 0f), Vector3.one * 1.18f, null);
            CreateImported("Mounted Mana Torch " + side, DungeonFolder + "/torch_mounted.fbx", workshop.transform,
                new Vector3(side * 1.15f, 2.15f, 7.42f), new Vector3(0f, 180f, 0f), Vector3.one, null);
            CreatePointLight("Warm Work Light " + side, workshop.transform,
                new Vector3(side * 1.15f, 2.45f, 6.85f), new Color(1f, 0.55f, 0.2f), 1.05f, 5f);
        }

        CreateImported("Alchemist Workbench", DungeonFolder + "/table_long_decorated_A.fbx", workshop.transform,
            new Vector3(-5.45f, 0.42f, 4.55f), new Vector3(0f, 92f, 0f), Vector3.one * 1.08f, null);
        CreateImported("Potion Reference Shelves", DungeonFolder + "/shelves.fbx", workshop.transform,
            new Vector3(5.6f, 0.42f, 4.65f), new Vector3(0f, -92f, 0f), Vector3.one * 1.12f, null);
        CreateImported("Candle Reagent Shelf", DungeonFolder + "/shelf_small_candles.fbx", workshop.transform,
            new Vector3(3.95f, 0.52f, 7.1f), new Vector3(0f, 180f, 0f), Vector3.one, null);
        CreateImported("Secured Reagent Chest", DungeonFolder + "/chest_gold.fbx", workshop.transform,
            new Vector3(-5.05f, 0.42f, 6.55f), new Vector3(0f, 25f, 0f), Vector3.one, null);
        CreateImported("Essence Storage Barrel", DungeonFolder + "/barrel_large_decorated.fbx", workshop.transform,
            new Vector3(5.15f, 0.42f, 6.45f), new Vector3(0f, -22f, 0f), Vector3.one * 0.92f, null);
        CreateImported("Workshop Supply Crates", DungeonFolder + "/crates_stacked.fbx", workshop.transform,
            new Vector3(-8.1f, 0f, -0.8f), new Vector3(0f, 28f, 0f), Vector3.one * 0.9f, null);

        CreateImported("Green Reagent Display", DungeonFolder + "/bottle_A_green.fbx", workshop.transform,
            new Vector3(-5.15f, 1.4f, 4.25f), new Vector3(0f, 18f, 0f), Vector3.one * 0.62f, null);
        CreateImported("Brown Reagent Display", DungeonFolder + "/bottle_B_brown.fbx", workshop.transform,
            new Vector3(-5.15f, 1.4f, 4.75f), new Vector3(0f, -20f, 0f), Vector3.one * 0.58f, null);
        CreateImported("Atelier Coin Reserve", DungeonFolder + "/coin_stack_large.fbx", workshop.transform,
            new Vector3(-4.6f, 0.52f, 6.28f), Vector3.zero, Vector3.one * 0.8f, gold);
    }

    private static void RepositionExistingSceneObjects()
    {
        MerchantPortal portal = Object.FindAnyObjectByType<MerchantPortal>();
        if (portal != null)
        {
            portal.transform.SetPositionAndRotation(new Vector3(7.35f, 0f, 3.75f), Quaternion.Euler(0f, -122f, 0f));
        }

        Camera camera = Camera.main;
        if (camera != null)
        {
            OrbitCameraController orbit = camera.GetComponent<OrbitCameraController>();
            if (orbit != null)
            {
                orbit.ConfigureFrame(new Vector3(0f, 0f, 1.4f), 24f, 0f, 38f);
            }
            else
            {
                camera.transform.SetPositionAndRotation(new Vector3(0f, 14.5f, -19.5f), Quaternion.identity);
                camera.transform.LookAt(new Vector3(0f, 1.1f, 1.4f));
            }
        }

        GameObject fill = GameObject.Find("Arcane Fill Light");
        if (fill != null)
        {
            fill.transform.position = new Vector3(0f, 6f, 2f);
        }
    }

    private static Material CreateMaterial(string name, Color color, float emission, float smoothness)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Standard")) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        material.SetFloat("_Metallic", name.Contains("Gold") ? 0.68f : 0.12f);
        material.SetFloat("_Glossiness", smoothness);
        if (emission > 0f)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Transform CreateAnchor(Transform parent, string name, Vector3 position, Quaternion rotation)
    {
        GameObject anchor = new GameObject(name);
        anchor.transform.SetParent(parent, false);
        anchor.transform.localPosition = position;
        anchor.transform.localRotation = rotation;
        return anchor.transform;
    }

    private static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject gameObject = GameObject.CreatePrimitive(type);
        gameObject.name = name;
        gameObject.transform.SetParent(parent, false);
        gameObject.transform.localPosition = position;
        gameObject.transform.localScale = scale;
        Renderer renderer = gameObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }
        return gameObject;
    }

    private static GameObject CreateImported(string name, string path, Transform parent, Vector3 position, Vector3 euler, Vector3 scale, Material overrideMaterial)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null)
        {
            Debug.LogWarning("Michael Island asset missing: " + path);
            return null;
        }
        GameObject instance = PrefabUtility.InstantiatePrefab(asset) as GameObject;
        if (instance == null)
        {
            return null;
        }
        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = position;
        instance.transform.localRotation = Quaternion.Euler(euler);
        instance.transform.localScale = scale;
        if (overrideMaterial != null)
        {
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = overrideMaterial;
                }
                renderer.sharedMaterials = materials;
            }
        }
        return instance;
    }

    private static GameObject CreateMeshObject(string name, Mesh mesh, Transform parent, Vector3 position, Vector3 euler, Vector3 scale, Material material)
    {
        GameObject gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        gameObject.transform.localPosition = position;
        gameObject.transform.localRotation = Quaternion.Euler(euler);
        gameObject.transform.localScale = scale;
        gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
        return gameObject;
    }

    private static void CreateBeam(string name, Transform parent, Vector3 start, Vector3 end, float width, float height, Material material)
    {
        Vector3 delta = end - start;
        GameObject beam = CreatePrimitive(name, PrimitiveType.Cube, parent, (start + end) * 0.5f,
            new Vector3(width, height, delta.magnitude), material);
        beam.transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
        beam.GetComponent<Collider>().enabled = false;
    }

    private static TextMesh CreateLabel(Transform parent, string text, Vector3 position, float characterSize, int fontSize, TextAnchor anchor)
    {
        GameObject labelObject = new GameObject("Michael Island Label");
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.localPosition = position;
        labelObject.transform.localScale = Vector3.one;
        TextMesh label = labelObject.AddComponent<TextMesh>();
        label.text = text;
        label.anchor = anchor;
        label.alignment = TextAlignment.Center;
        label.fontSize = fontSize;
        label.characterSize = characterSize;
        WorldText.Style(label);
        labelObject.AddComponent<WorldLabelBillboard>().ScaleWithDistance = false;
        return label;
    }

    private static Light CreatePointLight(string name, Transform parent, Vector3 position, Color color, float intensity, float range)
    {
        GameObject lightObject = new GameObject(name);
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = position;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
        return light;
    }
}
