using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[InitializeOnLoad]
public static class KeliangFarmLessonChecks
{
    static KeliangFarmLessonChecks() => EditorApplication.playModeStateChanged += OnPlay;
    [MenuItem("MP2B/Keliang/Check Farm Lessons and Spending")]
    public static void Run()
    {
        SessionState.SetBool("Keliang.LessonChecks", true);
        EditorApplication.isPlaying = true;
    }
    private static void OnPlay(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("Keliang.LessonChecks", false)) return;
        SessionState.SetBool("Keliang.LessonChecks", false);
        new GameObject("FarmLessonChecks").AddComponent<KeliangFarmLessonProbe>();
    }
}

public class KeliangFarmLessonProbe : MonoBehaviour
{
    private readonly List<string> lines = new List<string>();
    private readonly List<string> errors = new List<string>();
    private FarmResources resources;
    private FarmProgression progress;
    private XRInteractionManager xr;
    private XRDirectInteractor hand;
    private void OnEnable() => Application.logMessageReceived += Log;
    private void OnDisable() => Application.logMessageReceived -= Log;
    private void Log(string message, string stack, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Assert || (type == LogType.Error && !message.Contains("OpenXR") && !message.Contains("XR_ERROR"))) errors.Add(message);
    }
    private IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(.5f);
        var run = Checks(); bool failed = false; float speed = Time.timeScale;
        while (true)
        {
            bool next;
            try { next = run.MoveNext(); }
            catch (Exception e) { lines.Add("FAIL: " + e); failed = true; break; }
            if (!next) break;
            yield return run.Current;
        }
        Time.timeScale = speed;
        if (hand) Destroy(hand.gameObject);
        lines.AddRange(errors.Select(e => "RUNTIME ERROR: " + e));
        failed |= errors.Count > 0;
        File.WriteAllText(Path.GetFullPath("../tmp/keliang-farm-lesson-checks.txt"), (failed ? "FAIL\n" : "PASS\n") + string.Join("\n", lines));
        Debug.Log("KELIANG FARM LESSON CHECKS " + (failed ? "FAILED" : "PASSED"));
        EditorApplication.isPlaying = false;
    }

    private IEnumerator Checks()
    {
        resources = FindAnyObjectByType<FarmResources>(); progress = resources.Progression;
        var cells = FindObjectsByType<FarmCell>(FindObjectsSortMode.None).OrderBy(c => c.name).ToArray();
        var buy = FindObjectsByType<KitPurchase>(FindObjectsSortMode.None).Single(b => new SerializedObject(b).FindProperty("kit").enumValueIndex == (int)FarmResource.SprinklerKit);
        var so = new SerializedObject(buy);
        int stoneCost = so.FindProperty("stoneCost").intValue, coinCost = so.FindProperty("coinCost").intValue;
        var feedback = (ActionFeedback)so.FindProperty("feedback").objectReferenceValue;
        var message = (TextMesh)new SerializedObject(feedback).FindProperty("message").objectReferenceValue;
        var manager = FindAnyObjectByType<ArcaneWorkshop.ArcaneGameManager>();
        var tankBuy = FindObjectsByType<KitPurchase>(FindObjectsSortMode.None).Single(b => new SerializedObject(b).FindProperty("kit").enumValueIndex == (int)FarmResource.TankKit);
        var pickaxeBuy = FindObjectsByType<KitPurchase>(FindObjectsSortMode.None).Single(b => new SerializedObject(b).FindProperty("kit").enumValueIndex == (int)FarmResource.PickaxeKit);
        var pickaxe = FindAnyObjectByType<PickaxeTool>(FindObjectsInactive.Include);
        xr = FindAnyObjectByType<XRInteractionManager>();
        var handObject = new GameObject("lessonTestHand"); handObject.AddComponent<SphereCollider>().isTrigger = true; hand = handObject.AddComponent<XRDirectInteractor>();
        Check(progress.Stage == 0 && !Visible("moonSeeds") && !Visible("bloomSeeds") && !Visible("sprinklerCarrier"), "Initial seed bags and sprinkler carrier hidden; stock does not unlock anything");
        Check(!Visible("miningTool") && !Visible("miningField") && !Visible("pickaxeCarrier") && !Visible("tankCarrier")
            && FindObjectsByType<Boulder>(FindObjectsSortMode.None).Length == 0, "Mining tools, rocks and tank carrier start hidden");
        Check(buy.gameObject.activeInHierarchy && buy.GetComponent<XRSimpleInteractable>().enabled, "Locked purchase button remains clickable");
        resources.Add(FarmResource.Stones, 20); resources.Add(FarmResource.Coins, 20);
        var before = Snapshot();
        Select(buy.GetComponent<XRSimpleInteractable>());
        Check(Same(before) && message.text.Contains("Invalid purchase") && progress.CurrentGoal.Contains("harrow"), "Actual locked purchase interaction preserves ALL farm balances and teaches tilling");
        Capture("locked");
        Check(!resources.TryBuyKit(stoneCost, coinCost, FarmResource.SprinklerKit) && Same(before), "Direct purchase API also rejects locked sprinkler without spending");
        Select(tankBuy.GetComponent<XRSimpleInteractable>());
        Check(Same(before) && message.text.Contains("locked") && progress.CurrentGoal.Contains("harrow"), "Locked tank purchase gives the current tutorial and preserves every balance");
        Select(pickaxeBuy.GetComponent<XRSimpleInteractable>());
        Check(Same(before) && message.text.Contains("locked"), "Locked pickaxe purchase also preserves every balance");
        Check(!resources.TryBuyKit(tankBuy.StoneCost, tankBuy.CoinCost, FarmResource.TankKit) && Same(before), "Direct tank purchase API cannot bypass unlock");
        resources.Add(FarmResource.PickaxeKit, 1); before = Snapshot();
        Check(!pickaxe.Upgrade() && Same(before) && !pickaxe.IsUpgraded, "Preloaded pickaxe kit cannot bypass mining unlock or be consumed");
        Check(!cells[0].Plant() && Same(before), "Locked seed cannot plant or spend a seed");
        Check(cells[0].Till() && progress.Revealing && !Visible("moonSeeds"), "First till begins reveal before seed bag appears");
        yield return new WaitForSecondsRealtime(2.3f);
        Check(progress.Stage == 1 && Visible("moonSeeds") && !Visible("bloomSeeds"), "Threshold 1 opens only Moon Herb planting");
        cells[0].Water(100);
        Check(!progress.Revealing && !progress.MiningUnlocked, "Watering empty soil does not unlock mining");
        before = Snapshot();
        Check(!cells[1].Plant() && Same(before) && progress.CurrentGoal.Contains("harrow"), "Untilled planting gives a persistent preparation lesson without spending");
        Check(!cells[0].Plant(FarmCropKind.ConservatoryBloom) && Same(before), "Bloom remains gated after first unlock");
        int seedCount = resources.GetAmount(FarmResource.Seeds); resources.TrySpend(FarmResource.Seeds, seedCount);
        before = Snapshot();
        Check(!cells[0].Plant() && Same(before) && progress.CurrentGoal.Contains("Trade Moon Herbs"), "No-seed error gives recovery tutorial and no resource changes");
        resources.Add(FarmResource.Seeds, seedCount);
        Check(cells[0].Plant(), "Original seed planting succeeds once unlocked and funded");
        Check(!progress.MiningUnlocked && !Visible("miningTool"), "Planting in pre-watered soil alone does not unlock mining");
        before = Snapshot();
        Select(cells[0].Crop.GetComponentInChildren<XRSimpleInteractable>());
        Check(cells[0].IsPlanted && Same(before) && progress.CurrentGoal.Contains("soil moist"), "Immature GRIP interaction teaches watering without destroying plant or crediting harvest");
        yield return new WaitForSecondsRealtime(.2f);
        cells[0].Water(100);
        Check(progress.Revealing && !Visible("miningTool") && !Visible("miningField"), "Watering a planted Moon Herb starts the mining introduction before tools appear");
        Time.timeScale = 30;
        yield return new WaitForSeconds(31);
        Time.timeScale = 1;
        int herbs = manager.GetResource(ArcaneWorkshop.WorkshopResourceKind.MoonHerb);
        Select(cells[0].Crop.GetComponentInChildren<XRSimpleInteractable>());
        Check(progress.Revealing && !Visible("bloomSeeds") && manager.GetResource(ArcaneWorkshop.WorkshopResourceKind.MoonHerb) == herbs + 1, "Own harvest during mining reveal credits shared stock and queues the Bloom milestone");
        yield return new WaitForSecondsRealtime(2.3f);
        Check(progress.Stage == 2 && progress.Revealing && Visible("miningTool") && Visible("miningField") && !Visible("bloomSeeds"), "Threshold 2 reveals mining; queued Bloom introduction does not skip a stage");
        var rocks = FindObjectsByType<Boulder>(FindObjectsSortMode.None);
        Check(rocks.Length == 3, "Unlocked mining field spawns its original three rocks");
        var grab = pickaxe.GetComponentInParent<XRGrabInteractable>();
        xr.SelectEnter((IXRSelectInteractor)hand, (IXRSelectInteractable)grab);
        float hardness = rocks[0].Hardness;
        Check(pickaxe.Strike(rocks[0]) && rocks[0].Hardness < hardness, "Original grabbed pickaxe strike works after mining unlock");
        xr.SelectExit((IXRSelectInteractor)hand, (IXRSelectInteractable)grab);
        yield return new WaitForSecondsRealtime(2.3f);
        Check(progress.Stage == 3 && Visible("bloomSeeds") && !Visible("sprinklerCarrier"), "Threshold 3 opens Bloom, not sprinkler");
        before = Snapshot();
        Check(!buy.TryPurchase() && Same(before) && progress.CurrentGoal.Contains("Bloom"), "Locked purchase now teaches the current Bloom step, not the old tilling lesson");
        Check(cells[0].Plant(FarmCropKind.ConservatoryBloom), "Bloom plants with original operation after unlock");
        cells[0].Water(100); Time.timeScale = 30;
        yield return new WaitForSeconds(31);
        Time.timeScale = 1;
        int blooms = manager.GetResource(ArcaneWorkshop.WorkshopResourceKind.ConservatoryBloom);
        Select(cells[0].Crop.GetComponentInChildren<XRSimpleInteractable>());
        Check(progress.Revealing && !Visible("sprinklerCarrier") && manager.GetResource(ArcaneWorkshop.WorkshopResourceKind.ConservatoryBloom) == blooms + 2, "Bloom harvest credits exactly 2 and starts the sprinkler introduction");
        yield return new WaitForSecondsRealtime(2.3f);
        Check(progress.Stage == 4 && Visible("sprinklerCarrier") && !Visible("tankCarrier") && progress.CurrentGoal.Contains("sprinkler"), "Threshold 4 opens sprinkler and teaches automatic irrigation; tank stays locked");
        resources.TrySpend(FarmResource.Stones, resources.GetAmount(FarmResource.Stones));
        before = Snapshot();
        Check(!buy.TryPurchase() && Same(before) && progress.CurrentGoal.Contains("pickaxe"), "Insufficient stones do not partially charge coins; tutorial explains gathering");
        resources.Add(FarmResource.Stones, stoneCost);
        resources.TrySpend(FarmResource.Coins, resources.GetAmount(FarmResource.Coins));
        before = Snapshot();
        Check(!buy.TryPurchase() && Same(before) && progress.CurrentGoal.Contains("trade Moon Herbs"), "Insufficient coins do not partially charge stones; tutorial explains trading");
        yield return new WaitForSecondsRealtime(3.5f);
        Check(progress.CurrentGoal.Contains("trade Moon Herbs"), "Recovery tutorial remains after transient error disappears");
        Capture("materials");
        resources.Add(FarmResource.Coins, coinCost);
        yield return new WaitForSecondsRealtime(.3f);
        Check(progress.CurrentGoal.Contains("Materials ready"), "Tutorial advances when missing materials are obtained");
        before = Snapshot();
        Check(buy.TryPurchase() && resources.GetAmount(FarmResource.Stones) == before[(int)FarmResource.Stones] - stoneCost
            && resources.GetAmount(FarmResource.Coins) == before[(int)FarmResource.Coins] - coinCost
            && resources.GetAmount(FarmResource.SprinklerKit) == before[(int)FarmResource.SprinklerKit] + 1, "Valid purchase deducts both exact costs and grants exactly one kit");
        Check(cells[0].BuildSprinkler() && progress.CurrentGoal.Contains("Fill the sprinkler"), "Original placement consumes kit and teaches filling the sprinkler");
        var sprinkler = cells[0].Irrigator;
        resources.Add(FarmResource.TankKit, 1); before = Snapshot();
        Check(!sprinkler.Upgrade() && Same(before) && sprinkler.Capacity == 100f, "Preloaded tank kit cannot bypass successful-irrigation requirement");
        Check(!tankBuy.TryPurchase() && Same(before), "Tank purchase remains locked after sprinkler placement");
        cells[0].Water(100);
        yield return new WaitForSecondsRealtime(.3f);
        Check(progress.Stage == 4 && !progress.Revealing && !Visible("tankCarrier") && progress.CurrentGoal.Contains("nearby"), "Filling a sprinkler with no tilled neighbor does not unlock tank; tutorial explains the missing target");
        var neighbor = (FarmCell)new SerializedObject(cells[0]).FindProperty("neighbors").GetArrayElementAtIndex(0).objectReferenceValue;
        Check(neighbor.Till(), "Prepare the sprinkler's actual adjacent plot using original till operation");
        yield return new WaitForSecondsRealtime(.15f);
        Check(neighbor.Moisture > 0 && progress.Revealing && !Visible("tankCarrier"), "Real irrigation increases neighbor moisture and introduces tank before its carrier appears");
        yield return new WaitForSecondsRealtime(2.3f);
        Check(progress.Stage == 5 && progress.TankUnlocked && Visible("tankCarrier"), "Threshold 5 opens tank upgrade after successful irrigation");
        resources.TrySpend(FarmResource.TankKit, resources.GetAmount(FarmResource.TankKit));
        resources.TrySpend(FarmResource.Stones, resources.GetAmount(FarmResource.Stones));
        resources.Add(FarmResource.Coins, tankBuy.CoinCost); before = Snapshot();
        Check(!tankBuy.TryPurchase() && Same(before) && progress.CurrentGoal.Contains("pickaxe"), "Unaffordable tank purchase does not spend coins and teaches mining");
        resources.Add(FarmResource.Stones, tankBuy.StoneCost);
        resources.TrySpend(FarmResource.Coins, resources.GetAmount(FarmResource.Coins)); before = Snapshot();
        Check(!tankBuy.TryPurchase() && Same(before) && progress.CurrentGoal.Contains("trade Moon Herbs"), "Missing tank coins do not consume stones and teach trading");
        resources.Add(FarmResource.Coins, tankBuy.CoinCost); before = Snapshot();
        Check(tankBuy.TryPurchase() && resources.GetAmount(FarmResource.Stones) == before[(int)FarmResource.Stones] - tankBuy.StoneCost
            && resources.GetAmount(FarmResource.Coins) == before[(int)FarmResource.Coins] - tankBuy.CoinCost
            && resources.GetAmount(FarmResource.TankKit) == before[(int)FarmResource.TankKit] + 1, "Valid tank purchase charges both exact costs and grants one kit");
        Check(sprinkler.Upgrade() && sprinkler.Capacity == 200f && resources.GetAmount(FarmResource.TankKit) == 0
            && progress.CurrentGoal.Contains("Farm lessons complete"), "Original tank upgrade consumes one kit, doubles capacity and completes the five-stage tutorial");
        before = Snapshot();
        Check(!sprinkler.Upgrade() && Same(before), "Repeated tank upgrade cannot spend another kit");
    }

    private bool Visible(string field) => ((GameObject)new SerializedObject(progress).FindProperty(field).objectReferenceValue).activeInHierarchy;
    private int[] Snapshot() => Enum.GetValues(typeof(FarmResource)).Cast<FarmResource>().Select(r => resources.GetAmount(r)).ToArray();
    private bool Same(int[] before) => before.SequenceEqual(Snapshot());
    private void Select(XRSimpleInteractable target)
    {
        xr.SelectEnter((IXRSelectInteractor)hand, (IXRSelectInteractable)target);
        if (target && target.isSelected) xr.SelectExit((IXRSelectInteractor)hand, (IXRSelectInteractable)target);
    }
    private void Check(bool passed, string text)
    {
        if (!passed) throw new InvalidOperationException(text);
        lines.Add("PASS: " + text);
    }
    private void Capture(string name)
    {
        var board = GameObject.Find("farmLessonBoard").transform;
        var go = new GameObject("lessonPreviewCamera"); var camera = go.AddComponent<Camera>();
        camera.transform.position = board.TransformPoint(new Vector3(0, 0, -3f));
        camera.transform.rotation = board.rotation;
        camera.fieldOfView = 36;
        var rt = new RenderTexture(1200, 900, 24); camera.targetTexture = rt;
        camera.Render(); var previous = RenderTexture.active; RenderTexture.active = rt;
        var texture = new Texture2D(1200, 900, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, 1200, 900), 0, 0); texture.Apply();
        File.WriteAllBytes(Path.GetFullPath("../tmp/farm-lesson-" + name + ".png"), texture.EncodeToPNG());
        RenderTexture.active = previous; camera.targetTexture = null;
        Destroy(texture); rt.Release(); Destroy(rt); Destroy(go);
    }
}
