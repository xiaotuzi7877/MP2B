using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ArcaneWorkshop;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[InitializeOnLoad]
public static class KeliangMagicCropChecks
{
    static KeliangMagicCropChecks() => EditorApplication.playModeStateChanged += OnPlay;
    [MenuItem("MP2B/Keliang/Check Magic Crop Harvest")]
    public static void Run()
    {
        // The integrated farm now needs the sequential lesson flow before planting Bloom.
        if (UnityEngine.Object.FindAnyObjectByType<FarmProgression>() != null)
        {
            KeliangFarmLessonChecks.Run();
            return;
        }
        SessionState.SetBool("Keliang.MagicCropChecks", true);
        EditorApplication.isPlaying = true;
    }
    private static void OnPlay(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("Keliang.MagicCropChecks", false)) return;
        SessionState.SetBool("Keliang.MagicCropChecks", false);
        new GameObject("MagicCropChecks").AddComponent<KeliangMagicCropProbe>();
    }
}

public sealed class KeliangMagicCropProbe : MonoBehaviour
{
    private readonly List<string> results = new List<string>();
    private bool failed;
    private IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(.5f);
        float speed = Time.timeScale;
        var run = Checks();
        while (true)
        {
            bool more;
            try { more = run.MoveNext(); }
            catch (Exception e) { failed = true; results.Add("FAIL: " + e); break; }
            if (!more) break;
            yield return run.Current;
        }
        Time.timeScale = speed;
        File.WriteAllText(Path.GetFullPath("../tmp/keliang-magic-crop-checks.txt"), (failed ? "FAIL\n" : "PASS\n") + string.Join("\n", results));
        Debug.Log("KELIANG MAGIC CROP CHECKS " + (failed ? "FAILED" : "PASSED"));
        EditorApplication.isPlaying = false;
    }

    private IEnumerator Checks()
    {
        var manager = FindAnyObjectByType<ArcaneGameManager>();
        var farm = FindAnyObjectByType<FarmResources>();
        var cells = FindObjectsByType<FarmCell>(FindObjectsSortMode.None).OrderBy(c => c.name).ToArray();
        var bags = FindObjectsByType<SeedBagTool>(FindObjectsSortMode.None);
        Check(bags.Length == 2 && bags.Any(b => new SerializedObject(b).FindProperty("cropKind").enumValueIndex == 1), "Two grabbable seed bags with distinct crop choices");
        int herbs = manager.GetResource(WorkshopResourceKind.MoonHerb), blooms = manager.GetResource(WorkshopResourceKind.ConservatoryBloom);
        int seeds = farm.GetAmount(FarmResource.Seeds);
        Check(cells[0].Till() && cells[0].Plant(), "Original till/plant operation creates Moon Herb");
        Check(cells[1].Till() && cells[1].Plant(FarmCropKind.ConservatoryBloom), "Bloom uses the same tilled plot and planting operation");
        Check(farm.GetAmount(FarmResource.Seeds) == seeds - 2, "One seed spent per crop");
        cells[2].Till();
        Check(!cells[2].Plant(FarmCropKind.ConservatoryBloom) && farm.GetAmount(FarmResource.Seeds) == seeds - 2, "No duplicate quest crop or extra seed charge");
        Check(!cells[1].Harvest(cells[1].Crop) && manager.GetResource(WorkshopResourceKind.ConservatoryBloom) == blooms, "Immature Bloom gives no resources");
        cells[0].Water(100); cells[1].Water(100);
        farm.Add(FarmResource.Fertilizer, 1);
        Check(cells[1].Fertilize() && cells[1].Crop.IsFertilized, "Original fertilizer works on Bloom");
        Time.timeScale = 30;
        yield return new WaitForSeconds(32);
        Time.timeScale = 1;
        Check(cells[0].Crop.IsMature && cells[1].Crop.IsMature, "Both crops mature through existing watered growth");
        Check(cells[0].Harvest(cells[0].Crop), "Original harvest succeeds");
        Check(manager.GetResource(WorkshopResourceKind.MoonHerb) == herbs + 1 && farm.Crops == herbs + 1, "Moon Herb appears in one shared inventory, including the old farm shop");
        var crop = cells[1].Crop;
        var target = crop.GetComponentInChildren<XRSimpleInteractable>();
        var handObject = new GameObject("cropTestHand"); handObject.AddComponent<SphereCollider>().isTrigger = true;
        var hand = handObject.AddComponent<XRDirectInteractor>();
        var xr = FindAnyObjectByType<XRInteractionManager>();
        xr.SelectEnter((IXRSelectInteractor)hand, (IXRSelectInteractable)target);
        Check(manager.GetResource(WorkshopResourceKind.ConservatoryBloom) == blooms + 2 && !cells[1].IsPlanted, "XRI grip harvest credits exactly 2 Blooms");
        Check(!cells[1].Harvest(crop) && !cells[2].Plant(FarmCropKind.ConservatoryBloom), "Repeated harvest/plant cannot duplicate the first Bloom reward");
        Destroy(handObject);
        yield return null;
        var book = FindAnyObjectByType<SpellbookPanel>(FindObjectsInactive.Include);
        var type = typeof(SpellbookPanel);
        type.GetField("refreshTimer", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(book, -1f);
        type.GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(book, null);
        var text = (TextMesh)type.GetField("resourceText", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(book);
        Check(text.text == manager.ResourceSummary() && text.text.Contains("C. Blooms   " + (blooms + 2)), "Existing spellbook displays updated harvest balances");
        Check(farm.TrySpend(FarmResource.Crops, 1) && manager.GetResource(WorkshopResourceKind.MoonHerb) == herbs, "Original crop spending deducts the same shared Moon Herb balance");
        int changes = 0;
        farm.Changed += () => changes++;
        manager.AddResource(WorkshopResourceKind.MoonHerb, 1);
        Check(changes == 1 && farm.Crops == herbs + 1, "Main-island resource changes also refresh the farm display");
    }
    private void Check(bool condition, string label)
    {
        results.Add((condition ? "PASS: " : "FAIL: ") + label);
        if (!condition) { failed = true; throw new InvalidOperationException(label); }
    }
}
