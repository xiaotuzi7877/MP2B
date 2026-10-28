using System.Collections.Generic;
using ArcaneWorkshop;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

/// <summary>
/// Author: Michael (Ziyang Li).
/// The three-island layout after the comfort playtest. Lucas's farm and Justin's foundry sit 5 m closer to Michael
/// Island, so both main bridges are shorter while the mirror symmetry and the ritual islet between them stay; Keliang's
/// mainToFarmBridge is shortened in place (every plank, post and gate rail kept); and a third bridge joins the farm and
/// the foundry behind the ritual islet, so all three islands connect. Called at the end of Build Justin Foundry Island,
/// and therefore by Build Michael Island too. Idempotent: positions are absolute, the link bridge is rebuilt.
/// </summary>
public static class IslandLayoutBuilder
{
    // Keliang's installer placed the farm at (-28, 0, -36), 45.6 m out on the south-west diagonal, facing outward.
    private static readonly Vector3 FarmDirection = new Vector3(-28f, 0f, -36f).normalized;
    public const float OuterIslandDistance = 40.6f;
    public const string LinkName = "farmToFoundryBridge";

    // The link runs straight east-west, just south of the two island centres and clear of the ritual islet.
    private const float LinkOffsetZ = -0.6f;
    // Farm-local layout (KeliangFarmInstaller): 16.6 m walled yard, forest band out to the 31.6 m island edge.
    private const float FarmLinkEndX = -7.3f;
    private const float FarmWestWallX = -8.3f;
    private const float FarmEdge = 15.8f;
    private const float CorridorHalfWidth = 2.1f;

    public static Vector3 FarmCenter { get { return FarmDirection * OuterIslandDistance; } }
    public static Vector3 FoundryCenter { get { return new Vector3(-FarmCenter.x, 0f, FarmCenter.z); } }
    private static Quaternion FarmRotation { get { return Quaternion.LookRotation(FarmDirection, Vector3.up); } }
    public static float LinkZ { get { return FarmCenter.z + LinkOffsetZ; } }

    /// <summary>Foundry end of the link: just inside the rim, like the main bridge's landing.</summary>
    public static Vector3 LinkFoundryEnd
    {
        get
        {
            float inset = JustinFoundryBuilder.BridgeEndInset;
            return new Vector3(FoundryCenter.x - Mathf.Sqrt(inset * inset - LinkOffsetZ * LinkOffsetZ), 0f, LinkZ);
        }
    }

    /// <summary>Farm end of the link: 1 m inside the farm's west wall, where the yard is open ground.</summary>
    public static Vector3 LinkFarmEnd
    {
        get
        {
            Vector3 right = FarmRotation * Vector3.right;
            float x = FarmCenter.x + (FarmLinkEndX - LinkOffsetZ * right.z) / right.x;
            return new Vector3(x, 0f, LinkZ);
        }
    }

    internal static void LinkFarmAndFoundry(MichaelIslandBuilder.Materials m)
    {
        GameObject farm = GameObject.Find("keliangFarmIsland");
        if (farm == null)
        {
            Debug.LogWarning("keliangFarmIsland is missing: farm layout and farm-to-foundry bridge skipped.");
            return;
        }
        farm.transform.SetPositionAndRotation(FarmCenter, FarmRotation);
        ShortenFarmBridge(farm.transform);
        OpenFarmWestWall(farm.transform);

        GameObject old = GameObject.Find(LinkName);
        while (old != null)
        {
            Object.DestroyImmediate(old);
            old = GameObject.Find(LinkName);
        }
        GameObject link = JustinFoundryBuilder.CreateStoneBridge(LinkName, LinkFarmEnd, LinkFoundryEnd, m, m.lucasGlow, m.justinGlow,
            MichaelIslandBuilder.LucasColor, MichaelIslandBuilder.JustinColor, out _, out _);
        float length = Vector3.Distance(LinkFarmEnd, LinkFoundryEnd);
        TeleportationProvider provider = Provider();
        if (provider != null)
        {
            JustinFoundryBuilder.AddArea(link.transform.Find("walkableDeck").gameObject, SharedRigTeleportArea.Region.Bridge, link.transform,
                new Vector2(JustinFoundryBuilder.BridgeWidth * 0.5f - 0.4f, length * 0.5f - 0.25f), provider);
        }
        EditorUtility.SetDirty(farm);
        Debug.Log("Island layout: farm and foundry " + OuterIslandDistance + " m out, mainToFarmBridge shortened, " + LinkName +
            " built (" + length.ToString("F1") + " m).");
    }

    // Keliang's bridge runs from Lucas's gate (9 m out) to 7.3 m inside the farm. Keep its look: re-space every plank
    // and post along the new length, trim the deck, rails and barriers at the farm end, keep the gate rails in place.
    private static void ShortenFarmBridge(Transform farm)
    {
        GameObject bridgeObject = GameObject.Find("mainToFarmBridge");
        Transform deck = bridgeObject != null ? bridgeObject.transform.Find("walkableDeck") : null;
        if (deck == null)
        {
            Debug.LogWarning("mainToFarmBridge/walkableDeck is missing: farm bridge left as is.");
            return;
        }
        Transform bridge = bridgeObject.transform;
        Vector3 start = FarmDirection * MichaelIslandBuilder.GateRadius;
        Vector3 end = farm.TransformPoint(new Vector3(0f, 0f, -7.3f));
        end.y = start.y = 0f;
        float oldLength = deck.localScale.z;
        float newLength = Vector3.Distance(start, end);
        float ratio = newLength / oldLength;

        List<Transform> fixedParts = new List<Transform>();
        List<Pose> fixedPoses = new List<Pose>();
        foreach (Transform child in bridge)
        {
            if (child.name.Contains("Gate"))
            {
                fixedParts.Add(child);
                fixedPoses.Add(new Pose(child.position, child.rotation));
            }
        }
        Vector3 center = (start + end) * 0.5f;
        bridge.position = new Vector3(center.x, bridge.position.y, center.z);
        for (int i = 0; i < fixedParts.Count; i++)
        {
            fixedParts[i].SetPositionAndRotation(fixedPoses[i].position, fixedPoses[i].rotation);
        }

        foreach (Transform child in bridge)
        {
            if (fixedParts.Contains(child))
            {
                continue;
            }
            Vector3 position = child.localPosition;
            Vector3 scale = child.localScale;
            if (scale.z > 2f)
            {
                // Long parts keep their distance from both bridge ends.
                float near = position.z - scale.z * 0.5f + oldLength * 0.5f;
                float far = oldLength * 0.5f - (position.z + scale.z * 0.5f);
                float nearEdge = -newLength * 0.5f + near;
                float farEdge = newLength * 0.5f - far;
                position.z = (nearEdge + farEdge) * 0.5f;
                scale.z = farEdge - nearEdge;
            }
            else
            {
                position.z *= ratio;
                if (child.name.StartsWith("plank"))
                {
                    scale.z *= ratio;
                }
            }
            child.localPosition = position;
            child.localScale = scale;
        }
        SharedRigTeleportArea area = deck.GetComponent<SharedRigTeleportArea>();
        if (area != null)
        {
            area.halfExtents = new Vector2(area.halfExtents.x, newLength * 0.5f - 0.25f);
            EditorUtility.SetDirty(area);
        }
        EditorUtility.SetDirty(bridgeObject);
    }

    // Open the link gap in the farm's west wall and, like Keliang's entrance corridor, shift (never delete) the trees
    // standing in the way.
    private static void OpenFarmWestWall(Transform farm)
    {
        Vector3 farEnd = LinkFarmEnd;
        Vector3 outside = farEnd + Vector3.right * 30f;
        Vector3 localEnd = farm.InverseTransformPoint(farEnd);
        Vector3 localDirection = farm.InverseTransformDirection(Vector3.right);
        // Where the deck's centre line crosses the wall, and how wide the 38-degree crossing is along the wall.
        float toWall = (FarmWestWallX - localEnd.x) / localDirection.x;
        float gapCenter = localEnd.z + localDirection.z * toWall;
        float gapHalf = JustinFoundryBuilder.BridgeWidth * 0.5f / Mathf.Abs(localDirection.x) + 0.15f;

        Transform walls = farm.Find("sceneObjects/boundaryColliders");
        Transform west = walls != null ? walls.Find("westInvisibleWall") : null;
        if (west != null)
        {
            Vector3 wallCenter = walls.InverseTransformPoint(farm.TransformPoint(new Vector3(FarmWestWallX, 0f, gapCenter)));
            float halfLength = west.localScale.z * 0.5f;
            float wallZ = west.localPosition.z;
            float gapLow = wallCenter.z - gapHalf;
            float gapHigh = wallCenter.z + gapHalf;
            GameObject south = Object.Instantiate(west.gameObject, walls);
            south.name = "westBoundarySouth";
            SetSpan(south.transform, wallZ - halfLength, gapLow);
            west.name = "westBoundaryNorth";
            SetSpan(west, gapHigh, wallZ + halfLength);
        }

        Transform forest = farm.Find("sceneObjects/forest");
        if (forest == null)
        {
            return;
        }
        Vector2 a = new Vector2(farEnd.x, farEnd.z);
        Vector2 b = new Vector2(outside.x, outside.z);
        foreach (Transform tree in forest)
        {
            Renderer[] renderers = tree.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                continue;
            }
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers)
            {
                bounds.Encapsulate(r.bounds);
            }
            Vector2 p = new Vector2(bounds.center.x, bounds.center.z);
            float radius = Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.8f;
            float side = p.y - LinkZ;
            if (Mathf.Abs(side) >= CorridorHalfWidth + radius || p.x < a.x - 1f || p.x > b.x)
            {
                continue;
            }
            Vector3 local = farm.InverseTransformPoint(new Vector3(p.x, 0f, p.y));
            if (Mathf.Abs(local.x) > FarmEdge || Mathf.Abs(local.z) > FarmEdge)
            {
                continue;
            }
            // Shift across the corridor to the nearer side (north or south of the deck).
            float shift = (side >= 0f ? 1f : -1f) * (CorridorHalfWidth + radius) - side;
            tree.position += new Vector3(0f, 0f, shift);
        }

        void SetSpan(Transform wall, float from, float to)
        {
            Vector3 position = wall.localPosition;
            Vector3 scale = wall.localScale;
            position.z = (from + to) * 0.5f;
            scale.z = Mathf.Max(0.1f, to - from);
            wall.localPosition = position;
            wall.localScale = scale;
        }
    }

    private static TeleportationProvider Provider()
    {
        GameObject rig = GameObject.Find("playerRig");
        return rig != null ? rig.GetComponentInChildren<TeleportationProvider>(true) : null;
    }
}
