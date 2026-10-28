using System;
using System.IO;
using System.Linq;
using ArcaneWorkshop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public static class KeliangFarmInstaller
{
    private const string MainScene = "Assets/Scenes/ArcaneWorkshop.unity";
    private const string FarmScene = "Assets/Scenes/Keliang/Farm.unity";
    private const string Materials = "Assets/Materials/Keliang/Integration";
    private static readonly Vector3 FarmPosition = new Vector3(-28f, 0f, -36f);

    [MenuItem("MP2B/Keliang/Install Farm and Bridge")]
    public static void Install()
    {
        var main = EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);
        if (main.GetRootGameObjects().Any(g => g.name == "keliangFarmIsland"))
            throw new InvalidOperationException("Farm is already installed; edit its saved scene objects instead of reinstalling.");
        ConfigureLayer();
        ConvertMaterials();
        CleanImportedPrefabs();
        var farm = EditorSceneManager.OpenScene(FarmScene, OpenSceneMode.Additive);
        var farmRoots = farm.GetRootGameObjects();
        GameObject rig = farmRoots.Single(g => g.GetComponent<XROrigin>() != null);
        if (PrefabUtility.IsPartOfPrefabInstance(rig))
            PrefabUtility.UnpackPrefabInstance(rig, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        var island = new GameObject("keliangFarmIsland");
        SceneManager.MoveGameObjectToScene(island, main);
        foreach (var item in farmRoots)
        {
            SceneManager.MoveGameObjectToScene(item, main);
            if (item != rig) item.transform.SetParent(island.transform, false);
        }
        EditorSceneManager.CloseScene(farm, true);
        SceneManager.SetActiveScene(main);
        island.transform.SetPositionAndRotation(FarmPosition, Quaternion.LookRotation(FarmPosition.normalized, Vector3.up));
        rig.name = "playerRig";
        rig.transform.SetPositionAndRotation(island.transform.TransformPoint(new Vector3(0f, 0f, -5.4f)), island.transform.rotation);

        var ground = Find(island, "ground");
        // The original 80 m background plane is empty beyond the forest. Keep all farm objects
        // at their original scale and trim only that plane into an island footprint.
        ground.localScale = new Vector3(32f, .5f, 32f);
        var earth = Material("islandEarth", new Color(.20f, .16f, .22f));
        Cube("islandBase", island.transform, new Vector3(0f, -1.25f, 0f), new Vector3(31.6f, 2f, 31.6f), earth, false);

        // Open a 3.2 m gap in the existing entrance boundary, retaining both wall sides.
        var south = Find(island, "southInvisibleWall");
        var wallParent = south.parent;
        Vector3 wallPosition = south.localPosition;
        south.name = "southBoundaryLeft";
        south.localScale = new Vector3(7.2f, 7f, 1f);
        south.localPosition = new Vector3(-5.2f, wallPosition.y, wallPosition.z);
        var eastHalf = UnityEngine.Object.Instantiate(south.gameObject, wallParent);
        eastHalf.name = "southBoundaryRight";
        eastHalf.transform.localPosition = new Vector3(5.2f, wallPosition.y, wallPosition.z);
        var forest = Find(island, "forest");
        // Keep every decorative object, shifting only foliage in the new entrance corridor.
        foreach (var t in forest.GetComponentsInChildren<Transform>(true))
        {
            if (t.GetComponent<MeshRenderer>() == null) continue;
            Vector3 p = island.transform.InverseTransformPoint(t.position);
            if (p.z < -7f && Mathf.Abs(p.x) < 2.1f)
            {
                p.x = p.x < 0f ? -3.3f : 3.3f;
                t.position = island.transform.TransformPoint(p);
            }
        }
        BuildBridge(island.transform);
        ConfigureMainIslandCollision();

        var origin = rig.GetComponent<XROrigin>();
        var camera = origin.Camera;
        foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c == camera) continue;
            c.gameObject.SetActive(false);
        }
        foreach (var root in main.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);

        var manager = UnityEngine.Object.FindAnyObjectByType<ArcaneGameManager>();
        var left = Find(rig, "leftController");
        var right = Find(rig, "rightController");
        var ray = right.GetComponentsInChildren<NearFarInteractor>(true).Single();
        var hud = rig.GetComponentInChildren<PlayerFarmHud>(true).gameObject;
        rig.AddComponent<FarmWorkshopRig>().Configure(manager, camera.transform, left, ray.transform, hud);
        camera.tag = "MainCamera";
        camera.transform.localPosition = Vector3.up * 1.65f;
        camera.clearFlags = CameraClearFlags.Skybox;

        // Lighting belongs to the shared scene. A local fill keeps the farm readable
        // without replacing the workshop's sky, directional lighting, or materials.
        var fill = new GameObject("farmFillLight");
        fill.transform.SetParent(island.transform, false);
        fill.transform.localPosition = new Vector3(0f, 10f, 0f);
        var light = fill.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(.9f, .95f, 1f);
        light.intensity = .25f;
        light.range = 28f;
        light.shadows = LightShadows.None;

        AdjustLighting();

        EditorSceneManager.MarkSceneDirty(main);
        EditorSceneManager.SaveScene(main);
        AssetDatabase.SaveAssets();
        Validate();
        CapturePreview();
        Debug.Log("KELIANG FARM INSTALL COMPLETE: farm, bridge and shared XRI rig saved in ArcaneWorkshop.unity.");
    }

    private static void ConfigureLayer()
    {
        var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layer = tags.FindProperty("layers").GetArrayElementAtIndex(8);
        if (!string.IsNullOrEmpty(layer.stringValue) && layer.stringValue != "farmSoil")
            throw new InvalidOperationException("Layer 8 is occupied; farm soil masks must be remapped first.");
        layer.stringValue = "farmSoil";
        tags.ApplyModifiedPropertiesWithoutUndo();
    }

    [MenuItem("MP2B/Keliang/Fix Bridge Landing")]
    public static void ConfigureMainIslandCollision()
    {
        var island = GameObject.Find("Floating Workshop Island");
        var capsule = island.GetComponent<CapsuleCollider>();
        if (capsule != null) UnityEngine.Object.DestroyImmediate(capsule);
        var collider = island.GetComponent<MeshCollider>();
        if (collider == null) collider = island.AddComponent<MeshCollider>();
        collider.sharedMesh = island.GetComponent<MeshFilter>().sharedMesh;
        collider.convex = false;
        // Preserve the old main-island edge restriction as physical geometry, leaving
        // a bridge opening instead of clamping the shared rig around world origin.
        if (GameObject.Find("workshopBoundary") == null)
        {
            var boundary = new GameObject("workshopBoundary");
            Vector3 bridgeDirection = FarmPosition.normalized;
            const int segments = 48;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector3 radial = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                if (Vector3.Angle(radial, bridgeDirection) < 13f) continue;
                var wall = new GameObject("edgeBarrier" + i.ToString("D2"));
                wall.transform.SetParent(boundary.transform, false);
                wall.transform.SetPositionAndRotation(radial * 10.65f + Vector3.up * 1.5f, Quaternion.LookRotation(radial));
                wall.AddComponent<BoxCollider>().size = new Vector3(1.48f, 3f, .18f);
            }
        }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }

    [MenuItem("MP2B/Keliang/Adjust Farm Lighting")]
    public static void AdjustLighting()
    {
        var island = GameObject.Find("keliangFarmIsland");
        var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layer = tags.FindProperty("layers").GetArrayElementAtIndex(9);
        if (!string.IsNullOrEmpty(layer.stringValue) && layer.stringValue != "farmVisual")
            throw new InvalidOperationException("Layer 9 is occupied.");
        layer.stringValue = "farmVisual";
        tags.ApplyModifiedPropertiesWithoutUndo();
        foreach (var t in island.GetComponentsInChildren<Transform>(true))
            if (t.gameObject.layer == 0) t.gameObject.layer = 9;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Keliang/Farm" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith("/playerRig.prefab")) continue;
            var prefab = PrefabUtility.LoadPrefabContents(path);
            foreach (var t in prefab.GetComponentsInChildren<Transform>(true))
                if (t.gameObject.layer == 0) t.gameObject.layer = 9;
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
            PrefabUtility.UnloadPrefabContents(prefab);
        }
        int farmMask = (1 << 8) | (1 << 9);
        foreach (var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            bool belongsToFarm = light.transform.IsChildOf(island.transform);
            light.cullingMask = belongsToFarm ? farmMask : light.cullingMask & ~farmMask;
            if (belongsToFarm && light.type == LightType.Directional)
            {
                light.intensity = .9f;
                light.shadows = LightShadows.Soft;
            }
            if (light.name == "farmFillLight") light.intensity = .25f;
        }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        CapturePreview();
        Debug.Log("KELIANG LIGHTING: farm-only sun 0.9, fill 0.25; original workshop lights isolated from farm.");
    }

    private static void ConvertMaterials()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Materials/Keliang", "Assets/ThirdParty/Keliang" }))
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            var serialized = new SerializedObject(mat);
            var colors = serialized.FindProperty("m_SavedProperties.m_Colors");
            Color color = Color.white;
            bool hasBaseColor = false;
            for (int i = 0; i < colors.arraySize; i++)
            {
                var pair = colors.GetArrayElementAtIndex(i);
                if (pair.FindPropertyRelative("first").stringValue != "_BaseColor") continue;
                color = pair.FindPropertyRelative("second").colorValue;
                hasBaseColor = true;
            }
            string shader = mat.shader == null ? "" : mat.shader.name;
            if (!hasBaseColor && !shader.Contains("Universal") && !shader.Contains("Shader Graph") && !shader.Contains("InternalError")) continue;
            Texture texture = null;
            Vector2 scale = Vector2.one, offset = Vector2.zero;
            var textures = serialized.FindProperty("m_SavedProperties.m_TexEnvs");
            for (int i = 0; i < textures.arraySize; i++)
            {
                var pair = textures.GetArrayElementAtIndex(i);
                if (pair.FindPropertyRelative("first").stringValue != "_BaseMap") continue;
                var value = pair.FindPropertyRelative("second");
                texture = value.FindPropertyRelative("m_Texture").objectReferenceValue as Texture;
                scale = value.FindPropertyRelative("m_Scale").vector2Value;
                offset = value.FindPropertyRelative("m_Offset").vector2Value;
            }
            mat.shader = Shader.Find(mat.name.IndexOf("water", StringComparison.OrdinalIgnoreCase) >= 0 ? "Unlit/Texture" : "Standard");
            mat.shaderKeywords = Array.Empty<string>();
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            mat.mainTexture = texture;
            mat.mainTextureScale = scale;
            mat.mainTextureOffset = offset;
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", .15f);
            mat.renderQueue = -1;
            EditorUtility.SetDirty(mat);
        }
    }

    private static void CleanImportedPrefabs()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Keliang", "Assets/ThirdParty/Keliang" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".prefab")) continue;
            var root = PrefabUtility.LoadPrefabContents(path);
            int removed = 0;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            if (removed > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Transform Find(GameObject root, string name) =>
        root.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);

    private static Material Material(string name, Color color)
    {
        Directory.CreateDirectory(Materials);
        string path = Materials + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        material = new Material(Shader.Find("Standard")) { color = color };
        material.SetFloat("_Glossiness", .12f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool collision = true)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = position;
        cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = material;
        if (!collision) UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
        return cube;
    }

    private static void BuildBridge(Transform farm)
    {
        Vector3 direction = FarmPosition.normalized;
        Vector3 start = direction * 9f;
        Vector3 end = farm.TransformPoint(new Vector3(0f, 0f, -7.3f));
        float length = Vector3.Distance(start, end);
        var bridge = new GameObject("mainToFarmBridge");
        bridge.transform.SetPositionAndRotation((start + end) * .5f, Quaternion.LookRotation(direction));
        var wood = Material("bridgeWood", new Color(.29f, .18f, .10f));
        var trim = Material("bridgeTrim", new Color(.12f, .09f, .075f));
        var deck = Cube("walkableDeck", bridge.transform, new Vector3(0f, -.12f, 0f), new Vector3(3f, .24f, length), wood);
        deck.GetComponent<Renderer>().enabled = false;
        int count = Mathf.CeilToInt(length / .5f);
        float spacing = length / count;
        for (int i = 0; i < count; i++)
            Cube("plank" + (i + 1).ToString("D2"), bridge.transform,
                new Vector3(0f, -.09f, -length * .5f + spacing * (i + .5f)), new Vector3(3f, .18f, spacing - .018f), wood, false);
        for (int side = -1; side <= 1; side += 2)
        {
            string label = side < 0 ? "left" : "right";
            var rail = Cube(label + "SafetyBarrier", bridge.transform, new Vector3(side * 1.5f, .75f, 0f), new Vector3(.15f, 1.5f, length), trim);
            rail.GetComponent<Renderer>().enabled = false;
            Cube(label + "Handrail", bridge.transform, new Vector3(side * 1.5f, 1.12f, 0f), new Vector3(.16f, .16f, length), trim, false);
            Cube(label + "LowerRail", bridge.transform, new Vector3(side * 1.5f, .45f, 0f), new Vector3(.12f, .12f, length), wood, false);
            int posts = Mathf.CeilToInt(length / 2.5f);
            for (int i = 0; i <= posts; i++)
                Cube(label + "Post" + i.ToString("D2"), bridge.transform,
                    new Vector3(side * 1.5f, .57f, -length * .5f + length * i / posts), new Vector3(.2f, 1.3f, .2f), trim, false);
        }
    }

    [MenuItem("MP2B/Keliang/Validate Farm Integration")]
    public static void Validate()
    {
        Require(UnityEngine.Object.FindObjectsByType<FarmCell>(FindObjectsSortMode.None).Length == 9, "Nine farm plots");
        Require(UnityEngine.Object.FindObjectsByType<FarmResources>(FindObjectsSortMode.None).Length == 1, "One farm inventory");
        Require(UnityEngine.Object.FindObjectsByType<XROrigin>(FindObjectsSortMode.None).Length == 1, "One XRI origin");
        Require(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c => c.enabled) == 1, "One active camera");
        Require(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(c => c.enabled) == 1, "One audio listener");
        Require(UnityEngine.Object.FindAnyObjectByType<FarmWorkshopRig>() != null, "Workshop pointer adapter");
        Require(GameObject.Find("mainToFarmBridge/walkableDeck").GetComponent<BoxCollider>().enabled, "Bridge deck collider");
        Require(LayerMask.NameToLayer("farmSoil") == 8, "Watering soil layer");
        // Michael Island keeps one station (the Convergence Altar); Justin's machines live on the Astral Foundry island.
        Require(UnityEngine.Object.FindObjectsByType<TeamExpansionStation>(FindObjectsSortMode.None).Length == 1, "Original teammate stations retained");
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0, "No missing scripts: " + t.name);
        Debug.Log("KELIANG STATIC VALIDATION PASSED");
    }

    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    public static void CapturePreview()
    {
        var cameraObject = new GameObject("integrationPreviewCamera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.transform.position = new Vector3(-63f, 57f, -81f);
        camera.transform.LookAt(new Vector3(-14f, 0f, -18f));
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.18f, .23f, .3f);
        var rt = new RenderTexture(1600, 1000, 24);
        camera.targetTexture = rt;
        camera.Render();
        RenderTexture.active = rt;
        var image = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        image.Apply();
        string path = Path.GetFullPath("../tmp/keliang-farm-overview.png");
        File.WriteAllBytes(path, image.EncodeToPNG());
        RenderTexture.active = null;
        UnityEngine.Object.DestroyImmediate(image);
        UnityEngine.Object.DestroyImmediate(cameraObject);
        UnityEngine.Object.DestroyImmediate(rt);
        Debug.Log("Farm preview: " + path);
    }
}
