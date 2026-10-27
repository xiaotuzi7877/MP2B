using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArcaneWorkshop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Object = UnityEngine.Object;

public static class KeliangMagicCropSetup
{
    public const string HerbPath = "Assets/Prefabs/Keliang/Farm/Plants/seedling.prefab";
    public const string BloomPath = "Assets/Prefabs/Keliang/Farm/Plants/conservatoryBloom.prefab";
    private const string BagPath = "Assets/Prefabs/Keliang/Farm/Tools/seedBag.prefab";
    private const string BloomBagPath = "Assets/Prefabs/Keliang/Farm/Tools/bloomSeedBag.prefab";
    private const string ArtPath = "Assets/Materials/Keliang/MagicCrops";
    private static Mesh leaf;
    private static Material stem, teal, silver, violet, gold;

    [MenuItem("MP2B/Keliang/Set Up Magic Crops")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        var island = GameObject.Find("keliangFarmIsland");
        if (island == null) throw new InvalidOperationException("Open ArcaneWorkshop with the integrated farm first.");
        Directory.CreateDirectory(ArtPath);
        AssetDatabase.Refresh();
        stem = Material("TwilightStem", new Color(.12f, .22f, .28f), .025f);
        teal = Material("MoonJade", new Color(.13f, .53f, .48f), .05f);
        silver = Material("MoonSilver", new Color(.60f, .82f, .83f), .08f);
        violet = Material("BloomAmethyst", new Color(.52f, .23f, .73f), .06f);
        gold = Material("BloomAmber", new Color(.95f, .61f, .18f), .1f);
        leaf = LeafMesh();
        BuildPlant(HerbPath, false);
        if (!AssetDatabase.LoadAssetAtPath<GameObject>(BloomPath)) AssetDatabase.CopyAsset(HerbPath, BloomPath);
        BuildPlant(BloomPath, true);
        BuildBag(BagPath, false);
        if (!AssetDatabase.LoadAssetAtPath<GameObject>(BloomBagPath)) AssetDatabase.CopyAsset(BagPath, BloomBagPath);
        BuildBag(BloomBagPath, true);

        var inventory = island.GetComponentInChildren<FarmResources>(true);
        Set(inventory, "workshop", Object.FindAnyObjectByType<ArcaneGameManager>());
        var bloom = AssetDatabase.LoadAssetAtPath<GameObject>(BloomPath);
        foreach (var cell in island.GetComponentsInChildren<FarmCell>(true)) Set(cell, "bloomPrefab", bloom);
        var bags = island.GetComponentsInChildren<SeedBagTool>(true);
        var originalBag = bags.First(b => new SerializedObject(b).FindProperty("cropKind").enumValueIndex == 0)
            .GetComponentInParent<XRGrabInteractable>(true).transform;
        var bloomBags = bags.Where(b => new SerializedObject(b).FindProperty("cropKind").enumValueIndex == 1)
            .Select(b => b.GetComponentInParent<XRGrabInteractable>(true).gameObject).Distinct().ToArray();
        GameObject bloomBag;
        if (bloomBags.Length == 0)
        {
            bloomBag = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BloomBagPath), originalBag.parent);
        }
        else
        {
            bloomBag = bloomBags[0];
            // Recover only duplicate instances of this generated tool from a repeated setup.
            foreach (var duplicate in bloomBags.Skip(1)) Object.DestroyImmediate(duplicate);
            bloomBag.transform.SetParent(originalBag.parent, true);
        }
        bloomBag.name = "bloomSeedBag";
        bloomBag.transform.SetPositionAndRotation(originalBag.position + island.transform.right * .48f, originalBag.rotation);
        bloomBag.transform.localScale = originalBag.localScale;
        // Existing farm trading remains available; name its shared crop currency accurately.
        foreach (var label in island.GetComponentsInChildren<TextMesh>(true))
            if (label.text.Contains("Crops")) { label.text = label.text.Replace("Crops", "Moon Herbs"); EditorUtility.SetDirty(label); }
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(island.scene);
        EditorSceneManager.SaveScene(island.scene);
        CapturePreview();
        Debug.Log("KELIANG MAGIC CROPS INSTALLED: Moon Herb, Bloom, second seed bag, shared inventory.");
    }

    private static Material Material(string name, Color color, float emission)
    {
        string path = ArtPath + "/" + name + ".mat";
        var result = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!result) { result = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(result, path); }
        result.color = color;
        result.SetFloat("_Metallic", .12f);
        result.SetFloat("_Glossiness", .32f);
        result.EnableKeyword("_EMISSION");
        result.SetColor("_EmissionColor", color * emission);
        EditorUtility.SetDirty(result);
        return result;
    }

    private static Mesh LeafMesh()
    {
        string path = ArtPath + "/ArcaneLeaf.asset";
        var result = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        var v = new List<Vector3>(); var triangles = new List<int>();
        for (int i = 0; i <= 8; i++)
        {
            float t = i / 8f, width = Mathf.Sin(t * Mathf.PI) * .5f;
            float curve = .18f * Mathf.Sin(t * Mathf.PI) + .2f * t * t;
            v.Add(new Vector3(-width, curve, t));
            v.Add(new Vector3(0, curve + width * .18f, t));
            v.Add(new Vector3(width, curve, t));
        }
        for (int i = 0; i < 8; i++) for (int j = 0; j < 2; j++)
        {
            int a = i * 3 + j, b = a + 3;
            triangles.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
        }
        // Back faces need separate vertices so opposing normals do not cancel out.
        int frontVertices = v.Count, frontIndices = triangles.Count;
        v.AddRange(v.ToArray());
        for (int i = 0; i < frontIndices; i += 3)
            triangles.AddRange(new[] { triangles[i + 2] + frontVertices, triangles[i + 1] + frontVertices, triangles[i] + frontVertices });
        bool existing = result != null;
        if (!existing) result = new Mesh { name = "Curved pointed leaf" };
        result.Clear(); result.vertices = v.ToArray(); result.triangles = triangles.ToArray();
        result.RecalculateNormals(); result.RecalculateBounds();
        if (!existing) AssetDatabase.CreateAsset(result, path); else EditorUtility.SetDirty(result);
        return result;
    }

    private static void BuildPlant(string path, bool bloom)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            root.name = bloom ? "ConservatoryBloom" : "MoonHerb";
            var growth = new SerializedObject(root.GetComponent<CropGrowth>());
            var renderers = new List<Renderer>();
            string[] names = { "smallStage", "mediumStage", "matureStage" };
            for (int stage = 0; stage < 3; stage++)
            {
                var owner = (GameObject)growth.FindProperty(names[stage]).objectReferenceValue;
                Object.DestroyImmediate(owner.GetComponent<MeshRenderer>());
                Object.DestroyImmediate(owner.GetComponent<MeshFilter>());
                if (owner.transform.Find("magicVisual")) Object.DestroyImmediate(owner.transform.Find("magicVisual").gameObject);
                owner.transform.localPosition = Vector3.zero;
                owner.transform.localRotation = Quaternion.identity;
                owner.transform.localScale = Vector3.one;
                var visual = new GameObject("magicVisual").transform;
                visual.SetParent(owner.transform, false);
                float height = new[] { .18f, .40f, .65f }[stage];
                Shape("Stem", PrimitiveType.Cylinder, visual, new Vector3(0, height / 2, 0), new Vector3(.035f, height / 2, .035f), stem);
                int pairs = stage + 2;
                for (int n = 0; n < pairs; n++) for (int side = 0; side < 2; side++)
                {
                    var pivot = new GameObject("LeafWhorl").transform; pivot.SetParent(visual, false);
                    pivot.localPosition = new Vector3(0, height * (.18f + .22f * n), 0);
                    pivot.localRotation = Quaternion.Euler(-14f, n * 63f + side * 180f, 0);
                    float length = (.17f + stage * .08f) * (1f - n * .12f);
                    Leaf("JadeLeaf", pivot, new Vector3(length * .48f, length, length), teal);
                    Leaf("SilverVein", pivot, new Vector3(length * .035f, length * 1.06f, length), silver);
                }
                if (bloom)
                {
                    var crown = new GameObject("BloomCrown").transform; crown.SetParent(visual, false);
                    crown.localPosition = new Vector3(0, height, 0);
                    int petals = stage == 2 ? 9 : 5;
                    float length = new[] { .10f, .20f, .40f }[stage];
                    for (int ring = 0; ring < (stage == 2 ? 2 : 1); ring++)
                    for (int n = 0; n < petals; n++)
                    {
                        var pivot = new GameObject("Petal").transform; pivot.SetParent(crown, false);
                        pivot.localRotation = Quaternion.Euler(stage == 2 ? -8f - ring * 25f : -65f, n * 360f / petals + ring * 20f, 0);
                        float size = length * (ring == 0 ? 1 : .68f);
                        Leaf("AmethystPetal", pivot, new Vector3(size * .65f, size, size), violet);
                        Leaf("LuminousVein", pivot, new Vector3(size * .035f, size * 1.06f, size), silver);
                    }
                    Shape("AmberHeart", PrimitiveType.Sphere, crown, new Vector3(0, stage == 2 ? .15f : .045f, 0), Vector3.one * (.07f + stage * .035f), gold);
                    if (stage == 2) for (int n = 0; n < 7; n++)
                    {
                        float a = n * Mathf.PI * 2 / 7;
                        Shape("Pollen", PrimitiveType.Sphere, crown, new Vector3(Mathf.Cos(a) * .075f, .21f, Mathf.Sin(a) * .075f), Vector3.one * .025f, gold);
                    }
                }
                else
                {
                    for (int n = 0; n < stage + 1; n++)
                    {
                        var bud = Shape("MoonBud", PrimitiveType.Sphere, visual, new Vector3((n - stage * .5f) * .075f, height + .025f, 0), new Vector3(.055f, .10f, .055f), silver);
                        bud.transform.localRotation = Quaternion.Euler(0, 45f, (n - stage * .5f) * -18f);
                    }
                }
                CombineVisuals(visual, (bloom ? "Bloom" : "Herb") + stage);
                foreach (Transform t in visual.GetComponentsInChildren<Transform>()) t.gameObject.layer = 9;
                renderers.AddRange(visual.GetComponentsInChildren<Renderer>());
                owner.SetActive(stage == 0);
            }
            var health = new SerializedObject(root.GetComponent<CropHealth>());
            var array = health.FindProperty("plantRenderers"); array.arraySize = renderers.Count;
            for (int i = 0; i < renderers.Count; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
            health.ApplyModifiedPropertiesWithoutUndo();
            var target = root.GetComponentInChildren<CropHarvest>(true);
            target.transform.localPosition = new Vector3(0, .60f, 0);
            target.transform.localScale = Vector3.one;
            var box = target.GetComponent<BoxCollider>(); box.center = Vector3.zero; box.size = new Vector3(.75f, .55f, .75f);
            var prompt = target.GetComponent<InteractionPrompt>();
            string text = bloom ? "CONSERVATORY BLOOM\nGRIP to harvest 2 Blooms" : "MOON HERB\nGRIP to harvest 1 herb";
            SetText(prompt, "hoverMessage", text); SetText(prompt, "heldMessage", text);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void BuildBag(string path, bool bloom)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var tool = new SerializedObject(root.GetComponentInChildren<SeedBagTool>(true));
            tool.FindProperty("cropKind").enumValueIndex = bloom ? 1 : 0; tool.ApplyModifiedPropertiesWithoutUndo();
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r.name == "clothBody") r.sharedMaterial = bloom ? violet : teal;
                if (r.name.StartsWith("sprout") || r.name.StartsWith("rope")) r.sharedMaterial = bloom ? gold : silver;
            }
            var label = root.GetComponentsInChildren<TextMesh>(true).First(t => t.name == "seedLabelText");
            label.text = bloom ? "BLOOM" : "MOON";
            var prompt = root.GetComponentInChildren<InteractionPrompt>(true);
            SetText(prompt, "hoverMessage", bloom ? "BLOOM SEEDS\nGRIP to grab - first harvest: 2 Blooms" : "MOON HERB SEEDS\nGRIP to grab");
            SetText(prompt, "heldMessage", "Touch an empty tilled plot to plant\nUses 1 seed - release GRIP to drop");
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void CombineVisuals(Transform visual, string name)
    {
        var groups = visual.GetComponentsInChildren<MeshRenderer>().GroupBy(r => r.sharedMaterial).ToArray();
        var children = visual.Cast<Transform>().ToArray();
        foreach (var group in groups)
        {
            string path = ArtPath + "/" + name + "_" + group.Key.name + ".asset";
            var combined = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool exists = combined != null;
            if (!exists) combined = new Mesh { name = name + " " + group.Key.name };
            combined.Clear();
            combined.CombineMeshes(group.Select(r => new CombineInstance {
                mesh = r.GetComponent<MeshFilter>().sharedMesh,
                transform = visual.worldToLocalMatrix * r.transform.localToWorldMatrix
            }).ToArray());
            if (!exists) AssetDatabase.CreateAsset(combined, path); else EditorUtility.SetDirty(combined);
            var go = new GameObject(group.Key.name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(visual, false);
            go.GetComponent<MeshFilter>().sharedMesh = combined;
            go.GetComponent<MeshRenderer>().sharedMaterial = group.Key;
        }
        foreach (var child in children) Object.DestroyImmediate(child.gameObject);
    }

    private static void Leaf(string name, Transform parent, Vector3 scale, Material material)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false); go.transform.localScale = scale;
        go.GetComponent<MeshFilter>().sharedMesh = leaf; go.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static GameObject Shape(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        var go = GameObject.CreatePrimitive(type); go.name = name; Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material; return go;
    }

    private static void Set(Object target, string property, Object value)
    {
        var serialized = new SerializedObject(target); serialized.FindProperty(property).objectReferenceValue = value;
        serialized.ApplyModifiedProperties(); EditorUtility.SetDirty(target);
        if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }

    private static void SetText(Object target, string property, string value)
    {
        var serialized = new SerializedObject(target); serialized.FindProperty(property).stringValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void CapturePreview()
    {
        var preview = new PreviewRenderUtility();
        var instances = new List<GameObject>();
        try
        {
            for (int kind = 0; kind < 2; kind++) for (int stage = 0; stage < 3; stage++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(kind == 0 ? HerbPath : BloomPath), preview.camera.scene);
                instances.Add(go); go.transform.position = new Vector3(stage * 1.05f - 1.05f, 0, kind == 0 ? .75f : -.65f);
                var so = new SerializedObject(go.GetComponent<CropGrowth>());
                string[] names = { "smallStage", "mediumStage", "matureStage" };
                for (int s = 0; s < 3; s++) ((GameObject)so.FindProperty(names[s]).objectReferenceValue).SetActive(s == stage);
                foreach (var canvas in go.GetComponentsInChildren<Canvas>(true)) canvas.gameObject.SetActive(false);
            }
            preview.camera.transform.position = new Vector3(2.6f, 2.7f, -4.7f);
            preview.camera.transform.LookAt(new Vector3(0, .3f, 0));
            preview.camera.fieldOfView = 30f;
            preview.camera.clearFlags = CameraClearFlags.SolidColor; preview.camera.backgroundColor = new Color(.055f, .075f, .12f);
            preview.lights[0].intensity = 1.2f; preview.lights[0].transform.rotation = Quaternion.Euler(40, 35, 0);
            preview.lights[1].intensity = .5f;
            preview.BeginStaticPreview(new Rect(0, 0, 1400, 1000));
            preview.camera.Render();
            var texture = preview.EndStaticPreview();
            File.WriteAllBytes(Path.GetFullPath("../tmp/keliang-magic-crops.png"), texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }
        finally { foreach (var go in instances) Object.DestroyImmediate(go); preview.Cleanup(); }
    }
}
