using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

// Authored walkable surfaces only, with space for the player's body at the destination.
public sealed class SharedRigTeleportArea : TeleportationArea
{
    public enum Region { Workshop, Farm, Bridge }
    public Region region;
    public Transform regionSpace;
    public Vector2 halfExtents;

    public bool CanLand(Vector3 point)
    {
        Vector3 local = regionSpace.InverseTransformPoint(point);
        if (region == Region.Workshop)
        {
            if (new Vector2(local.x, local.z).magnitude > 10.1f) return false;
        }
        else if (Mathf.Abs(local.x) > halfExtents.x || Mathf.Abs(local.z) > halfExtents.y) return false;
        foreach (var blocker in Physics.OverlapCapsule(point + Vector3.up * .35f, point + Vector3.up * 1.6f,
                     .23f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (colliders.Contains(blocker) || blocker.GetComponentInParent<FarmWorkshopRig>() != null) continue;
            return false;
        }
        return true;
    }

    public override bool IsSelectableBy(IXRSelectInteractor interactor) =>
        base.IsSelectableBy(interactor) && interactor is XRRayInteractor ray &&
        ray.TryGetCurrent3DRaycastHit(out RaycastHit hit) && CanLand(hit.point);

    protected override bool GenerateTeleportRequest(IXRInteractor interactor, RaycastHit hit, ref TeleportRequest request) =>
        CanLand(hit.point) && base.GenerateTeleportRequest(interactor, hit, ref request);
}
