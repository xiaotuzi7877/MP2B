using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArcaneWorkshop;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[InitializeOnLoad]
public static class KeliangFarmPlayChecks
{
    private const string PendingKey = "KeliangFarmPlayChecks.Pending";
    static KeliangFarmPlayChecks() => EditorApplication.playModeStateChanged += OnPlayMode;

    [MenuItem("MP2B/Keliang/Run Farm Play Checks")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before running migration checks.");
        KeliangFarmInstaller.Validate();
        SessionState.SetBool(PendingKey, true);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayMode(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false)) return;
        SessionState.SetBool(PendingKey, false);
        new GameObject("farmMigrationChecks").AddComponent<KeliangFarmPlayProbe>();
    }
}

public sealed class KeliangFarmPlayProbe : MonoBehaviour
{
    private readonly List<string> results = new List<string>();
    private readonly List<string> errors = new List<string>();
    private bool failed;
    private float priorTimeScale;
    private float priorVolume;

    private void OnEnable() => Application.logMessageReceived += ObserveError;
    private void OnDisable() => Application.logMessageReceived -= ObserveError;
    private void ObserveError(string message, string stack, LogType kind)
    {
        if (kind == LogType.Exception || kind == LogType.Assert ||
            (kind == LogType.Error && !message.Contains("OpenXR") && !message.Contains("XR_ERROR"))) errors.Add(message);
    }

    private IEnumerator Start()
    {
        priorTimeScale = Time.timeScale;
        priorVolume = AudioListener.volume;
        AudioListener.volume = 0f;
        yield return new WaitForSecondsRealtime(.8f);
        IEnumerator checks = Checks();
        while (true)
        {
            bool more;
            try { more = checks.MoveNext(); }
            catch (Exception e) { results.Add("FAIL: " + e); failed = true; break; }
            if (!more) break;
            yield return checks.Current;
        }
        Time.timeScale = priorTimeScale;
        AudioListener.volume = priorVolume;
        foreach (string error in errors) { results.Add("RUNTIME ERROR: " + error); failed = true; }
        string report = (failed ? "FAIL" : "PASS") + "\n" + string.Join("\n", results);
        File.WriteAllText(Path.GetFullPath("../tmp/keliang-farm-play-checks.txt"), report);
        if (failed) Debug.LogError("KELIANG PLAY CHECKS FAILED. See ../tmp/keliang-farm-play-checks.txt");
        else Debug.Log("KELIANG PLAY CHECKS PASSED: " + results.Count(r => r.StartsWith("PASS:")) + " checks; farm state is discarded on exiting Play Mode.");
        EditorApplication.isPlaying = false;
    }

    private IEnumerator Checks()
    {
        Check(FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c => c.enabled) == 1, "Single active camera in Play Mode");
        Check(FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(c => c.enabled) == 1, "Single audio listener in Play Mode");
        Check(FindObjectsByType<VRRig>(FindObjectsSortMode.None).Length == 0, "Old radius-limited VR rig does not spawn");
        Check(FindObjectsByType<FarmWorkshopRig>(FindObjectsSortMode.None).Length == 1, "Workshop adapter is active");
        Check(GameObject.Find("playerRig").GetComponent<CharacterController>() != null, "XRI rig has a physical character controller");
        Check(FindAnyObjectByType<ArcaneGameManager>().GetResource(WorkshopResourceKind.Gold) >= 35, "Main workshop initialized");

        var interactionManager = FindAnyObjectByType<XRInteractionManager>();
        var tool = FindAnyObjectByType<WateringCanTool>().GetComponent<XRGrabInteractable>();
        var handObject = new GameObject("migrationTestHand");
        handObject.AddComponent<SphereCollider>().isTrigger = true;
        var hand = handObject.AddComponent<XRDirectInteractor>();
        interactionManager.SelectEnter((IXRSelectInteractor)hand, (IXRSelectInteractable)tool);
        Check(tool.isSelected, "Imported watering can can be selected through XRI");
        interactionManager.SelectExit((IXRSelectInteractor)hand, (IXRSelectInteractable)tool);
        Check(!tool.isSelected, "Imported watering can can be released through XRI");
        Destroy(handObject);

        KeliangRigPlayChecks.Run(Check);
        var travel = KeliangRigPlayChecks.CheckTeleportTravel(Check);
        while (travel.MoveNext()) yield return travel.Current;
        CheckBridge();
        var inventory = FindAnyObjectByType<FarmResources>();
        var cells = FindObjectsByType<FarmCell>(FindObjectsSortMode.None).OrderBy(c => c.name).ToArray();
        Check(cells.Length == 9, "Nine original plots retained");
        var cell = cells[0];
        int seeds = inventory.GetAmount(FarmResource.Seeds);
        Check(cell.Till() && cell.Plant(), "Tilling and planting create linked crop prefabs");
        Check(inventory.GetAmount(FarmResource.Seeds) == seeds - 1, "Planting consumes exactly one seed");
        Check(!cell.Plant() && inventory.GetAmount(FarmResource.Seeds) == seeds - 1, "Duplicate planting does not spend another seed");
        cell.Water(100f);
        Check(cell.Moisture > 99f, "Imported soil receives water");
        var soil = cell.GetComponentInChildren<SoilMoisture>();
        var soilRenderer = new SerializedObject(soil).FindProperty("soilRenderer").objectReferenceValue as Renderer;
        var block = new MaterialPropertyBlock();
        soilRenderer.GetPropertyBlock(block);
        Check(block.GetColor("_Color").r < .1f, "Wet soil updates the Built-in shader color");
        inventory.Add(FarmResource.Fertilizer, 1);
        Check(cell.Fertilize() && cell.Crop.Growth >= 25f, "Fertilizer advances original crop growth");
        Check(!cell.Fertilize(), "Repeated fertilizer is rejected");
        Time.timeScale = 10f;
        float until = Time.realtimeSinceStartup + 5f;
        while (!cell.Crop.IsMature && Time.realtimeSinceStartup < until) yield return null;
        Time.timeScale = 1f;
        Check(cell.Crop.IsMature, "Crop reaches maturity using its real growth Update");
        Check(cell.Health.Health > 0f, "Watered crop remains alive");
        int crops = inventory.Crops;
        Check(cell.Harvest(cell.Crop), "Mature crop can be harvested");
        Check(inventory.Crops == crops + 1 && cell.IsTilled && !cell.IsPlanted, "Harvest yields one crop and retains soil");
        inventory.Add(FarmResource.SoilKit, 1);
        Check(cell.UpgradeSoil() && cell.IsSoilUpgraded, "Original soil upgrade still applies");
        int coins = inventory.GetAmount(FarmResource.Coins);
        Check(inventory.TryTrade(FarmResource.Crops, 1, FarmResource.Coins, 3) && inventory.GetAmount(FarmResource.Coins) == coins + 3,
            "Original crop sale updates farm inventory");

        var irrigatorCell = cells[1];
        irrigatorCell.Till();
        inventory.Add(FarmResource.SprinklerKit, 1);
        Check(irrigatorCell.BuildSprinkler(), "Sprinkler prefab builds on an empty plot");
        irrigatorCell.Water(100f);
        float before = irrigatorCell.Irrigator.Water;
        cell.Water(-50f);
        yield return new WaitForSeconds(1f);
        Check(irrigatorCell.Irrigator.Water < before, "Sprinkler neighbor references survive island relocation");
        Check(FindObjectsByType<TeamExpansionStation>(FindObjectsSortMode.None).Length == 3, "Existing team stations still run");
    }

    private void CheckBridge()
    {
        var bridge = GameObject.Find("mainToFarmBridge").transform;
        float length = bridge.Find("walkableDeck").localScale.z;
        Vector3 start = bridge.TransformPoint(new Vector3(0f, .1f, -length * .5f + .4f));
        Vector3 end = bridge.TransformPoint(new Vector3(0f, .1f, length * .5f - .2f));
        var walker = new GameObject("bridgeCollisionProbe");
        walker.transform.position = start;
        var capsule = walker.AddComponent<CharacterController>();
        capsule.height = 1.8f;
        capsule.radius = .25f;
        capsule.center = Vector3.up * .9f;
        Physics.SyncTransforms();
        int steps = Mathf.CeilToInt(Vector3.Distance(start, end) / .1f);
        bool supported = true;
        for (int i = 0; i < steps; i++)
        {
            Vector3 target = Vector3.Lerp(start, end, (i + 1f) / steps);
            capsule.Move(target - walker.transform.position + Vector3.down * .02f);
            Vector3 p = walker.transform.position;
            supported &= p.y > -.3f;
        }
        Check(supported, "Bridge collision supports the walker along its full length");
        results.Add("Crossing position " + walker.transform.position + "; target " + end + "; nearby colliders: " +
            string.Join(", ", Physics.OverlapSphere(walker.transform.position + Vector3.up * .8f, .8f)
                .Select(c => c.name + " at " + c.transform.position)));
        Check(Vector3.Distance(walker.transform.position, end) < .5f, "Bridge and farm boundary allow a complete crossing");
        for (int i = 0; i < steps; i++)
            capsule.Move(Vector3.Lerp(end, start, (i + 1f) / steps) - walker.transform.position + Vector3.down * .02f);
        results.Add("Return position " + walker.transform.position + "; target " + start + "; nearby colliders: " +
            string.Join(", ", Physics.OverlapSphere(walker.transform.position + Vector3.up * .8f, .8f)
                .Select(c => c.name + " at " + c.transform.position)));
        Check(Vector3.Distance(walker.transform.position, start) < .5f, "Return crossing to the main island is unobstructed");
        Destroy(walker);
    }

    private void Check(bool value, string description)
    {
        results.Add((value ? "PASS: " : "FAIL: ") + description);
        if (!value) throw new InvalidOperationException(description);
    }
}
