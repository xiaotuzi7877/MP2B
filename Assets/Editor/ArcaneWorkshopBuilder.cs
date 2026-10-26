using System.IO;
using ArcaneWorkshop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ArcaneWorkshopBuilder
{
    private const string ScenePath = "Assets/Scenes/ArcaneWorkshop.unity";
    private const string AutoOpenedSceneSessionKey = "ArcaneWorkshop.AutoOpenedMainScene";
    private const string PrefabFolder = "Assets/Prefabs";
    private const string MaterialFolder = "Assets/Materials";
    private const string KayKitModelFolder = "Assets/ThirdParty/KayKitDungeon/Models";
    private const string KenneyModelFolder = "Assets/ThirdParty/KenneyNature/Models";
    private const string MeshFolder = "Assets/Meshes";

    private static Mesh ringMesh;
    private static Font labelFont;
    private static Material worldTextMaterial;
    private static Material runeCircleMaterial;
    private static Material blastMaterial;

    static ArcaneWorkshopBuilder()
    {
        EditorApplication.delayCall += EnsurePrototypeReady;
    }

    [MenuItem("MP2B/Rebuild Arcane Workshop Prototype")]
    public static void RebuildPrototype()
    {
        BuildPrototype(true);
    }

    [MenuItem("MP2B/Rebuild Arcane Workshop Prototype", true)]
    private static bool CanRebuildPrototype()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling;
    }

    [MenuItem("MP2B/Open Arcane Workshop Scene")]
    public static void OpenPrototype()
    {
        if (!File.Exists(ScenePath))
        {
            BuildPrototype(false);
        }
        EditorSceneManager.OpenScene(ScenePath);
    }

    private static void EnsurePrototypeReady()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        if (EditorApplication.isCompiling)
        {
            EditorApplication.delayCall += EnsurePrototypeReady;
            return;
        }

        if (!File.Exists(ScenePath))
        {
            BuildPrototype(false);
            SessionState.SetBool(AutoOpenedSceneSessionKey, true);
            return;
        }

        // A fresh clone has no local editor session state, so Unity opens an
        // untitled scene instead of the first scene in Build Settings. Open the
        // workshop once per editor session, but never replace a saved scene or
        // discard unsaved work.
        if (SessionState.GetBool(AutoOpenedSceneSessionKey, false))
        {
            return;
        }

        SessionState.SetBool(AutoOpenedSceneSessionKey, true);
        Scene activeScene = SceneManager.GetActiveScene();
        bool isUntitledStartupScene = string.IsNullOrEmpty(activeScene.path)
            && (!activeScene.isDirty || activeScene.rootCount == 0);
        if (isUntitledStartupScene)
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
    }

    private static void BuildPrototype(bool forceRebuild)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Exit Play Mode before rebuilding the MP2B prototype scene.");
            return;
        }

        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory(PrefabFolder);
        Directory.CreateDirectory(MaterialFolder);
        Directory.CreateDirectory(MeshFolder);
        AssetDatabase.Refresh();

        ringMesh = CreateRingMesh();
        labelFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        worldTextMaterial = CreateShaderMaterial("WorldText", "ArcaneWorkshop/World Text", labelFont.material.mainTexture);
        Material uiPanelMaterial = CreateShaderMaterial("UIPanel", "ArcaneWorkshop/UI Panel", null);
        runeCircleMaterial = CreateAdditiveMaterial("RuneCircle", CreateBandTexture("RuneCircleTexture", true),
            new Color(0.35f, 0.75f, 1f, 0.85f), new Vector2(0.05f, 0f), new Vector2(6f, 1f));
        blastMaterial = CreateAdditiveMaterial("AmplifierBlast", CreateBandTexture("BlastBandTexture", false),
            new Color(0.85f, 0.4f, 1f, 0.9f), new Vector2(0f, -0.55f), new Vector2(1f, 3f));

        Material rune = CreateMaterial("Rune", new Color(0.15f, 0.46f, 0.82f));
        Material soil = CreateMaterial("Soil", new Color(0.24f, 0.12f, 0.08f));
        Material herb = CreateMaterial("MoonHerb", new Color(0.32f, 0.92f, 0.46f));
        Material mana = CreateMaterial("Mana", new Color(0.18f, 0.75f, 1f));
        Material metal = CreateMaterial("DarkMetal", new Color(0.12f, 0.10f, 0.18f));
        Material crystal = CreateMaterial("Crystal", new Color(0.82f, 0.28f, 1f));
        Material flow = CreateFlowMaterial();
        Material floor = CreateMaterial("Floor", new Color(0.055f, 0.045f, 0.09f));
        Material stone = CreateTexturedMaterial(
            "KayKitDungeonStone",
            "Assets/ThirdParty/KayKitDungeon/Textures/dungeon_texture.png",
            Color.white);

        ConfigureEmission(rune, new Color(0.06f, 0.38f, 0.85f), 0.55f);
        ConfigureEmission(mana, new Color(0.04f, 0.58f, 0.82f), 0.7f);
        ConfigureEmission(crystal, new Color(0.48f, 0.08f, 0.78f), 0.65f);
        ConfigureEmission(flow, new Color(0.08f, 0.66f, 0.8f), 0.8f);

        ArcaneBuilding runePrefab = CreateBuildingPrefab(BuildingKind.RuneFoundation, rune, soil, herb, mana, metal, crystal, flow);
        ArcaneBuilding planterPrefab = CreateBuildingPrefab(BuildingKind.MoonPlanter, rune, soil, herb, mana, metal, crystal, flow);
        ArcaneBuilding stillPrefab = CreateBuildingPrefab(BuildingKind.ManaStill, rune, soil, herb, mana, metal, crystal, flow);
        ArcaneBuilding cauldronPrefab = CreateBuildingPrefab(BuildingKind.StarCauldron, rune, soil, herb, mana, metal, crystal, flow);
        ArcaneBuilding amplifierPrefab = CreateBuildingPrefab(BuildingKind.CrystalAmplifier, rune, soil, herb, mana, metal, crystal, flow);

        GatherableNode herbPrefab = CreateGatherablePrefab(NodeKind.MoonHerb, herb, crystal);
        GatherableNode clusterPrefab = CreateGatherablePrefab(NodeKind.CrystalCluster, herb, crystal);
        GatherableNode shardPrefab = CreateGatherablePrefab(NodeKind.CrystalShard, herb, crystal);
        CreateMerchantPortalPrefab(stone, flow, rune);

        if (forceRebuild && File.Exists(ScenePath))
        {
            AssetDatabase.DeleteAsset(ScenePath);
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "ArcaneWorkshop";

        GameObject managerObject = new GameObject("Arcane Workshop Game Manager");
        ArcaneGameManager manager = managerObject.AddComponent<ArcaneGameManager>();
        manager.buildingPrefabs = new[]
        {
            new BuildingPrefabEntry { kind = BuildingKind.RuneFoundation, prefab = runePrefab },
            new BuildingPrefabEntry { kind = BuildingKind.MoonPlanter, prefab = planterPrefab },
            new BuildingPrefabEntry { kind = BuildingKind.ManaStill, prefab = stillPrefab },
            new BuildingPrefabEntry { kind = BuildingKind.StarCauldron, prefab = cauldronPrefab },
            new BuildingPrefabEntry { kind = BuildingKind.CrystalAmplifier, prefab = amplifierPrefab }
        };
        manager.moonHerbPrefab = herbPrefab;
        manager.crystalClusterPrefab = clusterPrefab;
        manager.crystalShardPrefab = shardPrefab;
        manager.potionBottleVisual = AssetDatabase.LoadAssetAtPath<GameObject>(KayKitModelFolder + "/bottle_C_green.fbx");
        manager.worldFont = labelFont;
        manager.worldTextMaterial = worldTextMaterial;
        manager.uiPanelMaterial = uiPanelMaterial;

        Vector3 portalPosition = new Vector3(8.4f, 0f, -5.6f);
        GameObject portalAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/MerchantPortal.prefab");
        GameObject portalObject = (GameObject)PrefabUtility.InstantiatePrefab(portalAsset);
        portalObject.transform.position = portalPosition;
        // Face the player's default viewpoint (south-west) so the gate's veil reads clearly.
        portalObject.transform.rotation = Quaternion.LookRotation(new Vector3(-1f, 0f, -0.3f));
        manager.merchantPortal = portalObject.GetComponent<MerchantPortal>();

        GameObject audioObject = new GameObject("Arcane Audio");
        audioObject.AddComponent<ArcaneAudio>().SetLibrary(BuildSoundLibrary());

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.008f, 0.006f, 0.025f);
        camera.fieldOfView = 52f;
        camera.nearClipPlane = 0.05f;
        camera.allowHDR = true;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<OrbitCameraController>();
        // Match OrbitCameraController's starting orbit so the editor view shows the workshop too.
        Quaternion cameraRotation = Quaternion.Euler(42f, 38f, 0f);
        cameraObject.transform.SetPositionAndRotation(-(cameraRotation * Vector3.forward) * 13f, cameraRotation);

        GameObject lightObject = new GameObject("Moon Key Light");
        Light keyLight = lightObject.AddComponent<Light>();
        keyLight.type = LightType.Directional;
        keyLight.color = new Color(0.72f, 0.82f, 1f);
        keyLight.intensity = 0.72f;
        lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

        GameObject fillLightObject = new GameObject("Arcane Fill Light");
        Light fillLight = fillLightObject.AddComponent<Light>();
        fillLight.type = LightType.Point;
        fillLight.color = new Color(0.72f, 0.25f, 1f);
        fillLight.intensity = 2.6f;
        fillLight.range = 12f;
        fillLightObject.transform.position = new Vector3(0f, 5f, 0f);

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ground.name = "Floating Workshop Island";
        ground.transform.position = new Vector3(0f, -0.48f, 0f);
        ground.transform.localScale = new Vector3(22f, 0.32f, 22f);
        ground.GetComponent<Renderer>().sharedMaterial = floor;

        CreateWorkshopBackdrop(rune, crystal, metal, stone, floor);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.08f, 0.07f, 0.15f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.035f, 0.025f, 0.075f);
        RenderSettings.fogDensity = 0.012f;

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        Debug.Log("MP2B Arcane Workshop prototype generated at " + ScenePath);
    }

    private static ArcaneBuilding CreateBuildingPrefab(
        BuildingKind kind,
        Material rune,
        Material soil,
        Material herb,
        Material mana,
        Material metal,
        Material crystal,
        Material flow)
    {
        string path = PrefabFolder + "/" + kind + ".prefab";
        GameObject root = new GameObject(kind.ToString());
        ArcaneBuilding building = root.AddComponent<ArcaneBuilding>();
        building.ConfigureForKind(kind);
        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.isTrigger = kind == BuildingKind.StarCauldron;
        collider.size = kind == BuildingKind.RuneFoundation ? new Vector3(1.14f, 0.12f, 1.14f) : new Vector3(0.95f, 1.25f, 0.95f);
        collider.center = kind == BuildingKind.RuneFoundation ? new Vector3(0f, 0.02f, 0f) : new Vector3(0f, 0.55f, 0f);
        ArcaneWeights.Apply(root, ArcaneWeights.RealMassKg(kind));

        switch (kind)
        {
            case BuildingKind.RuneFoundation:
            {
                GameObject slab = CreatePrimitive("Rune Slab", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.02f, 0f), new Vector3(1.14f, 0.12f, 1.14f), rune);
                CreatePrimitive("Inset", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.085f, 0f), new Vector3(0.62f, 0.02f, 0.62f), metal).transform.rotation = Quaternion.Euler(0f, 45f, 0f);
                CreateMeshObject("Rune Circle", ringMesh, root.transform, new Vector3(0f, 0.1f, 0f), Vector3.zero, Vector3.one * 0.5f, runeCircleMaterial);
                AddLabel(root.transform, 0.46f);
                Visuals(root).Bind(null, null, null, slab.GetComponent<Renderer>(), null, null, null, null);
                break;
            }

            case BuildingKind.MoonPlanter:
            {
                CreatePrimitive("Planter", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.28f, 0f), new Vector3(0.86f, 0.5f, 0.86f), metal);
                GameObject soilBed = CreatePrimitive("Soil Bed", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.535f, 0f), new Vector3(0.76f, 0.03f, 0.76f), soil);
                GameObject foliage = CreateImportedModel("Moon Plant Foliage", KenneyModelFolder + "/plant_bush.fbx", root.transform, new Vector3(0f, 0.55f, 0f), new Vector3(0f, 25f, 0f), Vector3.one * 2.6f, null);
                GameObject bloom = CreateImportedModel("Moon Bloom", KenneyModelFolder + "/flower_yellowA.fbx", root.transform, new Vector3(0f, 0.5f, 0f), Vector3.zero, Vector3.one * 1.3f, null);
                CreatePrimitive("Ripe Glow", PrimitiveType.Sphere, bloom != null ? bloom.transform : root.transform, new Vector3(0f, 0.55f, 0f), Vector3.one * 0.12f, herb);
                if (bloom != null) bloom.SetActive(false);
                AddLabel(root.transform, 1.45f);
                Visuals(root).Bind(foliage != null ? foliage.transform : null, bloom != null ? bloom.transform : null,
                    soilBed.GetComponent<Renderer>(), null, null, null, null, null);
                break;
            }

            case BuildingKind.ManaStill:
                CreatePrimitive("Still Base", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.42f, 0f), new Vector3(0.62f, 0.42f, 0.62f), metal);
                GameObject vessel = CreatePrimitive("Mana Vessel", PrimitiveType.Sphere, root.transform, new Vector3(0f, 0.83f, 0f), new Vector3(0.62f, 0.72f, 0.62f), mana);
                Light stillLight = CreatePointLight("Mana Glow", root.transform, new Vector3(0f, 0.9f, 0f), new Color(0.3f, 0.75f, 1f), 1f, 2.2f);
                CreatePrimitive("Condenser", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 1.18f, 0f), new Vector3(0.17f, 0.30f, 0.17f), rune);
                CreateImportedModel("Green Reagent Bottle", KayKitModelFolder + "/bottle_A_green.fbx", root.transform, new Vector3(-0.42f, 0.05f, 0.28f), Vector3.zero, Vector3.one * 0.55f, null);
                CreateImportedModel("Brown Reagent Bottle", KayKitModelFolder + "/bottle_B_brown.fbx", root.transform, new Vector3(0.40f, 0.05f, 0.25f), new Vector3(0f, 28f, 0f), Vector3.one * 0.5f, null);
                AddLabel(root.transform, 1.72f);
                Visuals(root).Bind(null, null, null, vessel.GetComponent<Renderer>(), null, stillLight, null, null);
                break;

            case BuildingKind.StarCauldron:
                CreatePrimitive("Cauldron", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.44f, 0f), new Vector3(0.78f, 0.48f, 0.78f), metal);
                GameObject liquid = CreatePrimitive("Animated Brew Surface", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.92f, 0f), new Vector3(0.64f, 0.035f, 0.64f), flow);
                liquid.AddComponent<ArcaneFlowAnimator>();
                CreatePrimitive("Rim", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.91f, 0f), new Vector3(0.78f, 0.07f, 0.78f), rune);
                for (int leg = 0; leg < 3; leg++)
                {
                    float angle = leg * Mathf.PI * 2f / 3f;
                    CreatePrimitive("Cauldron Leg " + leg, PrimitiveType.Cylinder, root.transform,
                        new Vector3(Mathf.Cos(angle) * 0.32f, 0.2f, Mathf.Sin(angle) * 0.32f),
                        new Vector3(0.10f, 0.2f, 0.10f), metal).transform.localRotation = Quaternion.Euler(12f, 0f, -Mathf.Cos(angle) * 12f);
                }
                Light brewLight = CreatePointLight("Brew Glow", root.transform, new Vector3(0f, 1.25f, 0f), new Color(0.4f, 0.8f, 1f), 1f, 2.6f);
                AddLabel(root.transform, 1.62f);
                Visuals(root).Bind(null, null, null, null, liquid.transform, brewLight, null, null);
                break;

            case BuildingKind.CrystalAmplifier:
                CreatePrimitive("Amplifier Base", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.20f, 0f), new Vector3(0.68f, 0.20f, 0.68f), metal);
                GameObject core = CreatePrimitive("Floating Crystal", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.82f, 0f), new Vector3(0.52f, 0.82f, 0.52f), crystal);
                core.transform.rotation = Quaternion.Euler(18f, 45f, 18f);
                GameObject spinRing = CreateMeshObject("Resonance Rune Ring", ringMesh, root.transform, new Vector3(0f, 0.82f, 0f), new Vector3(12f, 0f, 0f), Vector3.one * 0.55f, runeCircleMaterial);
                GameObject blast = CreateMeshObject("Charge Blast Wave", ringMesh, root.transform, new Vector3(0f, 0.06f, 0f), Vector3.zero, Vector3.one * 0.62f, blastMaterial);
                Light ampLight = CreatePointLight("Charge Glow", root.transform, new Vector3(0f, 0.9f, 0f), new Color(0.8f, 0.35f, 1f), 1f, 2.2f);
                AddLabel(root.transform, 1.62f);
                Visuals(root).Bind(null, null, null, core.GetComponent<Renderer>(), null, ampLight, spinRing.transform, blast.GetComponent<Renderer>());
                break;
        }

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return saved.GetComponent<ArcaneBuilding>();
    }

    private static GatherableNode CreateGatherablePrefab(NodeKind kind, Material herb, Material crystal)
    {
        string path = PrefabFolder + "/" + kind + ".prefab";
        GameObject root = new GameObject(kind.ToString());
        GatherableNode node = root.AddComponent<GatherableNode>();
        node.Configure(kind);
        SphereCollider collider = root.AddComponent<SphereCollider>();
        collider.radius = kind == NodeKind.CrystalCluster ? 0.75f : 0.5f;
        ArcaneWeights.Apply(root, ArcaneWeights.RealMassKg(kind));

        if (kind == NodeKind.MoonHerb)
        {
            CreateImportedModel("Wild Moon Bloom", KenneyModelFolder + "/flower_yellowA.fbx", root.transform, Vector3.zero, Vector3.zero, Vector3.one * 1.45f, null);
            CreatePrimitive("Moonlight Core", PrimitiveType.Sphere, root.transform, new Vector3(0f, 0.64f, 0f), new Vector3(0.12f, 0.12f, 0.12f), herb);
            AddLabel(root.transform, 1.15f);
        }
        else
        {
            float scale = kind == NodeKind.CrystalCluster ? 0.72f : 0.36f;
            for (int i = 0; i < (kind == NodeKind.CrystalCluster ? 4 : 2); i++)
            {
                GameObject shard = CreatePrimitive("Crystal " + i, PrimitiveType.Cube, root.transform, new Vector3((i - 1.5f) * 0.18f, 0.36f + i * 0.06f, 0f), new Vector3(scale * 0.34f, scale, scale * 0.34f), crystal);
                shard.transform.rotation = Quaternion.Euler(i * 8f, 45f + i * 18f, i * 11f);
            }
            AddLabel(root.transform, kind == NodeKind.CrystalCluster ? 1.45f : 0.92f);
        }

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return saved.GetComponent<GatherableNode>();
    }

    private const string AudioFolder = "Assets/ThirdParty/KenneyAudio/";

    // Per-sound volume is relative within its category; ArcaneAudio applies category and master
    // volume on top. Heavy events (star impact, crumble) stay loudest, menu clicks quietest.
    private static SoundEntry[] BuildSoundLibrary()
    {
        return new[]
        {
            Sound(SoundId.UiSelect, SoundCategory.Interface, 0.7f, 0.05f, 0.05f, "select_001", "select_002", "select_003"),
            Sound(SoundId.UiBack, SoundCategory.Interface, 0.6f, 0.03f, 0.1f, "back_001"),
            Sound(SoundId.UiError, SoundCategory.Interface, 0.55f, 0.02f, 0.25f, "error_004", "error_006"),
            Sound(SoundId.TutorialPage, SoundCategory.Interface, 0.8f, 0.08f, 0.5f, "bookFlip1", "bookFlip2", "bookFlip3"),
            Sound(SoundId.BuildFoundation, SoundCategory.Interaction, 0.75f, 0.08f, 0.05f, "impactMining_000", "impactMining_001", "impactMining_002"),
            Sound(SoundId.BuildMachine, SoundCategory.Interaction, 0.7f, 0.06f, 0.05f, "impactMetal_medium_000", "impactMetal_medium_001"),
            Sound(SoundId.HerbPluck, SoundCategory.Interaction, 0.8f, 0.1f, 0.05f, "pluck_001", "pluck_002"),
            Sound(SoundId.CrystalShatter, SoundCategory.Impact, 0.6f, 0.08f, 0.1f, "impactGlass_heavy_000", "impactGlass_heavy_001", "impactGlass_heavy_002"),
            Sound(SoundId.ShardPick, SoundCategory.Interaction, 0.6f, 0.12f, 0.05f, "impactGlass_light_000", "impactGlass_light_001", "impactGlass_light_002"),
            Sound(SoundId.WaterDrop, SoundCategory.Interaction, 0.45f, 0.15f, 0.08f, "drop_001", "drop_002", "drop_003"),
            Sound(SoundId.PlanterHarvest, SoundCategory.Interaction, 0.7f, 0.05f, 0.2f, "confirmation_002"),
            Sound(SoundId.StillCollect, SoundCategory.Interaction, 0.6f, 0.1f, 0.2f, "glass_001", "glass_002", "glass_003"),
            Sound(SoundId.BrewStart, SoundCategory.Interaction, 0.6f, 0.06f, 0.3f, "metalPot1", "metalPot2", "metalPot3"),
            Sound(SoundId.IngredientPlop, SoundCategory.Interaction, 0.4f, 0.15f, 0.06f, "impactSoft_medium_000", "impactSoft_medium_001", "impactSoft_medium_002"),
            Sound(SoundId.BrewComplete, SoundCategory.Music, 0.6f, 0f, 0.5f, "jingles_PIZZI10"),
            Sound(SoundId.SparkLaunch, SoundCategory.Interaction, 0.35f, 0.08f, 0.1f, "laserRetro_000", "laserRetro_001"),
            Sound(SoundId.SparkHit, SoundCategory.Interaction, 0.45f, 0.08f, 0.1f, "forceField_000", "forceField_001"),
            Sound(SoundId.StarWhoosh, SoundCategory.Impact, 0.35f, 0.1f, 1f, "thrusterFire_000"),
            Sound(SoundId.StarImpact, SoundCategory.Impact, 0.8f, 0.08f, 0.5f, "lowFrequency_explosion_000", "explosionCrunch_000"),
            Sound(SoundId.BuildingCrumble, SoundCategory.Impact, 0.85f, 0.05f, 0.5f, "explosionCrunch_002"),
            Sound(SoundId.Repair, SoundCategory.Interaction, 0.6f, 0.1f, 0.1f, "impactMetal_light_000", "impactMetal_light_001", "impactMetal_light_002"),
            Sound(SoundId.PotionSend, SoundCategory.Interaction, 0.5f, 0.05f, 0.2f, "maximize_003"),
            Sound(SoundId.CoinSale, SoundCategory.Interaction, 0.75f, 0.05f, 0.2f, "handleCoins", "handleCoins2"),
            Sound(SoundId.GoalComplete, SoundCategory.Music, 0.7f, 0f, 1f, "jingles_STEEL04"),
            Sound(SoundId.Ending, SoundCategory.Music, 0.9f, 0f, 2f, "jingles_STEEL00")
        };
    }

    private static SoundEntry Sound(SoundId id, SoundCategory category, float volume, float pitchJitter, float cooldown, params string[] files)
    {
        System.Collections.Generic.List<AudioClip> clips = new System.Collections.Generic.List<AudioClip>();
        foreach (string file in files)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + file + ".ogg");
            if (clip != null)
            {
                clips.Add(clip);
            }
            else
            {
                Debug.LogWarning("Missing sound: " + file);
            }
        }
        return new SoundEntry { id = id, category = category, clips = clips.ToArray(), volume = volume, pitchJitter = pitchJitter, cooldown = cooldown };
    }

    private static MerchantPortal CreateMerchantPortalPrefab(Material stone, Material flow, Material rune)
    {
        string path = PrefabFolder + "/MerchantPortal.prefab";
        GameObject root = new GameObject("MerchantPortal");
        MerchantPortal portal = root.AddComponent<MerchantPortal>();
        SphereCollider collider = root.AddComponent<SphereCollider>();
        collider.center = new Vector3(0f, 1.3f, 0f);
        collider.radius = 1f;
        ArcaneWeights.Apply(root, ArcaneWeights.MerchantGateRealMassKg);

        CreateImportedModel("Gate Pillar Left", KayKitModelFolder + "/pillar_decorated.fbx", root.transform, new Vector3(-1.25f, 0f, 0f), Vector3.zero, Vector3.one * 0.68f, stone);
        CreateImportedModel("Gate Pillar Right", KayKitModelFolder + "/pillar_decorated.fbx", root.transform, new Vector3(1.25f, 0f, 0f), Vector3.zero, Vector3.one * 0.68f, stone);
        CreatePrimitive("Gate Step", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.05f, 0f), new Vector3(2.4f, 0.05f, 1.2f), rune);

        GameObject veil = CreatePrimitive("Moon Gate Veil", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 1.3f, 0f), new Vector3(1.15f, 0.01f, 1.15f), flow);
        veil.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        veil.AddComponent<ArcaneFlowAnimator>();

        GameObject swirl = CreateMeshObject("Moon Gate Swirl", ringMesh, root.transform, new Vector3(0f, 1.3f, 0f), new Vector3(90f, 0f, 0f), Vector3.one * 0.85f, blastMaterial);
        CreatePointLight("Gate Glow", root.transform, new Vector3(0f, 1.3f, 0.6f), new Color(0.55f, 0.85f, 1f), 1.6f, 3.5f);
        AddLabel(root.transform, 2.75f);

        // Designer-authored delivery route (world space): from the potion shelves, out over the
        // island rim, and into the gate's center. Potions follow it with a Catmull-Rom spline.
        Vector3 gateCenter = new Vector3(8.4f, 1.3f, -5.6f);
        portal.Configure(swirl.transform, new[]
        {
            new Vector3(6.3f, 2.3f, 3.2f),
            new Vector3(9.3f, 2.7f, 2.0f),
            new Vector3(10.4f, 3.1f, -1.0f),
            new Vector3(10.0f, 2.4f, -3.8f),
            gateCenter
        });

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return saved.GetComponent<MerchantPortal>();
    }

    private static void CreateWorkshopBackdrop(Material rune, Material crystal, Material metal, Material stone, Material floor)
    {
        GameObject environment = new GameObject("Arcane Workshop Environment");

        CreatePrimitive("Raised Ritual Dais", PrimitiveType.Cylinder, environment.transform, new Vector3(0f, -0.23f, 0f), new Vector3(11.2f, 0.15f, 11.2f), metal);
        CreatePrimitive("Inner Rune Disc", PrimitiveType.Cylinder, environment.transform, new Vector3(0f, -0.13f, 0f), new Vector3(9.8f, 0.035f, 9.8f), floor);

        for (int ray = 0; ray < 12; ray++)
        {
            float angle = ray * 30f;
            GameObject inlay = CreatePrimitive("Rune Inlay " + ray, PrimitiveType.Cube, environment.transform,
                Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, -0.08f, 5.15f),
                new Vector3(0.055f, 0.025f, 1.45f), ray % 2 == 0 ? rune : crystal);
            inlay.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
        }

        string[] backWalls = { "wall_arched.fbx", "wall.fbx", "wall_window_open.fbx", "wall.fbx", "wall_arched.fbx" };
        for (int i = 0; i < backWalls.Length; i++)
        {
            CreateImportedModel("Workshop Wall " + i, KayKitModelFolder + "/" + backWalls[i], environment.transform,
                new Vector3((i - 2) * 3.35f, -0.15f, 9.15f), Vector3.zero, Vector3.one * 1.65f, stone);
        }

        CreateImportedModel("Left Corner Wall", KayKitModelFolder + "/wall_corner.fbx", environment.transform,
            new Vector3(-8.15f, -0.15f, 7.7f), new Vector3(0f, 90f, 0f), Vector3.one * 1.65f, stone);
        CreateImportedModel("Right Corner Wall", KayKitModelFolder + "/wall_corner.fbx", environment.transform,
            new Vector3(8.15f, -0.15f, 7.7f), new Vector3(0f, -90f, 0f), Vector3.one * 1.65f, stone);

        for (int i = 0; i < 6; i++)
        {
            float x = -8.2f + i * 3.28f;
            CreateImportedModel("Carved Pillar " + i, KayKitModelFolder + "/pillar_decorated.fbx", environment.transform,
                new Vector3(x, -0.12f, 8.55f), Vector3.zero, Vector3.one * 1.65f, stone);

            if (i > 0 && i < 5)
            {
                CreateImportedModel("Mounted Torch " + i, KayKitModelFolder + "/torch_mounted.fbx", environment.transform,
                    new Vector3(x, 1.65f, 8.10f), new Vector3(0f, 180f, 0f), Vector3.one * 1.2f, stone);
                CreatePointLight("Torchlight " + i, environment.transform, new Vector3(x, 2.05f, 7.6f), new Color(1f, 0.48f, 0.15f), 1.35f, 6f);
            }
        }

        CreateImportedModel("Alchemist Workbench", KayKitModelFolder + "/table_long_decorated_A.fbx", environment.transform,
            new Vector3(-7.4f, 0f, 3.7f), new Vector3(0f, 90f, 0f), Vector3.one * 1.25f, stone);
        CreateImportedModel("Potion Shelves", KayKitModelFolder + "/shelves.fbx", environment.transform,
            new Vector3(7.65f, 0f, 4.1f), new Vector3(0f, -90f, 0f), Vector3.one * 1.35f, stone);
        CreateImportedModel("Candle Shelf", KayKitModelFolder + "/shelf_small_candles.fbx", environment.transform,
            new Vector3(6.7f, 0.2f, 7.8f), new Vector3(0f, 180f, 0f), Vector3.one * 1.15f, stone);
        CreateImportedModel("Arcane Chest", KayKitModelFolder + "/chest_gold.fbx", environment.transform,
            new Vector3(-7.3f, 0f, 6.2f), new Vector3(0f, 30f, 0f), Vector3.one * 1.15f, stone);
        CreateImportedModel("Supply Barrels", KayKitModelFolder + "/barrel_large_decorated.fbx", environment.transform,
            new Vector3(7.6f, 0f, 6.6f), new Vector3(0f, -20f, 0f), Vector3.one * 1.05f, stone);
        CreateImportedModel("Supply Crates", KayKitModelFolder + "/crates_stacked.fbx", environment.transform,
            new Vector3(-8.0f, 0f, 1.2f), new Vector3(0f, 25f, 0f), Vector3.one * 1.1f, stone);
        CreateImportedModel("Workshop Banner", KayKitModelFolder + "/banner_patternA_blue.fbx", environment.transform,
            new Vector3(0f, 2.0f, 8.76f), new Vector3(0f, 180f, 0f), Vector3.one * 1.4f, stone);
        CreateImportedModel("Coin Hoard", KayKitModelFolder + "/coin_stack_large.fbx", environment.transform,
            new Vector3(-6.55f, 0.08f, 5.75f), Vector3.zero, Vector3.one, stone);

        Vector3[] treePositions =
        {
            new Vector3(-8.8f, 0f, -3.0f), new Vector3(8.7f, 0f, -2.2f),
            new Vector3(-9.7f, 0f, 0.2f), new Vector3(8.9f, 0f, 1.4f)
        };
        for (int i = 0; i < treePositions.Length; i++)
        {
            string tree = i % 2 == 0 ? "/tree_oak.fbx" : "/tree_default.fbx";
            CreateImportedModel("Moonlit Tree " + i, KenneyModelFolder + tree, environment.transform,
                treePositions[i], new Vector3(0f, i * 71f, 0f), Vector3.one * (1.15f + i * 0.08f), null);
        }

        Vector3[] naturePositions =
        {
            new Vector3(-6.6f, 0f, -6.6f), new Vector3(-3.1f, 0f, -8.8f),
            new Vector3(3.0f, 0f, -8.9f), new Vector3(6.7f, 0f, -6.4f)
        };
        for (int i = 0; i < naturePositions.Length; i++)
        {
            string rock = i % 2 == 0 ? "/rock_largeA.fbx" : "/rock_largeB.fbx";
            CreateImportedModel("Floating Island Rock " + i, KenneyModelFolder + rock, environment.transform,
                naturePositions[i], new Vector3(0f, i * 53f, 0f), Vector3.one * (1.15f + i * 0.1f), null);
            CreateImportedModel("Arcane Mushrooms " + i, KenneyModelFolder + "/mushroom_redGroup.fbx", environment.transform,
                naturePositions[i] + new Vector3(0.8f, 0.05f, 0.45f), new Vector3(0f, i * 42f, 0f), Vector3.one * 0.72f, null);
            CreateImportedModel("Edge Grass " + i, KenneyModelFolder + "/grass_large.fbx", environment.transform,
                naturePositions[i] + new Vector3(-0.75f, 0.05f, -0.2f), new Vector3(0f, i * 31f, 0f), Vector3.one * 0.82f, null);
        }

        for (int i = 0; i < 10; i++)
        {
            float angle = i * Mathf.PI * 2f / 10f;
            Vector3 position = new Vector3(Mathf.Cos(angle) * 10.25f, -0.82f, Mathf.Sin(angle) * 10.25f);
            CreateImportedModel("Underside Rock " + i, KenneyModelFolder + "/rock_largeA.fbx", environment.transform,
                position, new Vector3(145f, i * 37f, 0f), new Vector3(1.35f, 2.2f, 1.35f), null);
        }
    }

    private static GameObject CreateImportedModel(
        string name,
        string assetPath,
        Transform parent,
        Vector3 localPosition,
        Vector3 localEulerAngles,
        Vector3 localScale,
        Material overrideMaterial)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (asset == null)
        {
            Debug.LogWarning("Optional art asset was not found: " + assetPath);
            return null;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(asset) as GameObject;
        if (instance == null)
        {
            return null;
        }

        instance.name = name;
        if (parent != null)
        {
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.Euler(localEulerAngles);
            instance.transform.localScale = localScale;
        }
        else
        {
            instance.transform.position = localPosition;
            instance.transform.rotation = Quaternion.Euler(localEulerAngles);
            instance.transform.localScale = localScale;
        }

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

    private static Light CreatePointLight(string name, Transform parent, Vector3 localPosition, Color color, float intensity, float range)
    {
        GameObject lightObject = new GameObject(name);
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = localPosition;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = range > 4f ? LightShadows.Soft : LightShadows.None;
        return light;
    }

    private static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject primitive = GameObject.CreatePrimitive(type);
        primitive.name = name;
        primitive.transform.localScale = localScale;

        if (parent != null)
        {
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
        }
        else
        {
            primitive.transform.position = localPosition;
        }

        Collider collider = primitive.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }
        primitive.GetComponent<Renderer>().sharedMaterial = material;
        return primitive;
    }

    private static void AddLabel(Transform parent, float height)
    {
        GameObject labelObject = new GameObject("Stat Label");
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.localPosition = new Vector3(0f, height, 0f);
        TextMesh text = labelObject.AddComponent<TextMesh>();
        text.font = labelFont;
        labelObject.GetComponent<MeshRenderer>().sharedMaterial = worldTextMaterial;
        text.text = parent.name;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontSize = 42;
        text.characterSize = 0.025f;
        text.color = new Color(0.84f, 0.96f, 1f);
        labelObject.AddComponent<WorldLabelBillboard>();
    }

    private static Material CreateMaterial(string name, Color color)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            SetMaterialColor(existing, color);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        Shader shader = Shader.Find("Standard");
        Material material = new Material(shader) { name = name };
        SetMaterialColor(material, color);
        material.SetFloat("_Glossiness", 0.35f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Material CreateShaderMaterial(string name, string shaderName, Texture texture)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Shader shader = Shader.Find(shaderName);
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = shader;
        if (texture != null)
        {
            material.mainTexture = texture;
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateTexturedMaterial(string name, string texturePath, Color tint)
    {
        Material material = CreateMaterial(name, tint);
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture != null && material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", texture);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ConfigureEmission(Material material, Color color, float intensity)
    {
        if (material == null || !material.HasProperty("_EmissionColor"))
        {
            return;
        }

        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color * intensity);
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(material);
    }

    private static Material CreateFlowMaterial()
    {
        string texturePath = MaterialFolder + "/ArcaneFlowTexture.asset";
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture == null)
        {
            texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            texture.name = "Arcane Flow Stripes";
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    bool stripe = ((x + y * 2) / 5) % 2 == 0;
                    texture.SetPixel(x, y, stripe ? new Color(0.15f, 1f, 0.92f) : new Color(0.38f, 0.12f, 0.72f));
                }
            }
            texture.Apply();
            AssetDatabase.CreateAsset(texture, texturePath);
        }

        Material material = CreateMaterial("AnimatedArcaneFlow", new Color(0.25f, 0.95f, 1f));
        Shader distortion = Shader.Find("ArcaneWorkshop/Distorted Brew");
        if (distortion != null)
        {
            material.shader = distortion;
            material.SetTexture("_NoiseTex", CreateNoiseTexture());
            material.SetTextureScale("_NoiseTex", new Vector2(1.5f, 1.5f));
        }
        material.SetTexture("_MainTex", texture);
        material.SetTextureScale("_MainTex", new Vector2(2f, 2f));
        EditorUtility.SetDirty(material);
        return material;
    }

    private static BuildingVisuals Visuals(GameObject root)
    {
        return root.AddComponent<BuildingVisuals>();
    }

    private static Texture2D CreateNoiseTexture()
    {
        string path = MaterialFolder + "/ArcaneNoiseTexture.asset";
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null)
        {
            return existing;
        }

        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Arcane Distortion Noise";
        texture.wrapMode = TextureWrapMode.Repeat;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Tileable noise: sample Perlin on a torus so the texture wraps without seams.
                float u = x / (float)size * Mathf.PI * 2f;
                float v = y / (float)size * Mathf.PI * 2f;
                float r = TileNoise(u, v, 0f);
                float g = TileNoise(u, v, 37f);
                float b = TileNoise(u, v, 71f);
                texture.SetPixel(x, y, new Color(r, g, b, 1f));
            }
        }
        texture.Apply();
        AssetDatabase.CreateAsset(texture, path);
        return texture;
    }

    private static float TileNoise(float u, float v, float seed)
    {
        float nx = Mathf.Cos(u) * 1.6f + seed;
        float ny = Mathf.Sin(u) * 1.6f + Mathf.Cos(v) * 1.6f + seed;
        float nz = Mathf.Sin(v) * 1.6f;
        return Mathf.Clamp01((Mathf.PerlinNoise(nx + nz, ny) + Mathf.PerlinNoise(ny * 2f, nx * 2f + nz) * 0.5f) / 1.5f);
    }

    // Flat annulus. U runs around the ring, V runs from the inner edge (0) to the outer edge (1),
    // and vertex alpha fades toward the rim so a V-scrolling texture reads as an outward pulse.
    private static Mesh CreateRingMesh()
    {
        string path = MeshFolder + "/RuneRing.asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
        {
            return existing;
        }

        const int segments = 64;
        const float inner = 0.55f;
        const float outer = 1f;
        Vector3[] vertices = new Vector3[(segments + 1) * 2];
        Vector2[] uvs = new Vector2[vertices.Length];
        Color[] colors = new Color[vertices.Length];
        int[] triangles = new int[segments * 6];
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = t * Mathf.PI * 2f;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            vertices[i * 2] = direction * inner;
            vertices[i * 2 + 1] = direction * outer;
            uvs[i * 2] = new Vector2(t, 0f);
            uvs[i * 2 + 1] = new Vector2(t, 1f);
            colors[i * 2] = Color.white;
            colors[i * 2 + 1] = new Color(1f, 1f, 1f, 0.15f);
            if (i < segments)
            {
                int k = i * 6;
                int a = i * 2;
                triangles[k] = a;
                triangles[k + 1] = a + 2;
                triangles[k + 2] = a + 1;
                triangles[k + 3] = a + 1;
                triangles[k + 4] = a + 2;
                triangles[k + 5] = a + 3;
            }
        }

        Mesh mesh = new Mesh { name = "Rune Ring" };
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    // alongU: dashed rune marks around the ring. Otherwise: soft concentric bands along V.
    private static Texture2D CreateBandTexture(string name, bool alongU)
    {
        string path = MaterialFolder + "/" + name + ".asset";
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null)
        {
            return existing;
        }

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = name;
        texture.wrapMode = TextureWrapMode.Repeat;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size;
                float v = y / (float)size;
                float alpha;
                if (alongU)
                {
                    float dash = Mathf.SmoothStep(0.1f, 0.2f, u) * (1f - Mathf.SmoothStep(0.6f, 0.7f, u));
                    float rim = Mathf.Max(1f - Mathf.Abs(v - 0.15f) * 12f, 1f - Mathf.Abs(v - 0.85f) * 12f);
                    float glyph = dash * (1f - Mathf.SmoothStep(0.25f, 0.4f, Mathf.Abs(v - 0.5f)));
                    alpha = Mathf.Clamp01(Mathf.Max(rim, glyph));
                }
                else
                {
                    float wave = Mathf.Sin(v * Mathf.PI * 2f) * 0.5f + 0.5f;
                    alpha = Mathf.Pow(wave, 3f);
                }
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.Apply();
        AssetDatabase.CreateAsset(texture, path);
        return texture;
    }

    private static Material CreateAdditiveMaterial(string name, Texture2D texture, Color color, Vector2 scroll, Vector2 tiling)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Shader shader = Shader.Find("ArcaneWorkshop/Scrolling Additive");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = shader;
        material.mainTexture = texture;
        material.mainTextureScale = tiling;
        material.SetColor("_Color", color);
        if (material.HasProperty("_Scroll"))
        {
            material.SetVector("_Scroll", new Vector4(scroll.x, scroll.y, 0f, 0f));
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject CreateMeshObject(string name, Mesh mesh, Transform parent, Vector3 localPosition, Vector3 localEuler, Vector3 localScale, Material material)
    {
        GameObject meshObject = new GameObject(name);
        meshObject.transform.SetParent(parent, false);
        meshObject.transform.localPosition = localPosition;
        meshObject.transform.localRotation = Quaternion.Euler(localEuler);
        meshObject.transform.localScale = localScale;
        meshObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = meshObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return meshObject;
    }

    private static void SetMaterialColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }
}
