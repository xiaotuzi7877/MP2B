using System.Collections.Generic;
using ArcaneWorkshop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

/// <summary>
/// Author: Michael (Ziyang Li), implementing Justin's part of the team design.
/// Builds Justin's Astral Foundry island (idempotent): a floating round island mirrored from Lucas's farm across the
/// workshop's north-south axis, a stone bridge from Justin's south-east gate, and the three foundry machines.
/// It reuses Michael Island's materials, Kenney/KayKit props and night lighting so the three islands read as one world.
/// </summary>
public static class JustinFoundryBuilder
{
    private const string ScenePath = "Assets/Scenes/ArcaneWorkshop.unity";
    private const string RootName = "Justin Astral Foundry";
    private const string BridgeName = "mainToFoundryBridge";

    // Mirror of keliangFarmIsland (-28, 0, -36) / mainToFarmBridge (yaw 217.9, deck 29.31 m) across x = 0.
    public static readonly Vector3 IslandCenter = new Vector3(28f, 0f, -36f);
    public const float IslandYaw = 142.1f;
    public const float IslandRadius = 8f;
    private const float FloorTop = 0f;
    private const float BridgeLength = 29.31f;
    private const float BridgeWidth = 3f;
    private static readonly Vector3 BridgeCenter = new Vector3(14.52f, 0f, -18.67f);

    [MenuItem("MP2B/Build Justin Foundry Island")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            Debug.LogWarning("Exit Play Mode and wait for compilation before building Justin's foundry.");
            return;
        }
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (scene.isDirty)
            {
                Debug.LogError("Save or discard the current scene before building Justin's foundry.");
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
        BuildFoundry(manager, MichaelIslandBuilder.LoadMaterials());
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
    }

    /// <summary>Called by Build Michael Island too, so a full rebuild always ends with the foundry docked.</summary>
    internal static void BuildFoundry(ArcaneGameManager manager, MichaelIslandBuilder.Materials m)
    {
        DestroyNamed(RootName);
        DestroyNamed(BridgeName);

        IslandLinkGate gate = FindGate("Justin");
        TeammateDeliveryReceiver receiver = FindReceiver("Justin");
        if (gate == null || receiver == null)
        {
            Debug.LogError("Build Michael Island first: Justin's bridge gate and receiver are missing.");
            return;
        }

        Material ember = MichaelIslandBuilder.ColorMaterial("FoundryEmber", new Color(1f, 0.5f, 0.18f), 1.1f, 0.4f, 0f);
        Material starGlow = MichaelIslandBuilder.ColorMaterial("FoundryStar", new Color(1f, 0.9f, 0.62f), 3f, 0.8f, 0f);
        Material banner = MichaelIslandBuilder.ColorMaterial("LinkBannerJustin",
            Color.Lerp(MichaelIslandBuilder.JustinColor, new Color(0.1f, 0.08f, 0.2f), 0.45f), 0.15f, 0.1f, 0f);

        GameObject root = new GameObject(RootName);
        root.transform.position = IslandCenter;
        root.transform.rotation = Quaternion.Euler(0f, IslandYaw, 0f);
        Transform t = root.transform;

        GameObject floor = CreateIslandBody(t, m);
        CreateRim(t, m);
        Transform bridgeFar;
        Transform bridgeNear;
        GameObject deck = CreateBridge(m, out bridgeNear, out bridgeFar);
        DockGate(gate, receiver);

        // Machines: Star Catcher (left), Rune Well (right), Astral Forge (back centre); the bridge lands at local -Z.
        FoundryStation catcher = CreateStation(t, FoundryStationKind.StarCatcher, "Star Catcher", new Vector3(-3.6f, FloorTop, 0.6f),
            new Vector3(1.6f, 3.4f, 1.6f), 3.95f);
        FoundryStation well = CreateStation(t, FoundryStationKind.RuneWell, "Rune Well", new Vector3(3.6f, FloorTop, 0.6f),
            new Vector3(2.3f, 2.2f, 2.3f), 3.1f);
        FoundryStation forge = CreateStation(t, FoundryStationKind.AstralForge, "Astral Forge", new Vector3(0f, FloorTop, 4.2f),
            new Vector3(4.2f, 3.2f, 3f), 5f);

        Transform catchPoint;
        Transform[] fragmentSlots;
        Renderer[] lamps;
        BuildStarCatcher(catcher.transform, m, out catchPoint, out fragmentSlots, out lamps);
        Transform crystal = BuildRuneWell(well.transform, m);
        Transform hammer;
        Light forgeLight;
        ParticleSystem sparks;
        Transform output;
        BuildForge(forge.transform, m, ember, out hammer, out forgeLight, out sparks, out output);
        Renderer[] channel = BuildRuneChannel(t, m, well.transform.localPosition, forge.transform.localPosition);

        GameObject crash = new GameObject("Star Crash Point");
        crash.transform.SetParent(t, false);
        crash.transform.localPosition = new Vector3(5.2f, FloorTop + 0.2f, -2.4f);

        Transform core = MichaelIslandBuilder.CreateMeshObject("Forged Astral Core", MichaelIslandBuilder.CrystalMesh(), t,
            Vector3.zero, Vector3.zero, new Vector3(0.36f, 0.42f, 0.36f), m.justinGlow).transform;
        core.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        MichaelIslandBuilder.CreatePointLight("Core Glow", core, Vector3.zero, MichaelIslandBuilder.JustinColor, 1.6f, 3f);
        core.gameObject.SetActive(false);

        // Delivery path: forge pad -> island end of the bridge -> gate end -> through the ring -> receiver pad.
        Transform payload = receiver.PayloadAnchor;
        Transform[] path =
        {
            Waypoint("Delivery Bridge Far", bridgeFar.position + Vector3.up * 1.4f, t),
            Waypoint("Delivery Bridge Near", bridgeNear.position + Vector3.up * 1.4f, t),
            Waypoint("Delivery Gate Ring", gate.transform.position + Vector3.up * 2.15f, t),
            payload
        };

        CreateDecor(t, m, banner);
        CreateTitle(t);
        CreateLighting(t, m);
        AddTeleportAreas(floor, deck, t);

        AstralFoundry foundry = root.AddComponent<AstralFoundry>();
        foundry.Configure(manager, gate, receiver, catcher, well, forge, catchPoint, crash.transform, fragmentSlots, lamps,
            channel, crystal, hammer, forgeLight, sparks, core, output, path, bridgeNear, bridgeFar, IslandRadius,
            starGlow, MichaelIslandBuilder.JustinColor);
        foreach (FoundryStation station in new[] { catcher, well, forge })
        {
            station.Configure(station.Kind, foundry, station.Label);
        }
        Debug.Log("Justin's Astral Foundry built: island, bridge, Star Catcher, Rune Well, Astral Forge; Justin's gate docked.");
    }

    // ------------------------------------------------------------------ island body and rim

    private static GameObject CreateIslandBody(Transform t, MichaelIslandBuilder.Materials m)
    {
        GameObject floor = MichaelIslandBuilder.CreatePrimitive("Foundry Floor", PrimitiveType.Cylinder, t,
            new Vector3(0f, FloorTop - 0.15f, 0f), new Vector3(IslandRadius * 2f, 0.15f, IslandRadius * 2f), m.paving);
        Object.DestroyImmediate(floor.GetComponent<Collider>());
        floor.AddComponent<MeshCollider>().sharedMesh = floor.GetComponent<MeshFilter>().sharedMesh;
        GameObject edge = MichaelIslandBuilder.CreatePrimitive("Foundry Floor Edge", PrimitiveType.Cylinder, t,
            new Vector3(0f, FloorTop - 0.24f, 0f), new Vector3(IslandRadius * 2f + 0.5f, 0.12f, IslandRadius * 2f + 0.5f), m.rimStone);
        Object.DestroyImmediate(edge.GetComponent<Collider>());

        GameObject underside = new GameObject("Foundry Rock Underside");
        underside.transform.SetParent(t, false);
        underside.transform.localPosition = new Vector3(0f, -0.1f, 0f);
        underside.transform.localScale = Vector3.one * (IslandRadius / 11f);
        underside.AddComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes/MichaelIslandUnderside.asset");
        MeshRenderer rock = underside.AddComponent<MeshRenderer>();
        rock.sharedMaterial = m.rock;
        rock.shadowCastingMode = ShadowCastingMode.Off;
        for (int i = 0; i < 8; i++)
        {
            float a = (i * 45f + 20f) * Mathf.Deg2Rad;
            float r = 6.6f - (i % 2) * 1.2f;
            MichaelIslandBuilder.CreateImported("Foundry Underside Boulder " + i, MichaelIslandBuilder.Castle + "rocks-large.fbx", t,
                new Vector3(Mathf.Cos(a) * r, -1.6f - (i % 3) * 0.9f, Mathf.Sin(a) * r), new Vector3(180f, i * 53f, 0f),
                Vector3.one * (1.8f + (i % 3) * 0.4f), m.rock);
        }

        // Rune circle inlay in the plaza, like Michael Island's grid glow.
        Mesh circle = MichaelIslandBuilder.TorusMesh("FoundryPlazaCircle", 2.2f, 0.05f, 48, 4);
        GameObject inlay = MichaelIslandBuilder.CreateMeshObject("Foundry Plaza Inlay", circle, t, new Vector3(0f, FloorTop + 0.01f, 0.8f),
            new Vector3(90f, 0f, 0f), Vector3.one, m.justinGlow);
        inlay.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        return floor;
    }

    private static void CreateRim(Transform t, MichaelIslandBuilder.Materials m)
    {
        GameObject rim = new GameObject("Foundry Rim");
        rim.transform.SetParent(t, false);
        GameObject boundary = new GameObject("foundryBoundary");
        boundary.transform.SetParent(t, false);
        const float rimRadius = IslandRadius - 0.25f;
        const float panelScale = 1.25f;
        int count = Mathf.FloorToInt(Mathf.PI * 2f * rimRadius / panelScale);
        float step = Mathf.PI * 2f / count;
        for (int i = 0; i < count; i++)
        {
            float angle = i * step;
            Vector3 radial = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            float yaw = angle * Mathf.Rad2Deg;
            if (Mathf.Abs(Mathf.DeltaAngle(yaw, 180f)) < 14f)
            {
                continue; // bridge landing at local -Z
            }
            Vector3 position = radial * rimRadius;
            Vector3 tangent = new Vector3(radial.z, 0f, -radial.x);
            Quaternion rotation = Quaternion.LookRotation(tangent, Vector3.up);
            GameObject panel = MichaelIslandBuilder.CreateImported("Rim Panel " + i, MichaelIslandBuilder.Town + "balcony-wall-fence.fbx",
                rim.transform, position - rotation * Vector3.right * (0.45f * panelScale) + Vector3.up * FloorTop, Vector3.zero,
                new Vector3(panelScale, 0.95f, panelScale), m.rimStone);
            if (panel != null)
            {
                panel.transform.localRotation = Quaternion.LookRotation(-tangent, Vector3.up);
            }
            Vector3 post = new Vector3(Mathf.Sin(angle + step * 0.5f), 0f, Mathf.Cos(angle + step * 0.5f)) * rimRadius;
            MichaelIslandBuilder.CreateImported("Rim Post " + i, MichaelIslandBuilder.Town + "pillar-stone.fbx", rim.transform,
                post + Vector3.up * FloorTop, Vector3.zero, new Vector3(1.6f, 1.05f, 1.6f), m.rimStone);
            if (i % 5 == 0)
            {
                GameObject cap = MichaelIslandBuilder.CreatePrimitive("Rim Post Glow " + i, PrimitiveType.Sphere, rim.transform,
                    post + Vector3.up * (FloorTop + 1.12f), Vector3.one * 0.16f, m.justinGlow);
                Object.DestroyImmediate(cap.GetComponent<Collider>());
            }

            // Invisible safety wall (Ignore Raycast layer, like workshopBoundary): blocks walking, not pointing.
            GameObject wall = new GameObject("edgeBarrier" + i.ToString("00"));
            wall.transform.SetParent(boundary.transform, false);
            wall.transform.localPosition = radial * (IslandRadius - 0.1f) + Vector3.up * 1.5f;
            wall.transform.localRotation = Quaternion.LookRotation(radial, Vector3.up);
            wall.layer = 2;
            BoxCollider box = wall.AddComponent<BoxCollider>();
            box.size = new Vector3(panelScale + 0.25f, 3f, 0.18f);
        }
    }

    // ------------------------------------------------------------------ bridge and gate

    private static GameObject CreateBridge(MichaelIslandBuilder.Materials m, out Transform nearEnd, out Transform farEnd)
    {
        GameObject bridge = new GameObject(BridgeName);
        bridge.transform.position = BridgeCenter;
        bridge.transform.rotation = Quaternion.Euler(0f, IslandYaw, 0f);
        Transform b = bridge.transform;
        float half = BridgeLength * 0.5f;

        GameObject deck = MichaelIslandBuilder.CreatePrimitive("walkableDeck", PrimitiveType.Cube, b, new Vector3(0f, -0.12f, 0f),
            new Vector3(BridgeWidth, 0.24f, BridgeLength), m.rimStone);
        Material inlay = m.justinGlow;
        int slabs = 24;
        for (int i = 0; i < slabs; i++)
        {
            float z = -half + (i + 0.5f) * BridgeLength / slabs;
            GameObject slab = MichaelIslandBuilder.CreatePrimitive("Paving Slab " + i, PrimitiveType.Cube, b, new Vector3(0f, 0.005f, z),
                new Vector3(BridgeWidth - 0.35f, 0.02f, BridgeLength / slabs - 0.08f), m.paving);
            Object.DestroyImmediate(slab.GetComponent<Collider>());
            if (i % 3 == 1)
            {
                GameObject rune = MichaelIslandBuilder.CreatePrimitive("Bridge Rune " + i, PrimitiveType.Cube, b, new Vector3(0f, 0.02f, z),
                    new Vector3(0.5f, 0.012f, 0.12f), inlay);
                Object.DestroyImmediate(rune.GetComponent<Collider>());
            }
        }
        // Keel under the deck and stone corbels so the span reads as masonry, not a plank.
        GameObject keel = MichaelIslandBuilder.CreatePrimitive("Bridge Keel", PrimitiveType.Cube, b, new Vector3(0f, -0.55f, 0f),
            new Vector3(1.4f, 0.7f, BridgeLength - 1f), m.rock);
        Object.DestroyImmediate(keel.GetComponent<Collider>());

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * (BridgeWidth * 0.5f - 0.08f);
            GameObject rail = MichaelIslandBuilder.CreatePrimitive((side < 0 ? "left" : "right") + "Handrail", PrimitiveType.Cube, b,
                new Vector3(x, 1.08f, 0f), new Vector3(0.14f, 0.1f, BridgeLength - 0.6f), m.gold);
            Object.DestroyImmediate(rail.GetComponent<Collider>());
            GameObject low = MichaelIslandBuilder.CreatePrimitive((side < 0 ? "left" : "right") + "LowerRail", PrimitiveType.Cube, b,
                new Vector3(x, 0.42f, 0f), new Vector3(0.1f, 0.08f, BridgeLength - 0.6f), m.rimStone);
            Object.DestroyImmediate(low.GetComponent<Collider>());
            for (int k = 0; k <= 12; k++)
            {
                float z = -half + 0.5f + k * (BridgeLength - 1f) / 12f;
                GameObject post = MichaelIslandBuilder.CreatePrimitive("Post " + side + "-" + k, PrimitiveType.Cube, b,
                    new Vector3(x, 0.55f, z), new Vector3(0.2f, 1.1f, 0.2f), m.rimStone);
                Object.DestroyImmediate(post.GetComponent<Collider>());
                if (k % 3 == 0)
                {
                    GameObject gem = MichaelIslandBuilder.CreateMeshObject("Post Gem " + side + "-" + k, MichaelIslandBuilder.CrystalMesh(), b,
                        new Vector3(x, 1.28f, z), Vector3.zero, new Vector3(0.12f, 0.12f, 0.12f), inlay);
                    gem.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                }
            }
            GameObject barrier = new GameObject((side < 0 ? "left" : "right") + "SafetyBarrier");
            barrier.transform.SetParent(b, false);
            barrier.transform.localPosition = new Vector3(side * BridgeWidth * 0.5f, 0.75f, 0.3f);
            barrier.layer = 2;
            barrier.AddComponent<BoxCollider>().size = new Vector3(0.15f, 1.5f, BridgeLength - 0.6f);
        }
        for (int i = 0; i < 3; i++)
        {
            float z = -half + 4f + i * (BridgeLength - 8f) / 2f;
            MichaelIslandBuilder.CreatePointLight("Bridge Glow " + i, b, new Vector3(0f, 1.6f, z), MichaelIslandBuilder.JustinColor, 0.9f, 5f);
        }

        // Short side walls where the bridge meets Michael Island's rim gap (mirror of Keliang's gate barriers).
        AddGateBarrier("leftGateSafetyBarrier", new Vector3(4.51f, 0.75f, -8.85f), 102.3f);
        AddGateBarrier("rightGateSafetyBarrier", new Vector3(7.47f, 0.75f, -6.55f), 181.9f);

        nearEnd = new GameObject("Bridge Near End").transform;
        nearEnd.SetParent(b, false);
        nearEnd.localPosition = new Vector3(0f, 0f, -half);
        farEnd = new GameObject("Bridge Far End").transform;
        farEnd.SetParent(b, false);
        farEnd.localPosition = new Vector3(0f, 0f, half);
        return deck;

        void AddGateBarrier(string name, Vector3 position, float yaw)
        {
            GameObject wall = new GameObject(name);
            wall.transform.SetParent(b, true);
            wall.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            wall.layer = 2;
            wall.AddComponent<BoxCollider>().size = new Vector3(0.15f, 1.5f, 1.17f);
        }
    }

    private static void DockGate(IslandLinkGate gate, TeammateDeliveryReceiver receiver)
    {
        // The clickable ring barrier becomes a trigger; a separate solid seal (Ignore Raycast) blocks the walk until
        // the gate awakens on Lucas's first Bloom.
        BoxCollider barrier = gate.GetComponent<BoxCollider>();
        barrier.isTrigger = true;
        WalkBlocker blocker = gate.GetComponent<WalkBlocker>();
        if (blocker != null)
        {
            Object.DestroyImmediate(blocker);
        }
        Transform pier = gate.transform.Find("Justin Bridge Pier");
        if (pier != null)
        {
            Object.DestroyImmediate(pier.gameObject);
        }
        foreach (Transform child in gate.transform)
        {
            if (child.name == "Michael Island Label" && child.GetComponent<TextMesh>().text.Contains("DOCKS HERE"))
            {
                Object.DestroyImmediate(child.gameObject);
                break;
            }
        }
        Transform oldSeal = gate.transform.Find("Justin Gate Seal");
        if (oldSeal != null)
        {
            Object.DestroyImmediate(oldSeal.gameObject);
        }
        GameObject seal = new GameObject("Justin Gate Seal");
        seal.transform.SetParent(gate.transform, false);
        seal.layer = 2;
        BoxCollider sealBox = seal.AddComponent<BoxCollider>();
        sealBox.center = barrier.center;
        sealBox.size = barrier.size;
        seal.AddComponent<WalkBlocker>();
        gate.DockBridge(sealBox,
            "NEW - Justin's bridge is open. Walk or teleport across the south-east bridge to the Astral Foundry, " +
            "where falling stars are forged into the Astral Core the Convergence needs.");

        // Michael Island's invisible edge wall must open where the bridge lands (Lucas's side already has its gap).
        GameObject boundary = GameObject.Find("workshopBoundary");
        if (boundary != null)
        {
            List<GameObject> remove = new List<GameObject>();
            foreach (Transform wall in boundary.transform)
            {
                Vector3 p = wall.position;
                float yaw = Mathf.Atan2(p.x, p.z) * Mathf.Rad2Deg;
                if (Mathf.Abs(Mathf.DeltaAngle(yaw, MichaelIslandBuilder.JustinYaw)) < 9f)
                {
                    remove.Add(wall.gameObject);
                }
            }
            foreach (GameObject wall in remove)
            {
                Object.DestroyImmediate(wall);
            }
        }
        EditorUtility.SetDirty(gate);
        EditorUtility.SetDirty(receiver);
    }

    // ------------------------------------------------------------------ machines

    private static FoundryStation CreateStation(Transform t, FoundryStationKind kind, string name, Vector3 position, Vector3 size, float labelHeight)
    {
        GameObject station = new GameObject(name);
        station.transform.SetParent(t, false);
        station.transform.localPosition = position;
        // Face the bridge landing so labels and fronts greet the arriving player.
        Vector3 toLanding = new Vector3(0f, 0f, -IslandRadius) - new Vector3(position.x, 0f, position.z);
        station.transform.localRotation = Quaternion.LookRotation(toLanding.normalized, Vector3.up);
        BoxCollider box = station.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, size.y * 0.5f, 0f);
        box.size = size;
        station.AddComponent<WalkBlocker>();
        TextMesh label = MichaelIslandBuilder.CreateLabel(station.transform, name.ToUpperInvariant(), new Vector3(0f, labelHeight, 0f),
            0.024f, 50, true);
        label.color = Color.Lerp(MichaelIslandBuilder.JustinColor, Color.white, 0.45f);
        station.AddComponent<HoverHighlight>().Configure(label);
        FoundryStation component = station.AddComponent<FoundryStation>();
        component.Configure(kind, null, label);
        return component;
    }

    private static void BuildStarCatcher(Transform s, MichaelIslandBuilder.Materials m, out Transform catchPoint,
        out Transform[] slots, out Renderer[] lamps)
    {
        MichaelIslandBuilder.CreateImported("Catcher Tower", MichaelIslandBuilder.Castle + "tower-hexagon-base.fbx", s, Vector3.zero,
            Vector3.zero, new Vector3(1.1f, 1.7f, 1.1f), m.rimStone);
        MichaelIslandBuilder.CreateImported("Catcher Crown", MichaelIslandBuilder.Castle + "tower-hexagon-top.fbx", s,
            new Vector3(0f, 2.2f, 0f), Vector3.zero, new Vector3(1.18f, 0.7f, 1.18f), m.gold);
        // Tilted dish of gold rings that the stars fall into.
        Mesh dishOuter = MichaelIslandBuilder.TorusMesh("CatcherDishOuter", 1.05f, 0.07f, 40, 6);
        Mesh dishInner = MichaelIslandBuilder.TorusMesh("CatcherDishInner", 0.65f, 0.05f, 32, 6);
        MichaelIslandBuilder.CreateMeshObject("Catcher Dish Outer", dishOuter, s, new Vector3(0f, 3.05f, 0f), new Vector3(90f, 0f, 0f),
            Vector3.one, m.gold);
        MichaelIslandBuilder.CreateMeshObject("Catcher Dish Inner", dishInner, s, new Vector3(0f, 2.92f, 0f), new Vector3(90f, 0f, 0f),
            Vector3.one, m.justinGlow);
        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.PI / 3f;
            GameObject spoke = MichaelIslandBuilder.CreatePrimitive("Dish Spoke " + i, PrimitiveType.Cube, s,
                new Vector3(Mathf.Cos(a) * 0.55f, 2.85f, Mathf.Sin(a) * 0.55f), new Vector3(0.06f, 0.4f, 0.06f), m.gold);
            spoke.transform.localRotation = Quaternion.Euler(Mathf.Sin(a) * 35f, 0f, -Mathf.Cos(a) * 35f);
            Object.DestroyImmediate(spoke.GetComponent<Collider>());
        }
        GameObject point = new GameObject("Catch Point");
        point.transform.SetParent(s, false);
        point.transform.localPosition = new Vector3(0f, 3.15f, 0f);
        catchPoint = point.transform;

        // Fragment rack: up to three caught Star Fragments hover above the dish.
        slots = new Transform[AstralFoundry.MaxFragments];
        for (int i = 0; i < slots.Length; i++)
        {
            float a = (i * 120f + 90f) * Mathf.Deg2Rad;
            GameObject shard = MichaelIslandBuilder.CreateMeshObject("Star Fragment " + i, MichaelIslandBuilder.CrystalMesh(), s,
                new Vector3(Mathf.Cos(a) * 0.45f, 3.45f, Mathf.Sin(a) * 0.45f), Vector3.zero, new Vector3(0.16f, 0.22f, 0.16f),
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FoundryStar.mat"));
            shard.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            shard.SetActive(false);
            slots[i] = shard.transform;
        }

        // Four focus lamps around the base: one lights per stored focus charge.
        lamps = new Renderer[AstralFoundry.MaxCharges];
        for (int i = 0; i < lamps.Length; i++)
        {
            float a = (i * 90f + 45f) * Mathf.Deg2Rad;
            GameObject lamp = MichaelIslandBuilder.CreateMeshObject("Focus Lamp " + i, MichaelIslandBuilder.CrystalMesh(), s,
                new Vector3(Mathf.Cos(a) * 1.05f, 1.55f, Mathf.Sin(a) * 1.05f), Vector3.zero, new Vector3(0.14f, 0.18f, 0.14f), m.justinGlow);
            lamp.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            lamps[i] = lamp.GetComponent<Renderer>();
        }
        MichaelIslandBuilder.CreatePointLight("Catcher Light", s, new Vector3(0f, 3.4f, 0.4f), MichaelIslandBuilder.JustinColor, 1.2f, 4f);
    }

    private static Transform BuildRuneWell(Transform s, MichaelIslandBuilder.Materials m)
    {
        MichaelIslandBuilder.CreateImported("Rune Well Basin", MichaelIslandBuilder.Town + "fountain-round-detail.fbx", s, Vector3.zero,
            Vector3.zero, new Vector3(1.05f, 1f, 1.05f), m.rimStone);
        GameObject pool = MichaelIslandBuilder.CreatePrimitive("Rune Well Pool", PrimitiveType.Cylinder, s, new Vector3(0f, 0.42f, 0f),
            new Vector3(1.55f, 0.01f, 1.55f), m.flow != null ? m.flow : m.justinGlow);
        Object.DestroyImmediate(pool.GetComponent<Collider>());
        GameObject spin = new GameObject("Rune Well Crystal");
        spin.transform.SetParent(s, false);
        spin.transform.localPosition = new Vector3(0f, 1.55f, 0f);
        MichaelIslandBuilder.CreateMeshObject("Well Crystal", MichaelIslandBuilder.CrystalMesh(), spin.transform, Vector3.zero, Vector3.zero,
            new Vector3(0.38f, 0.5f, 0.38f), m.justinGlow);
        Mesh ring = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes/RuneRing.asset");
        if (ring != null)
        {
            MichaelIslandBuilder.CreateMeshObject("Well Rune Ring", ring, spin.transform, Vector3.zero, Vector3.zero, Vector3.one * 0.55f, m.gold);
        }
        MichaelIslandBuilder.CreatePointLight("Well Light", s, new Vector3(0f, 1.4f, 0.5f), MichaelIslandBuilder.JustinColor, 1f, 3.5f);
        return spin.transform;
    }

    private static void BuildForge(Transform s, MichaelIslandBuilder.Materials m, Material ember, out Transform hammer,
        out Light light, out ParticleSystem sparks, out Transform output)
    {
        // Furnace: a wide hexagon tower with a glowing mouth, flanked by two short towers.
        MichaelIslandBuilder.CreateImported("Forge Furnace", MichaelIslandBuilder.Castle + "tower-hexagon-base.fbx", s,
            new Vector3(0f, 0f, -0.9f), new Vector3(0f, 30f, 0f), new Vector3(2.3f, 2.3f, 1.9f), m.rimStone);
        MichaelIslandBuilder.CreateImported("Forge Chimney Crown", MichaelIslandBuilder.Castle + "tower-hexagon-top.fbx", s,
            new Vector3(0f, 3f, -0.9f), new Vector3(0f, 30f, 0f), new Vector3(1.6f, 1.3f, 1.35f), m.gold);
        GameObject mouth = MichaelIslandBuilder.CreatePrimitive("Forge Mouth", PrimitiveType.Cube, s, new Vector3(0f, 0.95f, 0.12f),
            new Vector3(1.3f, 1f, 0.08f), ember);
        Object.DestroyImmediate(mouth.GetComponent<Collider>());
        for (int side = -1; side <= 1; side += 2)
        {
            MichaelIslandBuilder.CreateImported("Forge Flank Tower " + side, MichaelIslandBuilder.Castle + "tower-hexagon-base.fbx", s,
                new Vector3(side * 2.05f, 0f, -0.8f), Vector3.zero, new Vector3(0.75f, 1.5f, 0.75f), m.rimStone);
            MichaelIslandBuilder.CreateImported("Forge Flank Lantern " + side, MichaelIslandBuilder.Town + "lantern.fbx", s,
                new Vector3(side * 2.05f, 1.95f, -0.8f), Vector3.zero, Vector3.one * 0.8f, null);
        }

        // Anvil in front of the furnace; the hammer swings on a pivot while forging.
        Material metal = m.metal != null ? m.metal : m.rimStone;
        Transform anvil = new GameObject("Anvil").transform;
        anvil.SetParent(s, false);
        anvil.localPosition = new Vector3(0f, 0f, 1.15f);
        anvil.localScale = Vector3.one * 1.25f;
        RemoveCollider(MichaelIslandBuilder.CreatePrimitive("Anvil Foot", PrimitiveType.Cube, anvil, new Vector3(0f, 0.25f, 0f),
            new Vector3(0.5f, 0.5f, 0.4f), metal));
        RemoveCollider(MichaelIslandBuilder.CreatePrimitive("Anvil Waist", PrimitiveType.Cube, anvil, new Vector3(0f, 0.6f, 0f),
            new Vector3(0.3f, 0.25f, 0.28f), metal));
        RemoveCollider(MichaelIslandBuilder.CreatePrimitive("Anvil Face", PrimitiveType.Cube, anvil, new Vector3(0f, 0.8f, 0f),
            new Vector3(0.95f, 0.16f, 0.38f), metal));
        RemoveCollider(MichaelIslandBuilder.CreatePrimitive("Anvil Rune", PrimitiveType.Cube, anvil, new Vector3(0f, 0.885f, 0f),
            new Vector3(0.6f, 0.01f, 0.08f), m.justinGlow));

        GameObject pivot = new GameObject("Hammer Pivot");
        pivot.transform.SetParent(anvil, false);
        pivot.transform.localPosition = new Vector3(0.18f, 0.55f, -0.45f);
        RemoveCollider(MichaelIslandBuilder.CreatePrimitive("Hammer Handle", PrimitiveType.Cylinder, pivot.transform,
            new Vector3(0f, 0.32f, 0f), new Vector3(0.05f, 0.32f, 0.05f), m.gold));
        RemoveCollider(MichaelIslandBuilder.CreatePrimitive("Hammer Head", PrimitiveType.Cube, pivot.transform,
            new Vector3(0f, 0.66f, 0f), new Vector3(0.18f, 0.18f, 0.32f), metal));
        pivot.transform.localRotation = Quaternion.Euler(-25f, 0f, 0f);
        hammer = pivot.transform;

        GameObject pad = new GameObject("Core Output");
        pad.transform.SetParent(anvil, false);
        pad.transform.localPosition = new Vector3(0f, 1.15f, 0f);
        output = pad.transform;

        light = MichaelIslandBuilder.CreatePointLight("Forge Light", s, new Vector3(0f, 1.1f, -1.4f), new Color(1f, 0.6f, 0.25f), 0.8f, 6f);
        light.transform.localPosition = new Vector3(0f, 1.3f, 1.9f);
        sparks = CreateSparks(anvil, ember);
    }

    private static ParticleSystem CreateSparks(Transform anvil, Material ember)
    {
        GameObject sparkObject = new GameObject("Forge Sparks");
        sparkObject.transform.SetParent(anvil, false);
        sparkObject.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        sparkObject.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        ParticleSystem system = sparkObject.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = system.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.75f, 0.3f), new Color(0.85f, 0.5f, 1f));
        main.gravityModifier = 1.2f;
        main.maxParticles = 150;
        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18, 18, 99, 0.7f) });
        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 40f;
        shape.radius = 0.1f;
        ParticleSystemRenderer renderer = sparkObject.GetComponent<ParticleSystemRenderer>();
        Material mote = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/IslandMote.mat");
        renderer.sharedMaterial = mote != null ? mote : ember;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        return system;
    }

    private static Renderer[] BuildRuneChannel(Transform t, MichaelIslandBuilder.Materials m, Vector3 from, Vector3 to)
    {
        // Glowing floor tiles from the Rune Well to the Forge: they light up in proportion to Rune Power.
        const int count = 8;
        Renderer[] tiles = new Renderer[count];
        Vector3 start = from + (to - from).normalized * 1.3f;
        Vector3 end = to - (to - from).normalized * 1.6f;
        Quaternion rotation = Quaternion.LookRotation(new Vector3(end.x - start.x, 0f, end.z - start.z), Vector3.up);
        for (int i = 0; i < count; i++)
        {
            Vector3 p = Vector3.Lerp(start, end, i / (count - 1f));
            GameObject tile = MichaelIslandBuilder.CreatePrimitive("Rune Channel " + i, PrimitiveType.Cube, t,
                new Vector3(p.x, FloorTop + 0.012f, p.z), new Vector3(0.32f, 0.02f, 0.22f), m.justinGlow);
            tile.transform.localRotation = rotation;
            RemoveCollider(tile);
            tile.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            tiles[i] = tile.GetComponent<Renderer>();
        }
        return tiles;
    }

    // ------------------------------------------------------------------ dressing

    private static void CreateDecor(Transform t, MichaelIslandBuilder.Materials m, Material banner)
    {
        // Back towers give the foundry an angular, industrial silhouette from Michael Island.
        Vector3[] towers = { new Vector3(-5.4f, 0f, 4.6f), new Vector3(5.4f, 0f, 4.6f) };
        for (int i = 0; i < towers.Length; i++)
        {
            MichaelIslandBuilder.CreateImported("Foundry Tower " + i, MichaelIslandBuilder.Castle + "tower-hexagon-base.fbx", t, towers[i],
                new Vector3(0f, 15f, 0f), new Vector3(1.2f, 3.2f, 1.2f), m.rimStone);
            MichaelIslandBuilder.CreateImported("Foundry Tower Cap " + i, MichaelIslandBuilder.Castle + "tower-hexagon-top.fbx", t,
                towers[i] + Vector3.up * 4.1f, new Vector3(0f, 15f, 0f), new Vector3(1.28f, 1.3f, 1.28f), m.gold);
            MichaelIslandBuilder.CreateImported("Foundry Banner " + i, MichaelIslandBuilder.Castle + "flag-banner-long.fbx", t,
                towers[i] + new Vector3(0f, 1.6f, -0.95f), new Vector3(0f, 90f, 0f), new Vector3(0.9f, 0.7f, 0.9f), banner);
            GameObject gem = MichaelIslandBuilder.CreateMeshObject("Foundry Tower Gem " + i, MichaelIslandBuilder.CrystalMesh(), t,
                towers[i] + Vector3.up * 6.1f, Vector3.zero, new Vector3(0.4f, 0.5f, 0.4f), m.justinGlow);
            gem.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            BoxCollider solid = new GameObject("Foundry Tower Collider " + i).AddComponent<BoxCollider>();
            solid.transform.SetParent(t, false);
            solid.transform.localPosition = towers[i] + Vector3.up * 2f;
            solid.size = new Vector3(2f, 4f, 2f);
            solid.gameObject.AddComponent<WalkBlocker>();
        }
        PlaceProp(t, "Ore Crates", MichaelIslandBuilder.Dungeon + "crates_stacked.fbx", new Vector3(-5.6f, 0f, -1.8f), 35f, 0.9f, new Vector3(1.2f, 1.6f, 1.2f));
        PlaceProp(t, "Star Gold Chest", MichaelIslandBuilder.Dungeon + "chest_gold.fbx", new Vector3(5.6f, 0f, -1.6f), -60f, 0.9f, new Vector3(1.0f, 0.9f, 0.8f));
        for (int side = -1; side <= 1; side += 2)
        {
            MichaelIslandBuilder.CreateImported("Landing Post " + side, MichaelIslandBuilder.Town + "pillar-stone.fbx", t,
                new Vector3(side * 1.9f, FloorTop, -6.9f), Vector3.zero, new Vector3(1.6f, 1.6f, 1.6f), m.rimStone);
            MichaelIslandBuilder.CreateImported("Landing Lantern " + side, MichaelIslandBuilder.Town + "lantern.fbx", t,
                new Vector3(side * 1.9f, FloorTop + 1.75f, -6.9f), Vector3.zero, Vector3.one * 0.8f, null);
        }
    }

    private static void PlaceProp(Transform t, string name, string path, Vector3 position, float yaw, float scale, Vector3 colliderSize)
    {
        GameObject prop = MichaelIslandBuilder.CreateImported(name, path, t, position, new Vector3(0f, yaw, 0f), Vector3.one * scale, null);
        if (prop == null)
        {
            return;
        }
        BoxCollider box = prop.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, colliderSize.y * 0.5f / scale, 0f);
        box.size = colliderSize / scale;
        prop.AddComponent<WalkBlocker>();
    }

    private static void CreateTitle(Transform t)
    {
        TextMesh title = MichaelIslandBuilder.CreateLabel(t, "JUSTIN'S ASTRAL FOUNDRY\nCATCH  •  CHARGE  •  FORGE",
            new Vector3(0f, 6.6f, 5.2f), 0.07f, 64, true);
        title.color = Color.Lerp(MichaelIslandBuilder.JustinColor, Color.white, 0.35f);
    }

    private static void CreateLighting(Transform t, MichaelIslandBuilder.Materials m)
    {
        MichaelIslandBuilder.CreatePointLight("Foundry Fill Light", t, new Vector3(0f, 5f, 0f), new Color(0.55f, 0.5f, 0.9f), 1.1f, 14f);
        ParticleSystem motes = MichaelIslandBuilder.CreateSwirl("Foundry Motes", t, new Vector3(0f, 2.5f, 1f), 5.5f,
            MichaelIslandBuilder.JustinColor, m.mote);
        ParticleSystem.MainModule main = motes.main;
        main.playOnAwake = true;
        ParticleSystem.EmissionModule emission = motes.emission;
        emission.rateOverTime = 10f;
        motes.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private static void AddTeleportAreas(GameObject floor, GameObject deck, Transform island)
    {
        GameObject rig = GameObject.Find("playerRig");
        TeleportationProvider provider = rig != null ? rig.GetComponentInChildren<TeleportationProvider>(true) : null;
        if (provider == null)
        {
            Debug.LogWarning("playerRig has no TeleportationProvider: foundry teleport surfaces left unconfigured.");
            return;
        }
        AddArea(floor, SharedRigTeleportArea.Region.Foundry, island, new Vector2(IslandRadius - 0.6f, IslandRadius - 0.6f), provider);
        AddArea(deck, SharedRigTeleportArea.Region.Bridge, deck.transform.parent,
            new Vector2(BridgeWidth * 0.5f - 0.4f, BridgeLength * 0.5f - 0.25f), provider);
    }

    // Same settings as KeliangRigSetup.AddArea, so all teleport surfaces behave alike.
    private static void AddArea(GameObject target, SharedRigTeleportArea.Region region, Transform space, Vector2 halfExtents,
        TeleportationProvider provider)
    {
        SharedRigTeleportArea area = target.AddComponent<SharedRigTeleportArea>();
        area.region = region;
        area.regionSpace = space;
        area.halfExtents = halfExtents;
        area.teleportationProvider = provider;
        area.interactionLayers = 1 << 31;
        area.matchOrientation = MatchOrientation.WorldSpaceUp;
        area.teleportTrigger = BaseTeleportationInteractable.TeleportTrigger.OnSelectExited;
        area.filterSelectionByHitNormal = true;
        area.upNormalToleranceDegrees = 25f;
        area.colliders.Clear();
        area.colliders.Add(target.GetComponent<Collider>());
    }

    // ------------------------------------------------------------------ helpers

    private static Transform Waypoint(string name, Vector3 world, Transform parent)
    {
        GameObject point = new GameObject(name);
        point.transform.SetParent(parent, true);
        point.transform.position = world;
        return point.transform;
    }

    private static void RemoveCollider(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }
    }

    private static IslandLinkGate FindGate(string owner)
    {
        foreach (IslandLinkGate gate in Object.FindObjectsByType<IslandLinkGate>())
        {
            if (gate.Owner == owner)
            {
                return gate;
            }
        }
        return null;
    }

    private static TeammateDeliveryReceiver FindReceiver(string source)
    {
        foreach (TeammateDeliveryReceiver receiver in Object.FindObjectsByType<TeammateDeliveryReceiver>())
        {
            if (receiver.ExpectedSource == source)
            {
                return receiver;
            }
        }
        return null;
    }

    private static void DestroyNamed(string name)
    {
        GameObject existing = GameObject.Find(name);
        while (existing != null)
        {
            Object.DestroyImmediate(existing);
            existing = GameObject.Find(name);
        }
    }
}
