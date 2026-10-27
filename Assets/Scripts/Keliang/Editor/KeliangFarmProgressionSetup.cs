using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Object = UnityEngine.Object;

public static class KeliangFarmProgressionSetup
{
    [MenuItem("MP2B/Keliang/Set Up Farm Lessons")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        var island = GameObject.Find("keliangFarmIsland");
        if (island == null) throw new InvalidOperationException("Open the integrated ArcaneWorkshop scene.");
        var resources = island.GetComponentInChildren<FarmResources>(true);
        var progression = island.GetComponent<FarmProgression>();
        if (!progression) progression = island.AddComponent<FarmProgression>();
        var bags = island.GetComponentsInChildren<SeedBagTool>(true);
        var moon = bags.Single(b => new SerializedObject(b).FindProperty("cropKind").enumValueIndex == 0).GetComponentInParent<XRGrabInteractable>(true).gameObject;
        var bloom = bags.Single(b => new SerializedObject(b).FindProperty("cropKind").enumValueIndex == 1).GetComponentInParent<XRGrabInteractable>(true).gameObject;
        var carrier = island.GetComponentsInChildren<UpgradeKitTool>(true)
            .Single(b => new SerializedObject(b).FindProperty("kit").enumValueIndex == (int)FarmResource.SprinklerKit)
            .GetComponentInParent<XRGrabInteractable>(true).gameObject;
        var buy = island.GetComponentsInChildren<KitPurchase>(true)
            .Single(b => new SerializedObject(b).FindProperty("kit").enumValueIndex == (int)FarmResource.SprinklerKit);
        var label = buy.GetComponentInChildren<TextMesh>(true);
        var pickaxe = island.GetComponentInChildren<PickaxeTool>(true).GetComponentInParent<XRGrabInteractable>(true).gameObject;
        var field = island.GetComponentInChildren<RockField>(true).gameObject;
        GameObject Carrier(FarmResource kit) => island.GetComponentsInChildren<UpgradeKitTool>(true)
            .Single(t => new SerializedObject(t).FindProperty("kit").enumValueIndex == (int)kit)
            .GetComponentInParent<XRGrabInteractable>(true).gameObject;
        KitPurchase Purchase(FarmResource kit) => island.GetComponentsInChildren<KitPurchase>(true)
            .Single(t => new SerializedObject(t).FindProperty("kit").enumValueIndex == (int)kit);
        var pickaxeCarrier = Carrier(FarmResource.PickaxeKit);
        var tankCarrier = Carrier(FarmResource.TankKit);
        var pickaxeBuy = Purchase(FarmResource.PickaxeKit);
        var tankBuy = Purchase(FarmResource.TankKit);
        var board = island.transform.Find("farmLessonBoard");
        if (!board)
        {
            board = new GameObject("farmLessonBoard").transform;
            board.SetParent(island.transform, false);
            board.localPosition = new Vector3(3.5f, 1.65f, 3.7f);
            var material = new Material(Shader.Find("Standard")) { color = new Color(.055f, .085f, .12f) };
            string path = "Assets/Materials/Keliang/FarmLessonBoard.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing) { Object.DestroyImmediate(material); material = existing; }
            else AssetDatabase.CreateAsset(material, path);
            var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "Panel"; panel.transform.SetParent(board, false);
            panel.transform.localScale = new Vector3(2.3f, 1.65f, .05f);
            panel.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(panel.GetComponent<Collider>());
            Text(board, "Title", "VERDANT CONSERVATORY", new Vector3(-1.03f, .70f, -.04f), .075f, new Color(.55f, .95f, .82f));
            Text(board, "Goal", "NEXT STEP\nGrab the harrow and prepare a plot.", new Vector3(-1.03f, .43f, -.04f), .065f, Color.white);
            Text(board, "Lesson", "GRIP: grab / harvest\nTRIGGER with can: water\nY: spellbook", new Vector3(-1.03f, -.36f, -.04f), .05f, new Color(.65f, .82f, 1f));
            foreach (Transform t in board.GetComponentsInChildren<Transform>()) t.gameObject.layer = 9;
        }
        board.Find("Title").GetComponent<TextMesh>().characterSize = .018f;
        board.Find("Goal").GetComponent<TextMesh>().characterSize = .013f;
        board.Find("Lesson").GetComponent<TextMesh>().characterSize = .011f;
        Set(resources, "progression", progression);
        Set(progression, "resources", resources);
        Set(progression, "moonSeeds", moon);
        Set(progression, "bloomSeeds", bloom);
        Set(progression, "sprinklerCarrier", carrier);
        Set(progression, "miningTool", pickaxe);
        Set(progression, "miningField", field);
        Set(progression, "pickaxeCarrier", pickaxeCarrier);
        Set(progression, "tankCarrier", tankCarrier);
        Set(progression, "sprinklerPurchase", buy);
        Set(progression, "pickaxePurchase", pickaxeBuy);
        Set(progression, "tankPurchase", tankBuy);
        Set(progression, "goalText", board.Find("Goal").GetComponent<TextMesh>());
        Set(progression, "lessonText", board.Find("Lesson").GetComponent<TextMesh>());
        Set(progression, "sprinklerLabel", label);
        var so = new SerializedObject(progression);
        so.FindProperty("sprinklerPrice").stringValue = "SPRINKLER KIT\n5 stone / 3 coins";
        so.ApplyModifiedPropertiesWithoutUndo();
        foreach (var tool in new[] { moon, bloom, carrier, pickaxe, field, pickaxeCarrier, tankCarrier })
        {
            tool.SetActive(false);
            EditorUtility.SetDirty(tool);
            if (PrefabUtility.IsPartOfPrefabInstance(tool)) PrefabUtility.RecordPrefabInstancePropertyModifications(tool);
        }
        pickaxeBuy.RefreshAvailabilityLabel();
        tankBuy.RefreshAvailabilityLabel();
        EditorUtility.SetDirty(pickaxeBuy.GetComponentInChildren<TextMesh>(true));
        EditorUtility.SetDirty(tankBuy.GetComponentInChildren<TextMesh>(true));
        label.text = "LOCKED\nHarvest Bloom\nGRIP for help";
        EditorUtility.SetDirty(label);
        EnableGrowingCropInteraction(KeliangMagicCropSetup.HerbPath);
        EnableGrowingCropInteraction(KeliangMagicCropSetup.BloomPath);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(island.scene);
        EditorSceneManager.SaveScene(island.scene);
        Debug.Log("KELIANG FARM LESSONS INSTALLED");
    }

    public static void PreviewBoard()
    {
        var source = GameObject.Find("farmLessonBoard");
        var preview = new PreviewRenderUtility();
        var clone = Object.Instantiate(source);
        try
        {
            preview.AddSingleGO(clone);
            clone.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var goal = clone.transform.Find("Goal").GetComponent<TextMesh>();
            goal.text = "NEXT STEP\n" + FarmProgression.Wrap("Need 5 stones: grab the pickaxe, break rocks and collect the drops. Need 3 coins: trade Moon Herbs at the farm shop.", 43);
            preview.camera.transform.SetPositionAndRotation(new Vector3(0, 0, -3f), Quaternion.identity);
            preview.camera.fieldOfView = 36;
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.camera.backgroundColor = new Color(.045f, .06f, .09f);
            preview.BeginStaticPreview(new Rect(0, 0, 1200, 900));
            preview.camera.Render();
            var texture = preview.EndStaticPreview();
            File.WriteAllBytes(Path.GetFullPath("../tmp/farm-lesson-board-final.png"), texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }
        finally { Object.DestroyImmediate(clone); preview.Cleanup(); }
    }

    private static void EnableGrowingCropInteraction(string path)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var harvest = root.GetComponentInChildren<CropHarvest>(true);
            harvest.transform.SetParent(root.transform, false);
            harvest.transform.localPosition = new Vector3(0, .4f, 0);
            harvest.transform.localRotation = Quaternion.identity;
            harvest.transform.localScale = Vector3.one;
            var box = harvest.GetComponent<BoxCollider>(); box.center = Vector3.zero; box.size = new Vector3(.65f, .85f, .65f);
            harvest.gameObject.SetActive(true);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void Text(Transform parent, string name, string text, Vector3 position, float size, Color color)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = position;
        var mesh = go.AddComponent<TextMesh>(); mesh.text = text; mesh.fontSize = 64; mesh.characterSize = size;
        mesh.anchor = TextAnchor.UpperLeft; mesh.color = color;
        mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        go.GetComponent<Renderer>().sharedMaterial = mesh.font.material;
        var worldText = go.AddComponent<WorldText>();
        var so = new SerializedObject(worldText); so.FindProperty("textShader").objectReferenceValue = Shader.Find("GUI/Text Shader"); so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Set(Object target, string field, Object value)
    {
        var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
        if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }
}
