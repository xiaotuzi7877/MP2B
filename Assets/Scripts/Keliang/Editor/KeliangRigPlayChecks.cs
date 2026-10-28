using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;
using ArcaneWorkshop;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public static class KeliangRigPlayChecks
{
    public static IEnumerator CheckTeleportTravel(Action<bool, string> check)
    {
        var rig = UnityEngine.Object.FindAnyObjectByType<FarmWorkshopRig>();
        var provider = rig.GetComponentInChildren<TeleportationProvider>();
        Vector3 position = rig.transform.position;
        Quaternion rotation = rig.transform.rotation;
        try
        {
            foreach (var area in UnityEngine.Object.FindObjectsByType<SharedRigTeleportArea>(FindObjectsSortMode.None))
            {
                Vector3 destination = area.region == SharedRigTeleportArea.Region.Workshop ? new Vector3(0f, -.16f, -7.3f) :
                    area.region == SharedRigTeleportArea.Region.Farm ? area.regionSpace.TransformPoint(new Vector3(0f, -.06f, -5.4f)) : area.regionSpace.position;
                // Wild resources spawn dynamically. Find an actual exposed, unoccupied floor hit.
                if (!area.CanLand(destination))
                {
                    bool found = false;
                    for (int x = -8; x <= 8 && !found; x += 2)
                        for (int z = -8; z <= 8 && !found; z += 2)
                        {
                            Vector3 sample = area.regionSpace.TransformPoint(new Vector3(x, 0f, z));
                            if (Physics.Raycast(sample + Vector3.up * 4f, Vector3.down, out var hit, 8f,
                                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
                                area.colliders.Contains(hit.collider) && area.CanLand(hit.point))
                            { destination = hit.point; found = true; }
                        }
                }
                check(area.CanLand(destination), area.region + " has a clear playable teleport landing");
                provider.QueueTeleportRequest(new TeleportRequest { destinationPosition = destination, matchOrientation = MatchOrientation.WorldSpaceUp });
                for (int i = 0; i < 4; i++) yield return null;
                Vector3 head = Camera.main.transform.position;
                check(Vector2.Distance(new Vector2(head.x, head.z), new Vector2(destination.x, destination.z)) < .15f,
                    "XRI teleport provider actually moves the shared player to " + area.region);
            }
        }
        finally
        {
            rig.GetComponent<CharacterController>().enabled = false;
            rig.transform.SetPositionAndRotation(position, rotation);
            rig.GetComponent<CharacterController>().enabled = true;
            Physics.SyncTransforms();
        }
    }

    public static void Run(Action<bool, string> check)
    {
        var rig = UnityEngine.Object.FindAnyObjectByType<FarmWorkshopRig>();
        var manager = UnityEngine.Object.FindAnyObjectByType<ArcaneGameManager>();
        var xr = UnityEngine.Object.FindAnyObjectByType<XRInteractionManager>();
        var head = Camera.main.transform;
        var right = rig.transform.Find("cameraOffset/rightController");
        var left = rig.transform.Find("cameraOffset/leftController");
        bool rightActive = right.gameObject.activeSelf, leftActive = left.gameObject.activeSelf;
        // No headset is connected in editor checks; emulate tracked controllers for this frame.
        right.gameObject.SetActive(true);
        left.gameObject.SetActive(true);
        var aim = right.GetComponentInChildren<NearFarInteractor>(true);
        var leftInteractor = left.GetComponentInChildren<NearFarInteractor>(true);
        var book = rig.GetComponentInChildren<SpellbookPanel>(true);
        var tool = UnityEngine.Object.FindAnyObjectByType<WateringCanTool>().GetComponent<XRGrabInteractable>();
        var route = typeof(FarmWorkshopRig).GetMethod("RouteInput", BindingFlags.Instance | BindingFlags.NonPublic);
        void Input(bool trigger = false, bool grip = false, bool repair = false, bool cancel = false, bool menu = false, bool leftTracked = true)
            => route.Invoke(rig, new object[] { true, leftTracked, trigger, grip, repair, cancel, menu });
        Vector3 rigPosition = rig.transform.position, aimPosition = aim.transform.position;
        Quaternion aimRotation = aim.transform.rotation;
        Vector3 toolPosition = tool.transform.position;
        Quaternion toolRotation = tool.transform.rotation;
        var targetRoot = new GameObject("sharedRigInputProbe");
        var material = new Material(Shader.Find("Standard"));
        int presses = 0;
        var button = WorldButton.Create(targetRoot.transform, "Probe", Vector3.zero, Vector2.one, .02f, material, () => presses++);
        targetRoot.transform.position = new Vector3(0f, 5f, 0f);
        try
        {
            rig.GetComponent<CharacterController>().enabled = false;
            rig.transform.position += new Vector3(0f, head.position.y, -4f) - head.position;
            aim.transform.SetPositionAndRotation(new Vector3(0f, 5f, -2f), Quaternion.identity);
            Physics.SyncTransforms();
            Input();
            check(book.gameObject.activeInHierarchy, "Workshop spellbook attaches to the shared left hand");
            check(book.GetComponentsInChildren<Collider>().All(c => c.isTrigger), "Spellbook buttons are clickable without becoming physical obstacles");
            Input(trigger: true); Input(trigger: true);
            check(presses == 1, "Trigger activates the pointed workshop button once per press");
            Input(); Input(grip: true);
            check(presses == 2, "Free-hand grip routes to workshop use");
            Input(); Input(repair: true);
            check(presses == 3, "B routes the workshop secondary action");

            var near = aim.GetComponent<SphereInteractionCaster>();
            var far = aim.GetComponent<CurveInteractionCaster>();
            bool nearStabilized = near.enableStabilization, farStabilized = far.enableStabilization;
            near.enableStabilization = false;
            far.enableStabilization = false;
            var hits = new List<Collider>();
            Vector3 toolCenter = tool.colliders[0].bounds.center;
            Vector3 toolOffset = toolCenter - tool.transform.position;
            tool.transform.position = near.effectiveCastOrigin.position - toolOffset;
            Physics.SyncTransforms();
            near.TryGetColliderTargets(xr, hits);
            check(hits.Any(c => c.GetComponentInParent<XRGrabInteractable>() == tool), "Real XRI near cast detects farm tools on their visual layer");
            tool.transform.position = far.effectiveCastOrigin.position + far.effectiveCastOrigin.forward - toolOffset;
            Physics.SyncTransforms();
            hits.Clear();
            far.TryGetColliderTargets(xr, hits);
            check(hits.Any(c => c.GetComponentInParent<XRGrabInteractable>() == tool), "Real XRI far cast detects farm tools on their visual layer");
            tool.transform.SetPositionAndRotation(toolPosition, toolRotation);
            near.enableStabilization = nearStabilized;
            far.enableStabilization = farStabilized;
            Physics.SyncTransforms();

            Input();
            xr.HoverEnter((IXRHoverInteractor)aim, (IXRHoverInteractable)tool);
            Input(grip: true);
            check(presses == 3, "Hovering a farm tool reserves grip for XRI");
            xr.SelectEnter((IXRSelectInteractor)aim, (IXRSelectInteractable)tool);
            Input(trigger: true, grip: true, repair: true);
            check(tool.isSelected && presses == 3, "Holding a tool preserves its selection and blocks all workshop actions");
            xr.SelectExit((IXRSelectInteractor)aim, (IXRSelectInteractable)tool);
            xr.HoverExit((IXRHoverInteractor)aim, (IXRHoverInteractable)tool);
            tool.transform.SetPositionAndRotation(toolPosition, toolRotation);
            Input(trigger: true, grip: true, repair: true);
            check(presses == 3, "Held buttons cannot leak into workshop actions after releasing a tool");
            Input(); Input(trigger: true);
            check(presses == 4, "Workshop input resumes after releasing and pressing again");

            Input();
            xr.SelectEnter((IXRSelectInteractor)leftInteractor, (IXRSelectInteractable)tool);
            Input();
            check(!book.gameObject.activeSelf && tool.isSelected, "Left-hand tool use hides the book without dropping the tool");
            xr.SelectExit((IXRSelectInteractor)leftInteractor, (IXRSelectInteractable)tool);
            tool.transform.SetPositionAndRotation(toolPosition, toolRotation);
            Input(leftTracked: false);
            check(book.gameObject.activeSelf && book.transform.parent.parent == rig.transform,
                "Lost left-hand tracking moves the menu to the rig near the view");
            Input();
            // Since the tremor filter the book lives in rig space and snaps back onto the hand pose when tracking returns.
            check(book.transform.parent.parent == rig.transform &&
                  Vector3.Distance(book.transform.parent.position, left.TransformPoint(new Vector3(0f, .11f, .05f))) < .02f,
                "Restored left-hand tracking reattaches the book");
            Input(menu: true);
            check(!book.gameObject.activeSelf, "Y closes the book");
            Input(); Input(menu: true); Input();
            check(book.gameObject.activeSelf, "Y reopens the book");

            var buildButton = book.GetComponentsInChildren<WorldButton>().First(b => b.name.StartsWith("Button Rune Foundation"));
            manager.CancelPlacement(); // The original manager starts with foundation placement already selected.
            aim.transform.SetPositionAndRotation(buildButton.transform.position - book.transform.forward * .4f,
                Quaternion.LookRotation(book.transform.forward, book.transform.up));
            Physics.SyncTransforms();
            manager.TryGetPointerHit(new Ray(aim.transform.position, aim.transform.forward), out var bookHit);
            Input(); Input(trigger: true);
            check(manager.Placing && manager.SelectedBuilding == BuildingKind.RuneFoundation,
                "Pointing at the actual spellbook selects the original building action (hit=" +
                (bookHit.collider == null ? "none" : bookHit.collider.name) + ", button=" + buildButton.transform.position + ")");
            Input(); Input(cancel: true);
            check(!manager.Placing, "Left X cancels workshop placement");

            // Justin's Astral Foundry (Michael's build) replaced the old teammate station: grip and B both use its Star Catcher.
            var catcher = UnityEngine.Object.FindObjectsByType<FoundryStation>(FindObjectsSortMode.None)
                .Single(s => s.Kind == FoundryStationKind.StarCatcher);
            var foundry = catcher.GetComponentInParent<AstralFoundry>();
            if (!foundry.IsOpen)
            {
                // The gate polls for the first Bloom twice a second; this synchronous check awakens it directly.
                manager.RegisterTeammateOutput(WorkshopResourceKind.ConservatoryBloom, 1, "Lucas");
                var gate = UnityEngine.Object.FindObjectsByType<IslandLinkGate>(FindObjectsSortMode.None).Single(g => g.Owner == "Justin");
                typeof(IslandLinkGate).GetMethod("Awaken", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(gate, null);
            }
            manager.AddResource(WorkshopResourceKind.Crystal, 2);
            Vector3 foundryAim = catcher.GetComponent<BoxCollider>().bounds.center;
            aim.transform.SetPositionAndRotation(foundryAim + Vector3.up * 4f, Quaternion.LookRotation(Vector3.down, Vector3.forward));
            Physics.SyncTransforms();
            int charges = foundry.FocusCharges;
            Input(); Input(grip: true);
            check(foundry.FocusCharges == charges + AstralFoundry.ChargesPerCrystal, "Grip activates the actual teammate foundry station");
            charges = foundry.FocusCharges;
            Input(); Input(repair: true);
            check(foundry.FocusCharges == Mathf.Min(AstralFoundry.MaxCharges, charges + AstralFoundry.ChargesPerCrystal),
                "B reaches the foundry station without being swallowed by the workshop");
            aim.transform.SetPositionAndRotation(new Vector3(0f, 5f, -2f), Quaternion.identity);
            Physics.SyncTransforms();

            var teleportRay = right.GetComponentsInChildren<XRRayInteractor>(true).Single(r => r.name == "teleportInteractor");
            teleportRay.gameObject.SetActive(true);
            Input(trigger: true, grip: true);
            check(presses == 4 && !book.gameObject.activeSelf, "Teleport aiming suppresses workshop buttons and book");
            teleportRay.gameObject.SetActive(false);
            Input(grip: true);
            check(presses == 4, "Grip used to cancel teleport does not activate a machine");

            check(rig.GetComponentInChildren<ComfortVignette>() != null, "Shared rig has the original comfort overlay");
            check(rig.GetComponentInChildren<ContinuousMoveProvider>().moveSpeed == 1.3f, "Walking uses the original 1.3 m/s speed");
            check(rig.GetComponentInChildren<SnapTurnProvider>().isActiveAndEnabled &&
                rig.GetComponentInChildren<SnapTurnProvider>().turnAmount == 30f &&
                !rig.GetComponentsInChildren<ContinuousTurnProvider>().Any(t => t.enabled), "Only 30-degree snap turning is enabled");
            // Justin's foundry adds its own floor and bridge surfaces; this check covers the main island, farm and farm bridge.
            var areas = UnityEngine.Object.FindObjectsByType<SharedRigTeleportArea>(FindObjectsSortMode.None)
                .Where(a => a.regionSpace == null || !a.regionSpace.name.Contains("Foundry")).ToArray();
            check(areas.Length == 3, "Main island, bridge and farm have teleport surfaces");
            var bridgeArea = areas.Single(a => a.region == SharedRigTeleportArea.Region.Bridge);
            check(bridgeArea.CanLand(bridgeArea.regionSpace.position), "Bridge center accepts teleport landing");
            check(!bridgeArea.CanLand(bridgeArea.regionSpace.TransformPoint(new Vector3(2f, 0f, 0f))), "Teleport rejects bridge edge/outside landing");
            var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.transform.position = bridgeArea.regionSpace.position + Vector3.up;
            Physics.SyncTransforms();
            check(!bridgeArea.CanLand(bridgeArea.regionSpace.position), "Teleport rejects occupied landing space");
            UnityEngine.Object.DestroyImmediate(blocker);

            // Michael's altar and Justin's foundry machines are deliberately solid bodies (marked WalkBlocker);
            // anything else with a station click volume must stay a trigger.
            check(UnityEngine.Object.FindObjectsByType<TeamExpansionStation>(FindObjectsSortMode.None)
                    .All(s => s.GetComponents<BoxCollider>().All(c => c.isTrigger || s.GetComponent<WalkBlocker>() != null)) &&
                  UnityEngine.Object.FindObjectsByType<FoundryStation>(FindObjectsSortMode.None)
                    .All(s => s.GetComponent<WalkBlocker>() != null),
                "Station click volumes no longer form solid invisible boxes");
            var boundary = GameObject.Find("workshopBoundary");
            check(boundary.GetComponentsInChildren<Collider>().All(c => c.gameObject.layer == 2 && !c.isTrigger),
                "Safety boundaries still block bodies but not the workshop ray");
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.layer = 2;
            wall.transform.position = new Vector3(0f, 5f, -1f);
            Physics.SyncTransforms();
            check(manager.TryGetPointerHit(new Ray(aim.transform.position, aim.transform.forward), out var hit) && hit.collider == button.GetComponent<Collider>(),
                "Workshop ray passes through safety-only colliders");
            UnityEngine.Object.DestroyImmediate(wall);
            CheckStationApproach(check);
        }
        finally
        {
            if (aim.hasSelection) xr.SelectExit((IXRSelectInteractor)aim, (IXRSelectInteractable)tool);
            if (leftInteractor.hasSelection) xr.SelectExit((IXRSelectInteractor)leftInteractor, (IXRSelectInteractable)tool);
            rig.transform.position = rigPosition;
            aim.transform.SetPositionAndRotation(aimPosition, aimRotation);
            tool.transform.SetPositionAndRotation(toolPosition, toolRotation);
            rig.GetComponent<CharacterController>().enabled = true;
            right.gameObject.SetActive(rightActive);
            left.gameObject.SetActive(leftActive);
            UnityEngine.Object.DestroyImmediate(targetRoot);
            UnityEngine.Object.DestroyImmediate(material);
            Physics.SyncTransforms();
        }
    }

    private static void CheckStationApproach(Action<bool, string> check)
    {
        // The old teammate stations were replaced; Justin's Star Catcher is the station with an open approach.
        var station = UnityEngine.Object.FindObjectsByType<FoundryStation>(FindObjectsSortMode.None)
            .Single(s => s.Kind == FoundryStationKind.StarCatcher);
        var walker = new GameObject("stationCollisionProbe");
        var controller = walker.AddComponent<CharacterController>();
        controller.height = 1.7f;
        controller.radius = .2f;
        controller.center = Vector3.up * .85f;
        Vector3 start = station.transform.TransformPoint(new Vector3(0f, .25f, 2.4f));
        Vector3 end = station.transform.TransformPoint(new Vector3(0f, .25f, 1.15f));
        walker.transform.position = start;
        Physics.SyncTransforms();
        for (int i = 1; i <= 20; i++) controller.Move(Vector3.Lerp(start, end, i / 20f) - walker.transform.position + Vector3.down * .02f);
        check(Vector2.Distance(new Vector2(walker.transform.position.x, walker.transform.position.z), new Vector2(end.x, end.z)) < .12f,
            "Player capsule can approach the station through its former invisible box");
        UnityEngine.Object.DestroyImmediate(walker);
    }
}
