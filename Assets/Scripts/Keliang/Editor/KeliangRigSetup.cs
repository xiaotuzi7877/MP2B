using System;
using System.Linq;
using ArcaneWorkshop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public static class KeliangRigSetup
{
    [MenuItem("MP2B/Keliang/Configure Shared Rig and Collisions")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        var rig = GameObject.Find("playerRig");
        if (rig == null || GameObject.Find("keliangFarmIsland") == null) throw new InvalidOperationException("Open the integrated ArcaneWorkshop scene.");
        Undo.RegisterFullObjectHierarchyUndo(rig, "Configure shared XR rig");
        foreach (var controller in rig.GetComponentsInChildren<ControllerInputActionManager>(true))
        {
            var settings = new SerializedObject(controller);
            settings.FindProperty("m_SmoothMotionEnabled").boolValue = controller.name == "leftController";
            // Comfort (Michael): smooth turning made playtesters sick; snap turns with a blink are the VR comfort default.
            settings.FindProperty("m_SmoothTurnEnabled").boolValue = false;
            settings.FindProperty("m_NearFarInteractor").objectReferenceValue = controller.GetComponentInChildren<NearFarInteractor>(true);
            settings.FindProperty("m_TeleportInteractor").objectReferenceValue = controller.GetComponentsInChildren<XRRayInteractor>(true).Single(r => r.name == "teleportInteractor");
            settings.ApplyModifiedProperties();
        }
        foreach (var turn in rig.GetComponentsInChildren<ContinuousTurnProvider>(true)) turn.enabled = false;
        foreach (var turn in rig.GetComponentsInChildren<SnapTurnProvider>(true))
        {
            turn.enabled = true;
            turn.turnAmount = 30f;
        }
        foreach (var move in rig.GetComponentsInChildren<ContinuousMoveProvider>(true)) move.moveSpeed = 1.3f;
        foreach (var caster in rig.GetComponentsInChildren<SphereInteractionCaster>(true))
            caster.physicsLayerMask = Physics.DefaultRaycastLayers;
        foreach (var caster in rig.GetComponentsInChildren<CurveInteractionCaster>(true))
            caster.raycastMask = Physics.DefaultRaycastLayers;
        foreach (var ray in rig.GetComponentsInChildren<XRRayInteractor>(true))
        {
            ray.raycastMask = Physics.DefaultRaycastLayers;
            // Workstation interaction triggers must not block floor teleport rays.
            if (ray.name == "teleportInteractor") ray.raycastTriggerInteraction = QueryTriggerInteraction.Ignore;
        }

        var teamRoot = GameObject.Find("MP2B Team Expansion");
        Undo.RegisterFullObjectHierarchyUndo(teamRoot, "Fit workshop collisions to models");
        foreach (var station in teamRoot.GetComponentsInChildren<TeamExpansionStation>())
            foreach (var box in station.GetComponents<BoxCollider>()) box.isTrigger = true;
        int fitted = 0;
        foreach (var capsule in teamRoot.GetComponentsInChildren<CapsuleCollider>())
        {
            var mesh = capsule.GetComponent<MeshFilter>();
            Vector3 scale = capsule.transform.lossyScale;
            if (mesh == null || Mathf.Abs(scale.y) >= Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z))) continue;
            var owner = capsule.gameObject;
            Undo.DestroyObjectImmediate(capsule);
            var shape = Undo.AddComponent<MeshCollider>(owner);
            shape.sharedMesh = mesh.sharedMesh;
            fitted++;
        }
        SetBoundaryLayer(GameObject.Find("workshopBoundary").transform);
        var farm = GameObject.Find("keliangFarmIsland").transform;
        SetBoundaryLayer(farm.Find("sceneObjects/boundaryColliders"));
        var bridge = GameObject.Find("mainToFarmBridge").transform;
        foreach (var c in bridge.GetComponentsInChildren<Collider>())
            if (c.name.EndsWith("SafetyBarrier")) { Undo.RecordObject(c.gameObject, "Separate safety collision from rays"); c.gameObject.layer = 2; }

        var provider = rig.GetComponentInChildren<TeleportationProvider>();
        AddArea(GameObject.Find("Floating Workshop Island"), SharedRigTeleportArea.Region.Workshop,
            GameObject.Find("workshopBoundary").transform, Vector2.zero, provider);
        AddArea(farm.Find("sceneObjects/groundAndPaths/ground").gameObject, SharedRigTeleportArea.Region.Farm,
            farm, new Vector2(7.7f, 7.7f), provider);
        var deck = bridge.Find("walkableDeck");
        AddArea(deck.gameObject, SharedRigTeleportArea.Region.Bridge, bridge,
            new Vector2(1.1f, deck.localScale.z * .5f - .25f), provider);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log($"KELIANG SHARED RIG SAVED: fitted {fitted} platform colliders; station click volumes are triggers; safety barriers ignore pointing; three safe teleport surfaces.");
    }

    private static void SetBoundaryLayer(Transform root)
    {
        foreach (var c in root.GetComponentsInChildren<Collider>())
        {
            Undo.RecordObject(c.gameObject, "Separate safety collision from rays");
            c.gameObject.layer = 2;
        }
    }

    private static void AddArea(GameObject target, SharedRigTeleportArea.Region region, Transform space, Vector2 halfExtents, TeleportationProvider provider)
    {
        var area = target.GetComponent<SharedRigTeleportArea>();
        if (area == null) area = Undo.AddComponent<SharedRigTeleportArea>(target);
        Undo.RecordObject(area, "Configure safe teleport surface");
        area.region = region;
        area.regionSpace = space;
        area.halfExtents = halfExtents;
        area.teleportationProvider = provider;
        area.interactionLayers = 1 << 31; // Existing XRI Teleport interaction layer.
        area.matchOrientation = MatchOrientation.WorldSpaceUp;
        area.teleportTrigger = BaseTeleportationInteractable.TeleportTrigger.OnSelectExited;
        area.filterSelectionByHitNormal = true;
        area.upNormalToleranceDegrees = 25f;
        area.colliders.Clear();
        area.colliders.Add(target.GetComponent<Collider>());
        EditorUtility.SetDirty(area);
    }
}
