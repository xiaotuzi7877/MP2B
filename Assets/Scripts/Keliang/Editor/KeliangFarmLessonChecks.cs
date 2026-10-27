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
        xr = FindAnyObjectByType<XRInteractionManager>();
        var handObject = new GameObject("lessonTestHand"); handObject.AddComponent<SphereCollider>().isTrigger = true; hand = handObject.AddComponent<XRDirectInteractor>();
        Check(progress.Stage == 0 && !Visible("moonSeeds") && !Visible("bloomSeeds") && !Visible("sprinklerCarrier"), "Initial seed bags and sprinkler carrier hidden; stock does not unlock anything");
        Check(buy.gameObject.activeInHierarchy && buy.GetComponent<XRSimpleInteractable>().enabled, "Locked purchase button remains clickable");
        resources.Add(FarmResource.Stones, 20); resources.Add(FarmResource.Coins, 20);
        var before = Snapshot();
        Select(buy.GetComponent<XRSimpleInteractable>());
        Check(Same(before) && message.text.Contains("Invalid purchase") && progress.CurrentGoal.Contains("harrow"), "Actual locked purchase interaction preserves ALL farm balances and teaches tilling");
        Capture("locked");
        Check(!resources.TryBuyKit(stoneCost, coinCost, FarmResource.SprinklerKit) && Same(before), "Direct purchase API also rejects locked sprinkler without spending");
        Check(!cells[0].Plant() && Same(before), "Locked seed cannot plant or spend a seed");
        Check(cells[0].Till() && progress.Revealing && !Visible("moonSeeds"), "First till begins reveal before seed bag appears");
        yield return new WaitForSecondsRealtime(2.3f);
        Check(progress.Stage == 1 && Visible("moonSeeds") && !Visible("bloomSeeds"), "Threshold 1 opens only Moon Herb planting");
        before = Snapshot();
        Check(!cells[1].Plant() && Same(before) && progress.CurrentGoal.Contains("harrow"), "Untilled planting gives a persistent preparation lesson without spending");
        Check(!cells[0].Plant(FarmCropKind.ConservatoryBloom) && Same(before), "Bloom remains gated after first unlock");
        int seedCount = resources.GetAmount(FarmResource.Seeds); resources.TrySpend(FarmResource.Seeds, seedCount);
        before = Snapshot();
        Check(!cells[0].Plant() && Same(before) && progress.CurrentGoal.Contains("Trade Moon Herbs"), "No-seed error gives recovery tutorial and no resource changes");
        resources.Add(FarmResource.Seeds, seedCount);
        Check(cells[0].Plant(), "Original seed planting succeeds once unlocked and funded");
        before = Snapshot();
        Select(cells[0].Crop.GetComponentInChildren<XRSimpleInteractable>());
        Check(cells[0].IsPlanted && Same(before) && progress.CurrentGoal.Contains("soil moist"), "Immature GRIP interaction teaches watering without destroying plant or crediting harvest");
        cells[0].Water(100); Time.timeScale = 30;
        yield return new WaitForSeconds(31);
        Time.timeScale = 1;
        int herbs = manager.GetResource(ArcaneWorkshop.WorkshopResourceKind.MoonHerb);
        Select(cells[0].Crop.GetComponentInChildren<XRSimpleInteractable>());
        Check(progress.Revealing && !Visible("bloomSeeds") && manager.GetResource(ArcaneWorkshop.WorkshopResourceKind.MoonHerb) == herbs + 1, "Own Moon Herb harvest credits shared stock and introduces Bloom before revealing it");
        yield return new WaitForSecondsRealtime(2.3f);
        Check(progress.Stage == 2 && Visible("bloomSeeds") && !Visible("sprinklerCarrier"), "Threshold 2 opens Bloom, not sprinkler");
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
        Check(progress.Stage == 3 && Visible("sprinklerCarrier") && progress.CurrentGoal.Contains("Justin"), "Threshold 3 opens sprinkler and shows foundry handoff objective");
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
        cells[0].Water(100);
        yield return new WaitForSecondsRealtime(.3f);
        Check(progress.CurrentGoal.Contains("Justin"), "Completing recovery returns to the main handoff objective");
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
