using System.Collections.Generic;
using ArcaneWorkshop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// MP2B pass over the MP2A Arcane Workshop island. Keeps the MP2A layout and gameplay, and authors:
/// two ring-portal bridge gates (Lucas west, Justin east) with supply receivers, the Convergence Altar,
/// the floating Nexus Core between the islands, a rim balustrade, a rock underside, a night sky,
/// lighting, ambient motes, and walk-blocking colliders on every solid prop. Safe to re-run.
/// </summary>
public static class MichaelIslandBuilder
{
    private const string ScenePath = "Assets/Scenes/ArcaneWorkshop.unity";
    private const string RootName = "Michael Island Links";
    private const string MaterialFolder = "Assets/Materials";
    private const string Town = "Assets/ThirdParty/KenneyFantasyTown/Models/";
    private const string Castle = "Assets/ThirdParty/KenneyCastle/Models/";
    private const string Dungeon = "Assets/ThirdParty/KayKitDungeon/Models/";
    private const string AmbientCG = "Assets/ThirdParty/AmbientCG/";

    private const float FloorTop = -0.16f;
    private const float IslandRadius = 11f;
    private const float RimRadius = 10.75f;

    // Gate layout (yaw = facing out of the island). Keliang aligned Lucas's gate with `mainToFarmBridge` on the
    // south-west diagonal; Justin's gate mirrors it on the south-east diagonal and the Merchant Gate takes the east slot.
    private const float GateRadius = 9f;
    private const float LucasYaw = 218f;
    private const float JustinYaw = 142f;
    private const float MerchantYaw = 90f;
    private const float GateGapDegrees = 15f;

    // Ritual islet: the centre of the three-island triangle (Michael 0,0 · Lucas -28,-36 · Justin mirrored 28,-36).
    private static readonly Vector3 RitualIsletCenter = new Vector3(0f, 3.2f, -24f);

    public static readonly Color LucasColor = new Color(0.32f, 0.95f, 0.45f);
    public static readonly Color JustinColor = new Color(0.78f, 0.42f, 1f);
    private static readonly Vector3 MoonDirection = new Vector3(-0.35f, 0.55f, 0.75f);

    [MenuItem("MP2B/Build Michael Island")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            Debug.LogWarning("Exit Play Mode and wait for compilation before building Michael Island.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (scene.isDirty)
            {
                Debug.LogError("Save or discard the current scene before building Michael Island.");
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

        DestroyRoot("MP2B Team Expansion");
        DestroyRoot(RootName);
        GameObject root = new GameObject(RootName);
        root.AddComponent<AmbientSoundscape>();

        Materials m = LoadMaterials();
        ArrangeMp2aProps();
        RetextureIsland(m);
        CreateUnderside(root.transform, m);
        CreateRimBalustrade(root.transform, m);

        // Lucas's real bridge (mainToFarmBridge) docks at his gate, so only Justin's gate gets a placeholder pier.
        IslandLinkGate lucasGate = CreateLinkGate(root.transform, manager, m, "Lucas", "Verdant Conservatory",
            LucasYaw, false, LucasColor, m.lucasGlow, m.lucasVeil, 6, false, WorkshopResourceKind.Gold);
        IslandLinkGate justinGate = CreateLinkGate(root.transform, manager, m, "Justin", "Astral Foundry",
            JustinYaw, true, JustinColor, m.justinGlow, m.justinVeil, 0, true, WorkshopResourceKind.ConservatoryBloom);

        // Each receiver stands just inside its gate, on the outer flank (away from the south view to the ritual islet).
        CreateReceiver(root.transform, manager, m, lucasGate, "Lucas", WorkshopResourceKind.ConservatoryBloom,
            BesideGate(LucasYaw, 1f), LucasColor, m.lucasGlow,
            new[]
            {
                new WorkshopResourceAmount(WorkshopResourceKind.Water, 2),
                new WorkshopResourceAmount(WorkshopResourceKind.MoonHerb, 1),
                new WorkshopResourceAmount(WorkshopResourceKind.Gold, 6)
            });
        CreateReceiver(root.transform, manager, m, justinGate, "Justin", WorkshopResourceKind.AstralCore,
            BesideGate(JustinYaw, -1f), JustinColor, m.justinGlow,
            new[]
            {
                new WorkshopResourceAmount(WorkshopResourceKind.Crystal, 2),
                new WorkshopResourceAmount(WorkshopResourceKind.Essence, 1),
                new WorkshopResourceAmount(WorkshopResourceKind.Gold, 6)
            });

        CreateConvergenceAltar(root.transform, manager, m);
        CreateRitualIslet(root.transform, m);
        CreateNexusCore(root.transform, manager, m);
        CreateTitle(root.transform, m);
        CreateAmbientMotes(root.transform, m);
        AddPropColliders();
        GroundMp2aProps();
        FixWallOverlap();
        ConfigureSkyAndLighting(m);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Michael Island built: MP2A workshop + bridge gates, receivers, altar, Nexus Core, sky and lighting.");
    }

    // ------------------------------------------------------------------ materials

    private sealed class Materials
    {
        public Material paving, rock, rimStone, gold, metal, crystal, rune, flow;
        public Material lucasGlow, justinGlow, lucasVeil, justinVeil, nexusGlow, mote, sky;
    }

    private static Materials LoadMaterials()
    {
        Materials m = new Materials();
        m.paving = TexturedMaterial("IslandPaving", "PavingStones070", new Color(0.62f, 0.66f, 0.86f), new Vector2(7f, 7f), 0.18f);
        m.rock = TexturedMaterial("IslandUndersideRock", "Rock030", new Color(0.42f, 0.4f, 0.58f), new Vector2(4f, 2f), 0.05f);
        m.rimStone = ColorMaterial("IslandRimStone", new Color(0.6f, 0.64f, 0.78f), 0f, 0.25f, 0.1f);
        m.gold = ColorMaterial("IslandGoldTrim", new Color(0.92f, 0.68f, 0.25f), 0.25f, 0.55f, 0.75f);
        m.lucasGlow = ColorMaterial("LinkGlowLucas", LucasColor, 1.4f, 0.6f, 0f);
        m.justinGlow = ColorMaterial("LinkGlowJustin", JustinColor, 1.4f, 0.6f, 0f);
        m.nexusGlow = ColorMaterial("NexusCoreGlow", new Color(0.35f, 0.5f, 0.95f), 1.2f, 0.7f, 0f);
        m.metal = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/DarkMetal.mat");
        m.crystal = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Crystal.mat");
        m.rune = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Rune.mat");
        m.flow = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/AnimatedArcaneFlow.mat");
        m.lucasVeil = VeilMaterial("LinkVeilLucas", LucasColor, m.flow);
        m.justinVeil = VeilMaterial("LinkVeilJustin", JustinColor, m.flow);
        m.mote = ParticleMaterial();
        m.sky = SkyMaterial();
        return m;
    }

    private static Material TexturedMaterial(string name, string texture, Color tint, Vector2 tiling, float smoothness)
    {
        Material material = GetOrCreate(name, Shader.Find("Standard"));
        Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(AmbientCG + texture + "_Color.jpg");
        string normalPath = AmbientCG + texture + "_Normal.jpg";
        TextureImporter importer = AssetImporter.GetAtPath(normalPath) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.NormalMap)
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.SaveAndReimport();
        }
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        material.color = tint;
        material.mainTexture = albedo;
        material.mainTextureScale = tiling;
        if (normal != null)
        {
            material.SetTexture("_BumpMap", normal);
            material.SetFloat("_BumpScale", 0.8f);
            material.EnableKeyword("_NORMALMAP");
        }
        material.SetFloat("_Glossiness", smoothness);
        material.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material ColorMaterial(string name, Color color, float emission, float smoothness, float metallic)
    {
        Material material = GetOrCreate(name, Shader.Find("Standard"));
        material.color = color;
        material.SetFloat("_Glossiness", smoothness);
        material.SetFloat("_Metallic", metallic);
        if (emission > 0f)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else
        {
            material.DisableKeyword("_EMISSION");
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material VeilMaterial(string name, Color color, Material template)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = template != null ? new Material(template) : new Material(Shader.Find("Standard"));
            material.name = name;
            AssetDatabase.CreateAsset(material, path);
        }
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", new Color(color.r, color.g, color.b, 0.85f));
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material ParticleMaterial()
    {
        string path = MaterialFolder + "/IslandMote.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Legacy Shaders/Particles/Additive");
            material = new Material(shader) { name = "IslandMote" };
            Material builtin = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
            if (builtin != null)
            {
                material.mainTexture = builtin.mainTexture;
            }
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_TintColor", new Color(0.55f, 0.75f, 1f, 0.6f));
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material SkyMaterial()
    {
        Material material = GetOrCreate("ArcaneNightSky", Shader.Find("ArcaneWorkshop/Night Sky"));
        material.SetVector("_MoonDirection", MoonDirection);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material GetOrCreate(string name, Shader shader)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        else if (material.shader != shader && shader != null)
        {
            material.shader = shader;
        }
        return material;
    }

    // ------------------------------------------------------------------ MP2A island pass

    private static void ArrangeMp2aProps()
    {
        // Keep the two diagonal bridge gates, their receivers and the east Merchant Gate clear.
        MoveChild("Arcane Workshop Environment", "Supply Crates", new Vector3(-4.35f, 0f, 6.75f), 20f);
        MoveChild("Arcane Workshop Environment", "Moonlit Tree 0", new Vector3(-9.35f, 0f, 2.3f), 0f);
        MoveChild("Arcane Workshop Environment", "Moonlit Tree 2", new Vector3(-9.55f, 0f, -1.6f), 0f);
        MoveChild("Arcane Workshop Environment", "Moonlit Tree 1", new Vector3(8.95f, 0f, -2.7f), 0f);
        MoveChild("Arcane Workshop Environment", "Moonlit Tree 3", new Vector3(9.3f, 0f, 3.5f), 0f);
        MoveMerchantGate();
    }

    private static Vector3 YawDirection(float yaw)
    {
        return Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
    }

    // A receiver spot just inside a gate: side +1 = the gate's local right, -1 = its left.
    private static Vector3 BesideGate(float yaw, float side)
    {
        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
        return rotation * new Vector3(side * 3.25f, 0f, GateRadius - 0.9f);
    }

    // The Merchant Gate is an MP2A prefab instance with a world-space delivery path; swing it around the island
    // centre so its distance from the centre stays the same.
    private static void MoveMerchantGate()
    {
        GameObject portalObject = GameObject.Find("MerchantPortal");
        MerchantPortal portal = portalObject != null ? portalObject.GetComponent<MerchantPortal>() : null;
        if (portal == null)
        {
            return;
        }
        Vector3 flat = new Vector3(portalObject.transform.position.x, 0f, portalObject.transform.position.z);
        float currentYaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
        float delta = Mathf.DeltaAngle(currentYaw, MerchantYaw);
        if (Mathf.Abs(delta) < 0.01f)
        {
            return;
        }
        Quaternion swing = Quaternion.Euler(0f, delta, 0f);
        portalObject.transform.SetPositionAndRotation(swing * portalObject.transform.position,
            swing * portalObject.transform.rotation);
        SerializedObject serialized = new SerializedObject(portal);
        // The route starts at the potion shelf and ends at the gate: keep the start, swing later points progressively.
        SerializedProperty path = serialized.FindProperty("deliveryPath");
        for (int i = 1; i < path.arraySize; i++)
        {
            SerializedProperty point = path.GetArrayElementAtIndex(i);
            float share = i / (float)(path.arraySize - 1);
            point.vector3Value = Quaternion.Euler(0f, delta * share, 0f) * point.vector3Value;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // MP2A placed its props at y = 0, but the paved floor surface is at FloorTop, so they hovered 16 cm.
    // Drop every floor-standing prop (bottom within 30 cm above the floor) onto the paving. Idempotent.
    private static void GroundMp2aProps()
    {
        List<Transform> props = new List<Transform>();
        GameObject environment = GameObject.Find("Arcane Workshop Environment");
        if (environment != null)
        {
            foreach (Transform child in environment.transform)
            {
                props.Add(child);
            }
        }
        GameObject portal = GameObject.Find("MerchantPortal");
        if (portal != null)
        {
            props.Add(portal.transform);
        }
        foreach (Transform prop in props)
        {
            Renderer[] renderers = prop.GetComponentsInChildren<Renderer>();
            float bottom = float.MaxValue;
            foreach (Renderer renderer in renderers)
            {
                if (renderer is MeshRenderer && renderer.enabled)
                {
                    bottom = Mathf.Min(bottom, renderer.bounds.min.y);
                }
            }
            Vector3 flat = new Vector3(prop.position.x, 0f, prop.position.z);
            if (bottom == float.MaxValue || flat.magnitude > IslandRadius || bottom <= FloorTop + 0.005f || bottom > FloorTop + 0.3f)
            {
                continue;
            }
            prop.position += Vector3.down * (bottom - FloorTop);
        }
    }

    // The five back-wall segments are 6.6 m wide but spaced 3.35 m apart, so neighbours share the same front and
    // back planes and z-fight (the shimmering castle texture in the headset). Push the two plain fill segments a
    // few centimetres behind the arched/window segments so every visible face has a single owner.
    private static void FixWallOverlap()
    {
        GameObject environment = GameObject.Find("Arcane Workshop Environment");
        if (environment == null)
        {
            return;
        }
        foreach (Transform child in environment.transform)
        {
            if (child.name == "Workshop Wall 1" || child.name == "Workshop Wall 3")
            {
                child.position = new Vector3(child.position.x, -0.17f, 9.23f);
            }
        }
    }

    private static void MoveChild(string rootName, string childName, Vector3 position, float yaw)
    {
        GameObject rootObject = GameObject.Find(rootName);
        Transform child = rootObject != null ? rootObject.transform.Find(childName) : null;
        if (child == null)
        {
            Debug.LogWarning("Michael Island: could not find " + rootName + "/" + childName);
            return;
        }
        child.position = position;
        if (yaw != 0f)
        {
            child.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }

    private static void RetextureIsland(Materials m)
    {
        GameObject island = GameObject.Find("Floating Workshop Island");
        if (island == null)
        {
            return;
        }
        island.GetComponent<Renderer>().sharedMaterial = m.paving;
        // A primitive cylinder ships with a CapsuleCollider, which at this scale is an 11 m sphere
        // swallowing pointer rays. Use the real mesh instead.
        CapsuleCollider capsule = island.GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            Object.DestroyImmediate(capsule);
        }
        MeshCollider meshCollider = island.GetComponent<MeshCollider>();
        if (meshCollider == null)
        {
            meshCollider = island.AddComponent<MeshCollider>();
        }
        meshCollider.sharedMesh = island.GetComponent<MeshFilter>().sharedMesh;
    }

    private static void CreateUnderside(Transform parent, Materials m)
    {
        float[] heights = { FloorTop - 0.3f, -1.6f, -3.1f, -4.9f, -6.8f, -8.6f };
        float[] radii = { IslandRadius, 10.2f, 8.6f, 6.4f, 3.8f, 1.4f };
        const int segments = 40;
        System.Random random = new System.Random(417);
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();
        for (int ring = 0; ring < heights.Length; ring++)
        {
            for (int s = 0; s <= segments; s++)
            {
                float angle = (s % segments) * Mathf.PI * 2f / segments;
                float jitter = ring == 0 ? 0f : (float)(random.NextDouble() - 0.5) * radii[ring] * 0.22f;
                float r = radii[ring] + jitter;
                float y = heights[ring] + (ring == 0 ? 0f : (float)(random.NextDouble() - 0.5) * 0.5f);
                vertices.Add(new Vector3(Mathf.Cos(angle) * r, y, Mathf.Sin(angle) * r));
                uvs.Add(new Vector2(s / (float)segments * 6f, y * 0.35f));
            }
        }
        int tip = vertices.Count;
        vertices.Add(new Vector3(0.4f, -10.4f, -0.3f));
        uvs.Add(new Vector2(0.5f, -3.6f));
        int stride = segments + 1;
        for (int ring = 0; ring < heights.Length - 1; ring++)
        {
            for (int s = 0; s < segments; s++)
            {
                int a = ring * stride + s;
                int b = a + 1;
                int c = a + stride;
                int d = c + 1;
                triangles.AddRange(new[] { a, c, b, b, c, d });
            }
        }
        int last = (heights.Length - 1) * stride;
        for (int s = 0; s < segments; s++)
        {
            triangles.AddRange(new[] { last + s, tip, last + s + 1 });
        }

        Mesh mesh = new Mesh { name = "Michael Island Rock Underside" };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        SaveMesh(mesh, "Assets/Meshes/MichaelIslandUnderside.asset");

        GameObject underside = new GameObject("Floating Rock Underside");
        underside.transform.SetParent(parent, false);
        underside.AddComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes/MichaelIslandUnderside.asset");
        MeshRenderer renderer = underside.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = m.rock;
        renderer.shadowCastingMode = ShadowCastingMode.Off;

        // Boulder clusters hugging the underside sell the floating silhouette.
        for (int i = 0; i < 12; i++)
        {
            float angle = (i * 30f + 9f) * Mathf.Deg2Rad;
            float r = 9.3f - (i % 3) * 1.1f;
            Vector3 position = new Vector3(Mathf.Cos(angle) * r, -1.9f - (i % 3) * 1.1f, Mathf.Sin(angle) * r);
            CreateImported("Underside Boulder " + i, Castle + "rocks-large.fbx", underside.transform, position,
                new Vector3(180f + (i % 2) * 15f, i * 47f, (i % 3 - 1) * 12f), Vector3.one * (2.2f + (i % 4) * 0.45f), m.rock);
        }
    }

    private static void CreateRimBalustrade(Transform parent, Materials m)
    {
        GameObject rim = new GameObject("Rim Balustrade");
        rim.transform.SetParent(parent, false);
        const float panelScale = 1.25f;
        float step = panelScale / RimRadius;
        int count = Mathf.FloorToInt(Mathf.PI * 2f / step);
        step = Mathf.PI * 2f / count;
        for (int i = 0; i < count; i++)
        {
            float angle = i * step;
            Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 position = radial * RimRadius;
            float yaw = Mathf.Atan2(radial.x, radial.z) * Mathf.Rad2Deg;
            if (position.z > 6.6f || NearYaw(yaw, LucasYaw, GateGapDegrees) || NearYaw(yaw, JustinYaw, GateGapDegrees) ||
                NearYaw(yaw, MerchantYaw, 11f))
            {
                continue; // back workshop wall, both bridge gates, merchant gate
            }
            Vector3 tangent = new Vector3(-radial.z, 0f, radial.x);
            Quaternion rotation = Quaternion.LookRotation(tangent, Vector3.up);
            GameObject panel = CreateImported("Rim Panel " + i, Town + "balcony-wall-fence.fbx", rim.transform,
                position + rotation * Vector3.right * (0.45f * panelScale) + Vector3.up * FloorTop, Vector3.zero,
                new Vector3(panelScale, 0.95f, panelScale), m.rimStone);
            if (panel != null)
            {
                panel.transform.rotation = rotation;
            }
            Vector3 post = new Vector3(Mathf.Cos(angle - step * 0.5f), 0f, Mathf.Sin(angle - step * 0.5f)) * RimRadius;
            GameObject pillar = CreateImported("Rim Post " + i, Town + "pillar-stone.fbx", rim.transform,
                post + Vector3.up * FloorTop, Vector3.zero, new Vector3(1.6f, 1.05f, 1.6f), m.rimStone);
            if (i % 6 == 0 && pillar != null)
            {
                GameObject cap = CreatePrimitive("Rim Post Glow " + i, PrimitiveType.Sphere, rim.transform,
                    post + Vector3.up * (FloorTop + 1.12f), Vector3.one * 0.16f, m.rune);
                Object.DestroyImmediate(cap.GetComponent<Collider>());
            }
        }
    }

    private static bool NearYaw(float yaw, float target, float halfWidth)
    {
        return Mathf.Abs(Mathf.DeltaAngle(yaw, target)) < halfWidth;
    }

    // ------------------------------------------------------------------ bridge gates

    private static IslandLinkGate CreateLinkGate(Transform parent, ArcaneGameManager manager, Materials m,
        string owner, string title, float yaw, bool placeholderPier, Color accent, Material glow, Material veilMaterial,
        int goals, bool needsDelivery, WorkshopResourceKind deliveryKind)
    {
        Vector3 outward = YawDirection(yaw);
        GameObject gate = new GameObject(owner + " Bridge Gate");
        gate.transform.SetParent(parent, false);
        gate.transform.position = outward * GateRadius + Vector3.up * FloorTop;
        gate.transform.rotation = Quaternion.LookRotation(outward, Vector3.up);
        Transform t = gate.transform;

        Material cloth = ColorMaterial("LinkBanner" + owner, Color.Lerp(accent, new Color(0.1f, 0.08f, 0.2f), 0.45f), 0.15f, 0.1f, 0f);
        const float ringY = 2.15f;
        const float ringRadius = 1.75f;

        // Threshold and the stone pier that the teammate bridge docks onto.
        CreatePrimitive(owner + " Gate Threshold", PrimitiveType.Cube, t, new Vector3(0f, 0.05f, 0.35f),
            new Vector3(4.6f, 0.14f, 2.2f), m.rimStone).GetComponent<Collider>().enabled = false;
        CreatePrimitive(owner + " Threshold Inlay", PrimitiveType.Cube, t, new Vector3(0f, 0.125f, 0.35f),
            new Vector3(3.2f, 0.02f, 0.14f), glow).GetComponent<Collider>().enabled = false;
        if (placeholderPier)
        {
            CreateWoodPier(t, owner, m);
            TextMesh pierSign = CreateLabel(t, owner.ToUpperInvariant() + "'S BRIDGE DOCKS HERE", new Vector3(0f, 1.35f, 7.1f),
                0.028f, 48, true);
            pierSign.color = Color.Lerp(accent, Color.white, 0.4f);
        }

        // Twin hexagonal towers holding the ring.
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * 2.25f;
            CreateImported(owner + " Gate Tower " + side, Castle + "tower-hexagon-base.fbx", t,
                new Vector3(x, 0f, 0.3f), new Vector3(0f, 30f, 0f), new Vector3(1.25f, 2.1f, 1.25f), m.rimStone);
            CreateImported(owner + " Gate Tower Cap " + side, Castle + "tower-hexagon-top.fbx", t,
                new Vector3(x, 2.75f, 0.3f), new Vector3(0f, 30f, 0f), new Vector3(1.32f, 1.2f, 1.32f), m.gold);
            CreateImported(owner + " Gate Lantern " + side, Town + "lantern.fbx", t,
                new Vector3(x, 2.9f, 0.3f), Vector3.zero, Vector3.one * 0.85f, null);
            GameObject flag = CreateImported(owner + " Gate Banner " + side, Castle + "flag-banner-long.fbx", t,
                new Vector3(x, 0.75f, -0.36f), new Vector3(0f, 90f, 0f), new Vector3(0.9f, 0.62f, 0.9f), cloth);
            if (flag != null)
            {
                flag.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            }
        }

        // The ring: carved stone torus, gold rune band, a spinning glyph ring and the portal veil.
        Mesh stoneTorus = TorusMesh("LinkGateStoneRing", ringRadius, 0.24f, 40, 10);
        GameObject stoneRing = CreateMeshObject(owner + " Stone Ring", stoneTorus, t, new Vector3(0f, ringY, 0.3f),
            Vector3.zero, Vector3.one, m.rimStone);
        Mesh goldTorus = TorusMesh("LinkGateGoldBand", ringRadius - 0.24f, 0.06f, 40, 6);
        GameObject goldBand = CreateMeshObject(owner + " Rune Band", goldTorus, t, new Vector3(0f, ringY, 0.3f),
            Vector3.zero, Vector3.one, glow);
        CreatePrimitive(owner + " Ring Footing", PrimitiveType.Cube, t, new Vector3(0f, 0.28f, 0.3f),
            new Vector3(1.3f, 0.36f, 0.62f), m.rimStone).GetComponent<Collider>().enabled = false;
        CreatePrimitive(owner + " Keystone", PrimitiveType.Cube, t, new Vector3(0f, ringY + ringRadius + 0.05f, 0.3f),
            new Vector3(0.5f, 0.5f, 0.6f), m.gold).GetComponent<Collider>().enabled = false;

        GameObject spinRoot = new GameObject(owner + " Glyph Spin");
        spinRoot.transform.SetParent(t, false);
        spinRoot.transform.localPosition = new Vector3(0f, ringY, 0.3f);
        Mesh glyphRing = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes/RuneRing.asset");
        Renderer glyphRenderer = null;
        if (glyphRing != null)
        {
            glyphRenderer = CreateMeshObject(owner + " Glyph Ring", glyphRing, spinRoot.transform, Vector3.zero,
                new Vector3(90f, 0f, 0f), Vector3.one * 1.32f, glow).GetComponent<Renderer>();
        }
        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.PI / 3f;
            GameObject rune = CreatePrimitive(owner + " Ring Rune " + i, PrimitiveType.Cube, spinRoot.transform,
                new Vector3(Mathf.Cos(a) * 1.32f, Mathf.Sin(a) * 1.32f, 0f), new Vector3(0.14f, 0.14f, 0.05f), glow);
            rune.transform.localRotation = Quaternion.Euler(0f, 0f, i * 60f + 45f);
            Object.DestroyImmediate(rune.GetComponent<Collider>());
        }

        GameObject veil = CreatePrimitive(owner + " Portal Veil", PrimitiveType.Cylinder, t, new Vector3(0f, ringY, 0.3f),
            new Vector3(ringRadius * 2f - 0.3f, 0.01f, ringRadius * 2f - 0.3f), veilMaterial);
        veil.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Object.DestroyImmediate(veil.GetComponent<Collider>());
        veil.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

        ParticleSystem swirl = CreateSwirl(owner + " Portal Swirl", t, new Vector3(0f, ringY, 0.3f), ringRadius - 0.25f, accent, m.mote);

        Light light = CreatePointLight(owner + " Gate Light", t, new Vector3(0f, ringY, -0.9f), accent, 2.2f, 7f);

        TextMesh label = CreateLabel(t, string.Empty, new Vector3(0f, 4.55f, 0.3f), 0.034f, 56, true);

        AudioSource hum = gate.AddComponent<AudioSource>();
        hum.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ThirdParty/KenneyAudio/forceField_000.ogg");
        hum.loop = true;
        hum.playOnAwake = false;
        hum.spatialBlend = 1f;
        hum.volume = 0.28f;
        hum.minDistance = 1.5f;
        hum.maxDistance = 11f;
        hum.rolloffMode = AudioRolloffMode.Linear;

        // Solid barrier in the ring: clickable, blocks the pointer-walk and the VR body until linked.
        BoxCollider barrier = gate.AddComponent<BoxCollider>();
        barrier.center = new Vector3(0f, 1.9f, 0.3f);
        barrier.size = new Vector3(5.6f, 3.9f, 0.7f);
        gate.AddComponent<WalkBlocker>();

        gate.AddComponent<HoverHighlight>().Configure(label);
        IslandLinkGate link = gate.AddComponent<IslandLinkGate>();
        Renderer[] glows = glyphRenderer != null
            ? new[] { goldBand.GetComponent<Renderer>(), glyphRenderer }
            : new[] { goldBand.GetComponent<Renderer>() };
        link.Configure(manager, owner, title, accent, goals, needsDelivery, deliveryKind,
            glows, veil, spinRoot.transform, swirl, light, label, barrier, hum);
        stoneRing.name = owner + " Stone Ring";
        return link;
    }

    // Plank pier beyond the ring where the teammate's bridge docks (local +Z points into the void).
    private static void CreateWoodPier(Transform gate, string owner, Materials m)
    {
        Material wood = ColorMaterial("PierWood", new Color(0.4f, 0.27f, 0.18f), 0f, 0.15f, 0f);
        Material darkWood = ColorMaterial("PierWoodDark", new Color(0.22f, 0.15f, 0.11f), 0f, 0.1f, 0f);
        Material rope = ColorMaterial("PierRope", new Color(0.62f, 0.52f, 0.36f), 0f, 0.05f, 0f);
        const float start = 1.45f;
        const float length = 5.8f;
        const float width = 2.7f;
        GameObject pier = new GameObject(owner + " Bridge Pier");
        pier.transform.SetParent(gate, false);
        Transform p = pier.transform;
        for (int i = 0; i < 12; i++)
        {
            float z = start + 0.25f + i * (length / 12f);
            GameObject plank = CreatePrimitive("Plank " + i, PrimitiveType.Cube, p, new Vector3(((i % 3) - 1) * 0.03f, 0.04f, z),
                new Vector3(width, 0.08f, length / 12f - 0.05f), i % 4 == 1 ? darkWood : wood);
            Object.DestroyImmediate(plank.GetComponent<Collider>());
        }
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * (width * 0.5f - 0.12f);
            CreatePrimitive("Stringer " + side, PrimitiveType.Cube, p, new Vector3(x, -0.12f, start + length * 0.5f),
                new Vector3(0.2f, 0.24f, length + 0.2f), darkWood).GetComponent<Collider>().enabled = false;
            for (int k = 0; k < 4; k++)
            {
                float z = start + 0.2f + k * (length - 0.4f) / 3f;
                CreatePrimitive("Rail Post " + side + "-" + k, PrimitiveType.Cube, p, new Vector3(x, 0.55f, z),
                    new Vector3(0.14f, 1.05f, 0.14f), darkWood).GetComponent<Collider>().enabled = false;
                CreatePrimitive("Post Cap " + side + "-" + k, PrimitiveType.Cube, p, new Vector3(x, 1.1f, z),
                    new Vector3(0.2f, 0.06f, 0.2f), m.gold).GetComponent<Collider>().enabled = false;
            }
            CreatePrimitive("Rope Rail " + side, PrimitiveType.Cube, p, new Vector3(x, 0.92f, start + length * 0.5f),
                new Vector3(0.05f, 0.05f, length - 0.3f), rope).GetComponent<Collider>().enabled = false;
            CreatePrimitive("Low Rope " + side, PrimitiveType.Cube, p, new Vector3(x, 0.5f, start + length * 0.5f),
                new Vector3(0.04f, 0.04f, length - 0.3f), rope).GetComponent<Collider>().enabled = false;
            GameObject strut = CreatePrimitive("Support Strut " + side, PrimitiveType.Cube, p, new Vector3(x, -1.05f, start + 1.1f),
                new Vector3(0.18f, 0.18f, 2.9f), darkWood);
            strut.transform.localRotation = Quaternion.Euler(-40f, 0f, 0f);
            Object.DestroyImmediate(strut.GetComponent<Collider>());
            CreateImported("Pier Lantern " + side, Town + "lantern.fbx", p, new Vector3(x, 0.08f, start + length - 0.15f),
                Vector3.zero, Vector3.one * 0.8f, null);
        }
    }

    private static ParticleSystem CreateSwirl(string name, Transform parent, Vector3 position, float radius, Color color, Material material)
    {
        GameObject swirlObject = new GameObject(name);
        swirlObject.transform.SetParent(parent, false);
        swirlObject.transform.localPosition = position;
        ParticleSystem system = swirlObject.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = system.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
        main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.6f));
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 120;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = 36f;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.radiusThickness = 0.15f;
        shape.rotation = Vector3.zero;

        ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.orbitalZ = new ParticleSystem.MinMaxCurve(1.4f);
        velocity.radial = new ParticleSystem.MinMaxCurve(-0.35f);
        velocity.x = new ParticleSystem.MinMaxCurve(0f);
        velocity.y = new ParticleSystem.MinMaxCurve(0f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f);

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = system.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = fade;

        ParticleSystemRenderer renderer = swirlObject.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return system;
    }

    // ------------------------------------------------------------------ receivers, altar, nexus

    private static void CreateReceiver(Transform parent, ArcaneGameManager manager, Materials m, IslandLinkGate gate,
        string source, WorkshopResourceKind resource, Vector3 position, Color accent, Material glow, WorkshopResourceAmount[] cost)
    {
        GameObject receiverObject = new GameObject(source + " Supply Receiver");
        receiverObject.transform.SetParent(parent, false);
        receiverObject.transform.position = new Vector3(position.x, FloorTop, position.z);
        Vector3 toCenter = -new Vector3(position.x, 0f, position.z).normalized;
        receiverObject.transform.rotation = Quaternion.LookRotation(toCenter, Vector3.up);
        Transform t = receiverObject.transform;

        CreateImported(source + " Receiver Plinth", Castle + "tower-hexagon-base.fbx", t, Vector3.zero,
            Vector3.zero, new Vector3(1.15f, 0.62f, 1.15f), m.rimStone);
        CreateImported(source + " Receiver Rim", Castle + "tower-hexagon-top.fbx", t, new Vector3(0f, 0.81f, 0f),
            Vector3.zero, new Vector3(1.22f, 1f, 1.22f), m.gold);
        CreatePrimitive(source + " Receiver Pad", PrimitiveType.Cylinder, t, new Vector3(0f, 0.95f, 0f),
            new Vector3(0.78f, 0.012f, 0.78f), glow).GetComponent<Collider>().enabled = false;

        GameObject anchor = new GameObject("Delivered Model Anchor");
        anchor.transform.SetParent(t, false);
        anchor.transform.localPosition = new Vector3(0f, 0.98f, 0f);

        GameObject beacon = new GameObject(source + " Receiver Beacon");
        beacon.transform.SetParent(t, false);
        beacon.transform.localPosition = new Vector3(0f, 1.75f, 0f);
        CreateMeshObject(source + " Beacon Gem", CrystalMesh(), beacon.transform, Vector3.zero, Vector3.zero,
            new Vector3(0.32f, 0.28f, 0.32f), glow);
        Mesh ring = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes/RuneRing.asset");
        if (ring != null)
        {
            CreateMeshObject(source + " Beacon Halo", ring, beacon.transform, Vector3.zero, new Vector3(0f, 0f, 0f),
                Vector3.one * 0.34f, m.gold);
        }

        GameObject crate = CreateImported(source + " Shipment Crate", Dungeon + "crates_stacked.fbx", t,
            new Vector3(0f, 1.2f, 0f), Vector3.zero, Vector3.one * 0.22f, null);
        if (crate != null)
        {
            CreatePointLight("Crate Glow", crate.transform, new Vector3(0f, 1f, 0f), accent, 1.6f, 2.5f);
            crate.SetActive(false);
        }

        GameObject origin = new GameObject("Shipment Origin");
        origin.transform.SetParent(gate.transform, false);
        origin.transform.localPosition = new Vector3(0f, 2.3f, 6.2f);

        TextMesh label = CreateLabel(t, string.Empty, new Vector3(0f, 2.45f, 0f), 0.024f, 50, true);
        label.color = Color.Lerp(accent, Color.white, 0.45f);
        CreatePointLight(source + " Receiver Light", t, new Vector3(0f, 1.6f, 0.5f), accent, 1.1f, 3.5f);

        BoxCollider collider = receiverObject.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 1.05f, 0f);
        collider.size = new Vector3(1.35f, 2.1f, 1.35f);
        receiverObject.AddComponent<WalkBlocker>();

        receiverObject.AddComponent<HoverHighlight>().Configure(label);
        TeammateDeliveryReceiver receiver = receiverObject.AddComponent<TeammateDeliveryReceiver>();
        receiver.Configure(resource, source, anchor.transform, label, manager, gate, origin.transform,
            crate != null ? crate.transform : null, beacon.transform, cost);
    }

    private static void CreateConvergenceAltar(Transform parent, ArcaneGameManager manager, Materials m)
    {
        GameObject altar = new GameObject("Michael Convergence Altar");
        altar.transform.SetParent(parent, false);
        altar.transform.position = new Vector3(0f, FloorTop, 6.35f);
        Transform t = altar.transform;

        CreateImported("Altar Basin", Town + "fountain-round-detail.fbx", t, Vector3.zero, Vector3.zero,
            new Vector3(1.25f, 1.1f, 1.25f), m.rimStone);
        CreateImported("Altar Gold Focus", Town + "fountain-center.fbx", t, new Vector3(0f, 0.012f, 0f), Vector3.zero,
            new Vector3(1.25f, 1.1f, 1.25f), m.gold);

        GameObject triad = new GameObject("Altar Orbit");
        triad.transform.SetParent(t, false);
        triad.transform.localPosition = new Vector3(0f, 1.9f, 0f);
        GameObject core = CreateMeshObject("Convergence Crystal", CrystalMesh(), triad.transform, Vector3.zero, Vector3.zero,
            new Vector3(0.55f, 0.42f, 0.55f), m.crystal);
        Color[] colors = { LucasColor, JustinColor, new Color(0.35f, 0.75f, 1f) };
        Material[] orbMaterials = { m.lucasGlow, m.justinGlow, m.nexusGlow };
        for (int i = 0; i < 3; i++)
        {
            float a = i * Mathf.PI * 2f / 3f;
            GameObject orb = CreatePrimitive("Discipline Orb " + i, PrimitiveType.Sphere, triad.transform,
                new Vector3(Mathf.Cos(a) * 0.72f, 0f, Mathf.Sin(a) * 0.72f), Vector3.one * 0.16f, orbMaterials[i]);
            Object.DestroyImmediate(orb.GetComponent<Collider>());
        }
        Mesh ring = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes/RuneRing.asset");
        if (ring != null)
        {
            CreateMeshObject("Altar Orbit Ring", ring, triad.transform, Vector3.zero, Vector3.zero, Vector3.one * 0.72f, m.gold);
        }

        Light light = CreatePointLight("Convergence Light", t, new Vector3(0f, 1.9f, -0.6f), new Color(0.4f, 0.7f, 1f), 2f, 6f);
        TextMesh label = CreateLabel(t, string.Empty, new Vector3(0f, 3.05f, 0f), 0.024f, 50, true);

        BoxCollider collider = altar.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 1.2f, 0f);
        collider.size = new Vector3(2.6f, 2.4f, 2.6f);
        altar.AddComponent<WalkBlocker>();

        altar.AddComponent<HoverHighlight>().Configure(label);
        AudioSource altarHum = altar.AddComponent<AudioSource>();
        altarHum.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ThirdParty/KenneyAudio/forceField_001.ogg");
        altarHum.loop = true;
        altarHum.playOnAwake = true;
        altarHum.spatialBlend = 1f;
        altarHum.volume = 0.18f;
        altarHum.minDistance = 1.2f;
        altarHum.maxDistance = 8f;
        altarHum.rolloffMode = AudioRolloffMode.Linear;
        TeamExpansionStation station = altar.AddComponent<TeamExpansionStation>();
        station.Configure(TeamStationKind.ConvergenceNexus, manager, label, triad.transform, core.GetComponent<Renderer>(), light);
    }

    private static void CreateNexusCore(Transform parent, ArcaneGameManager manager, Materials m)
    {
        // Hovers above the ritual islet at the centre of the three islands.
        GameObject nexus = new GameObject("Floating Nexus Core");
        nexus.transform.SetParent(parent, false);
        nexus.transform.position = RitualIsletCenter + Vector3.up * 1.6f;
        FinalRitualIndicator indicator = nexus.AddComponent<FinalRitualIndicator>();

        BoxCollider collider = nexus.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 2.2f, 0f);
        collider.size = new Vector3(5f, 6f, 5f);

        GameObject crystalRoot = new GameObject("Nexus Crystal");
        crystalRoot.transform.SetParent(nexus.transform, false);
        crystalRoot.transform.localPosition = new Vector3(0f, 2.2f, 0f);
        GameObject core = CreateMeshObject("Nexus Crystal Core", CrystalMesh(), crystalRoot.transform, Vector3.zero,
            Vector3.zero, new Vector3(2.6f, 2.4f, 2.6f), m.nexusGlow);
        for (int i = 0; i < 4; i++)
        {
            float a = i * Mathf.PI / 2f + 0.4f;
            CreateMeshObject("Nexus Shard " + i, CrystalMesh(), crystalRoot.transform,
                new Vector3(Mathf.Cos(a) * 2.3f, (i % 2) * 0.9f - 0.45f, Mathf.Sin(a) * 2.3f), new Vector3(0f, i * 90f, 18f),
                new Vector3(0.55f, 0.7f, 0.55f), m.crystal);
        }
        Mesh ring = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes/RuneRing.asset");
        if (ring != null)
        {
            CreateMeshObject("Nexus Orbit A", ring, crystalRoot.transform, Vector3.zero, new Vector3(75f, 0f, 0f), Vector3.one * 2.7f, m.gold);
            CreateMeshObject("Nexus Orbit B", ring, crystalRoot.transform, Vector3.zero, new Vector3(0f, 40f, 65f), Vector3.one * 2.2f, m.nexusGlow);
        }
        Mesh orbit = TorusMesh("NexusOrbitTorus", 3.1f, 0.07f, 48, 6);
        CreateMeshObject("Nexus Gold Orbit", orbit, crystalRoot.transform, Vector3.zero, new Vector3(70f, 0f, 0f), Vector3.one, m.gold);
        CreateMeshObject("Nexus Blue Orbit", orbit, crystalRoot.transform, Vector3.zero, new Vector3(20f, 60f, 0f), Vector3.one * 0.82f, m.nexusGlow);
        TextMesh label = CreateLabel(nexus.transform, string.Empty, new Vector3(0f, 5.6f, 0f), 0.06f, 60, true);
        Light light = CreatePointLight("Nexus Light", nexus.transform, new Vector3(0f, 2.2f, 2f), new Color(0.32f, 0.42f, 0.72f), 0.65f, 14f);
        nexus.AddComponent<HoverHighlight>().Configure(label);
        indicator.Configure(manager, crystalRoot.transform, label, core.GetComponent<Renderer>(), light);
    }

    // Small floating islet under the Nexus Core: a paved ritual circle, standing stones and a rock root.
    // Deliberately unreachable: no collider on the walkable surface and no teleport area, so the player only sees it.
    private static void CreateRitualIslet(Transform parent, Materials m)
    {
        GameObject islet = new GameObject("Central Ritual Islet");
        islet.transform.SetParent(parent, false);
        islet.transform.position = RitualIsletCenter;
        Transform t = islet.transform;

        GameObject top = CreatePrimitive("Ritual Islet Paving", PrimitiveType.Cylinder, t, new Vector3(0f, -0.15f, 0f),
            new Vector3(9f, 0.15f, 9f), m.paving);
        Object.DestroyImmediate(top.GetComponent<Collider>());
        GameObject rim = CreatePrimitive("Ritual Islet Rim", PrimitiveType.Cylinder, t, new Vector3(0f, -0.2f, 0f),
            new Vector3(9.5f, 0.16f, 9.5f), m.rimStone);
        Object.DestroyImmediate(rim.GetComponent<Collider>());

        GameObject root = new GameObject("Ritual Islet Rock");
        root.transform.SetParent(t, false);
        root.transform.localPosition = new Vector3(0f, -0.35f, 0f);
        root.transform.localScale = new Vector3(0.43f, 0.5f, 0.43f);
        root.AddComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes/MichaelIslandUnderside.asset");
        MeshRenderer rock = root.AddComponent<MeshRenderer>();
        rock.sharedMaterial = m.rock;
        rock.shadowCastingMode = ShadowCastingMode.Off;
        for (int i = 0; i < 5; i++)
        {
            float a = (i * 72f + 20f) * Mathf.Deg2Rad;
            CreateImported("Ritual Islet Boulder " + i, Castle + "rocks-large.fbx", t,
                new Vector3(Mathf.Cos(a) * 3.4f, -1.3f - (i % 2) * 0.7f, Mathf.Sin(a) * 3.4f),
                new Vector3(180f, i * 61f, 0f), Vector3.one * (1.4f + (i % 3) * 0.3f), m.rock);
        }

        // Six standing stones, one pair per island colour, facing the centre.
        Material[] glows = { m.lucasGlow, m.justinGlow, m.nexusGlow };
        for (int i = 0; i < 6; i++)
        {
            float a = (i * 60f + 30f) * Mathf.Deg2Rad;
            Vector3 position = new Vector3(Mathf.Cos(a) * 3.55f, 0f, Mathf.Sin(a) * 3.55f);
            GameObject stone = CreateImported("Ritual Standing Stone " + i, Town + "pillar-stone.fbx", t, position,
                Vector3.zero, new Vector3(1.5f, 1.9f, 1.5f), m.rimStone);
            if (stone != null)
            {
                foreach (Collider collider in stone.GetComponentsInChildren<Collider>())
                {
                    Object.DestroyImmediate(collider);
                }
            }
            GameObject gem = CreateMeshObject("Ritual Stone Gem " + i, CrystalMesh(), t, position + Vector3.up * 2.25f,
                Vector3.zero, new Vector3(0.22f, 0.2f, 0.22f), glows[(i / 2) % 3]);
            gem.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }
        Mesh circle = TorusMesh("RitualIsletCircle", 2.6f, 0.05f, 56, 4);
        GameObject inlay = CreateMeshObject("Ritual Circle Inlay", circle, t, new Vector3(0f, 0.01f, 0f),
            new Vector3(90f, 0f, 0f), Vector3.one, m.rune);
        inlay.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        CreatePointLight("Ritual Islet Glow", t, new Vector3(0f, 1.2f, 0f), new Color(0.45f, 0.55f, 1f), 1.4f, 9f);
    }

    private static void CreateTitle(Transform parent, Materials m)
    {
        GameObject title = new GameObject("Workshop Title Plaque");
        title.transform.SetParent(parent, false);
        title.transform.position = new Vector3(0f, 6.05f, 8.05f);
        CreatePrimitive("Plaque Backing", PrimitiveType.Cube, title.transform, Vector3.zero, new Vector3(7.6f, 1.1f, 0.12f),
            m.metal).GetComponent<Collider>().enabled = false;
        CreatePrimitive("Plaque Trim Top", PrimitiveType.Cube, title.transform, new Vector3(0f, 0.58f, -0.04f),
            new Vector3(7.8f, 0.06f, 0.1f), m.gold).GetComponent<Collider>().enabled = false;
        CreatePrimitive("Plaque Trim Bottom", PrimitiveType.Cube, title.transform, new Vector3(0f, -0.58f, -0.04f),
            new Vector3(7.8f, 0.06f, 0.1f), m.gold).GetComponent<Collider>().enabled = false;
        TextMesh text = CreateLabel(title.transform, "ARCANE WORKSHOP: REAWAKENING\nMICHAEL  •  CONVERGENCE ATELIER",
            new Vector3(0f, 0.02f, -0.08f), 0.05f, 62, false);
        text.color = new Color(0.85f, 0.92f, 1f);
    }

    private static void CreateAmbientMotes(Transform parent, Materials m)
    {
        GameObject motes = new GameObject("Ambient Mana Motes");
        motes.transform.SetParent(parent, false);
        motes.transform.position = new Vector3(0f, 1.6f, 0f);
        ParticleSystem system = motes.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = system.main;
        main.loop = true;
        main.playOnAwake = true;
        main.prewarm = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.12f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.45f, 0.8f, 1f), new Color(0.8f, 0.55f, 1f));
        main.maxParticles = 160;
        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = 14f;
        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(19f, 2.6f, 19f);
        ParticleSystem.NoiseModule noise = system.noise;
        noise.enabled = true;
        noise.strength = 0.25f;
        noise.frequency = 0.3f;
        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = system.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = fade;
        ParticleSystemRenderer renderer = motes.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = m.mote;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    // ------------------------------------------------------------------ colliders, sky, lighting

    private static void AddPropColliders()
    {
        string[] solidPrefixes =
        {
            "Workshop Wall", "Left Corner Wall", "Right Corner Wall", "Carved Pillar", "Alchemist Workbench",
            "Potion Shelves", "Candle Shelf", "Arcane Chest", "Supply Barrels", "Supply Crates", "Coin Hoard", "Moonlit Tree"
        };
        GameObject environment = GameObject.Find("Arcane Workshop Environment");
        if (environment != null)
        {
            foreach (Transform child in environment.transform)
            {
                for (int i = 0; i < solidPrefixes.Length; i++)
                {
                    if (child.name.StartsWith(solidPrefixes[i]))
                    {
                        FitBoxCollider(child, child.name.StartsWith("Moonlit Tree") ? 0.3f : 1f);
                        break;
                    }
                }
            }
        }
        GameObject portal = GameObject.Find("MerchantPortal");
        if (portal != null)
        {
            Transform left = portal.transform.Find("Gate Pillar Left");
            Transform right = portal.transform.Find("Gate Pillar Right");
            if (left != null) FitBoxCollider(left, 0.7f);
            if (right != null) FitBoxCollider(right, 0.7f);
        }
    }

    private static void FitBoxCollider(Transform target, float horizontalScale)
    {
        bool hasBounds = false;
        Bounds local = new Bounds();
        foreach (MeshFilter filter in target.GetComponentsInChildren<MeshFilter>())
        {
            if (filter.sharedMesh == null)
            {
                continue;
            }
            Bounds mb = filter.sharedMesh.bounds;
            for (int c = 0; c < 8; c++)
            {
                Vector3 corner = mb.center + Vector3.Scale(mb.extents, new Vector3((c & 1) == 0 ? -1 : 1, (c & 2) == 0 ? -1 : 1, (c & 4) == 0 ? -1 : 1));
                Vector3 point = target.InverseTransformPoint(filter.transform.TransformPoint(corner));
                if (!hasBounds)
                {
                    local = new Bounds(point, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    local.Encapsulate(point);
                }
            }
        }
        if (!hasBounds)
        {
            return;
        }
        foreach (Collider existing in target.GetComponents<Collider>())
        {
            Object.DestroyImmediate(existing);
        }
        BoxCollider box = target.gameObject.AddComponent<BoxCollider>();
        box.center = local.center;
        box.size = new Vector3(local.size.x * horizontalScale, local.size.y, local.size.z * horizontalScale);
        if (target.GetComponent<WalkBlocker>() == null)
        {
            target.gameObject.AddComponent<WalkBlocker>();
        }
    }

    private static void ConfigureSkyAndLighting(Materials m)
    {
        RenderSettings.skybox = m.sky;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.26f, 0.28f, 0.5f);
        RenderSettings.ambientEquatorColor = new Color(0.2f, 0.16f, 0.32f);
        RenderSettings.ambientGroundColor = new Color(0.06f, 0.05f, 0.1f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.12f, 0.1f, 0.24f);
        RenderSettings.fogDensity = 0.011f;

        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.farClipPlane = Mathf.Max(camera.farClipPlane, 400f);
        }

        GameObject keyObject = GameObject.Find("Moon Key Light");
        if (keyObject != null)
        {
            Light key = keyObject.GetComponent<Light>();
            key.transform.rotation = Quaternion.LookRotation(-MoonDirection.normalized, Vector3.up);
            key.color = new Color(0.72f, 0.8f, 1f);
            key.intensity = 0.95f;
            key.shadows = LightShadows.Soft;
            key.shadowStrength = 0.55f;
            // Larger biases stop the striped shadow acne that crawled over the castle wall.
            key.shadowBias = 0.12f;
            key.shadowNormalBias = 0.8f;
        }
        ConfigureQuestQuality();
        // Torches keep their glow but drop shadow maps: four shadowed point lights are too heavy for Quest.
        foreach (Light light in Object.FindObjectsByType<Light>())
        {
            if (light.type == LightType.Point)
            {
                light.shadows = LightShadows.None;
            }
        }
        DynamicGI.UpdateEnvironment();
    }

    // The Quest build (Android default quality level) renders without real-time shadow maps: at that resolution
    // they shimmer with every head movement and two shadowed directional lights (moon + farm sun) cost frames,
    // and dropped frames are the main source of VR discomfort. Desktop quality levels keep their shadows.
    private static void ConfigureQuestQuality()
    {
        Object asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0];
        SerializedObject quality = new SerializedObject(asset);
        SerializedProperty levels = quality.FindProperty("m_QualitySettings");
        SerializedProperty defaults = quality.FindProperty("m_PerPlatformDefaultQuality");
        int androidLevel = -1;
        for (int i = 0; i < defaults.arraySize; i++)
        {
            SerializedProperty entry = defaults.GetArrayElementAtIndex(i);
            if (entry.FindPropertyRelative("first").stringValue == "Android")
            {
                androidLevel = entry.FindPropertyRelative("second").intValue;
            }
        }
        if (androidLevel < 0 || androidLevel >= levels.arraySize)
        {
            return;
        }
        SerializedProperty level = levels.GetArrayElementAtIndex(androidLevel);
        level.FindPropertyRelative("shadows").intValue = 0;
        quality.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
    }

    // ------------------------------------------------------------------ helpers

    private static void DestroyRoot(string name)
    {
        GameObject existing = GameObject.Find(name);
        while (existing != null)
        {
            Object.DestroyImmediate(existing);
            existing = GameObject.Find(name);
        }
    }

    private static Mesh TorusMesh(string name, float radius, float tube, int segments, int sides)
    {
        string path = "Assets/Meshes/" + name + ".asset";
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();
        for (int s = 0; s <= segments; s++)
        {
            float u = s / (float)segments * Mathf.PI * 2f;
            Vector3 center = new Vector3(Mathf.Cos(u) * radius, Mathf.Sin(u) * radius, 0f);
            for (int k = 0; k <= sides; k++)
            {
                float v = k / (float)sides * Mathf.PI * 2f;
                Vector3 normal = new Vector3(Mathf.Cos(u) * Mathf.Cos(v), Mathf.Sin(u) * Mathf.Cos(v), Mathf.Sin(v));
                vertices.Add(center + normal * tube);
                uvs.Add(new Vector2(s / (float)segments * 8f, k / (float)sides));
            }
        }
        int stride = sides + 1;
        for (int s = 0; s < segments; s++)
        {
            for (int k = 0; k < sides; k++)
            {
                int a = s * stride + k;
                int b = a + stride;
                triangles.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b });
            }
        }
        Mesh mesh = new Mesh { name = name };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        SaveMesh(mesh, path);
        return AssetDatabase.LoadAssetAtPath<Mesh>(path);
    }

    // Faceted crystal: an elongated octahedron with a slightly offset waist, saved once as an asset.
    private static Mesh CrystalMesh()
    {
        const string path = "Assets/Meshes/ArcaneCrystal.asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
        {
            return existing;
        }
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        Vector3 top = new Vector3(0f, 1f, 0f);
        Vector3 bottom = new Vector3(0f, -0.75f, 0f);
        const int sides = 6;
        Vector3[] waist = new Vector3[sides];
        for (int i = 0; i < sides; i++)
        {
            float a = i * Mathf.PI * 2f / sides;
            waist[i] = new Vector3(Mathf.Cos(a) * 0.42f, 0.12f, Mathf.Sin(a) * 0.42f);
        }
        for (int i = 0; i < sides; i++)
        {
            Vector3 a = waist[i];
            Vector3 b = waist[(i + 1) % sides];
            int v = vertices.Count;
            vertices.AddRange(new[] { top, b, a });
            triangles.AddRange(new[] { v, v + 1, v + 2 });
            v = vertices.Count;
            vertices.AddRange(new[] { bottom, a, b });
            triangles.AddRange(new[] { v, v + 1, v + 2 });
        }
        Mesh mesh = new Mesh { name = "ArcaneCrystal" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static void SaveMesh(Mesh mesh, string path)
    {
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(mesh, path);
            return;
        }
        existing.Clear();
        existing.SetVertices(new List<Vector3>(mesh.vertices));
        existing.SetUVs(0, new List<Vector2>(mesh.uv));
        existing.SetTriangles(mesh.triangles, 0);
        existing.RecalculateNormals();
        existing.RecalculateTangents();
        existing.RecalculateBounds();
        EditorUtility.SetDirty(existing);
    }

    private static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject gameObject = GameObject.CreatePrimitive(type);
        gameObject.name = name;
        gameObject.transform.SetParent(parent, false);
        gameObject.transform.localPosition = position;
        gameObject.transform.localScale = scale;
        Renderer renderer = gameObject.GetComponent<Renderer>();
        if (renderer != null && material != null)
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

    private static TextMesh CreateLabel(Transform parent, string text, Vector3 position, float characterSize, int fontSize, bool billboard)
    {
        GameObject labelObject = new GameObject("Michael Island Label");
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.localPosition = position;
        labelObject.transform.localRotation = Quaternion.identity;
        labelObject.transform.localScale = Vector3.one;
        TextMesh label = labelObject.AddComponent<TextMesh>();
        label.text = text;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.fontSize = fontSize;
        label.characterSize = characterSize;
        ArcaneWorkshop.WorldText.Style(label);
        if (billboard)
        {
            labelObject.AddComponent<WorldLabelBillboard>().ScaleWithDistance = false;
        }
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
