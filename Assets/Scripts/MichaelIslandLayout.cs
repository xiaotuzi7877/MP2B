using UnityEngine;

namespace ArcaneWorkshop
{
    /// <summary>
    /// Stable spatial contract for Michael Island. Gameplay systems use these anchors instead of
    /// assuming that the workshop is a circular island centered at the world origin.
    /// </summary>
    public sealed class MichaelIslandLayout : MonoBehaviour
    {
        [SerializeField] private Transform buildGridAnchor;
        [SerializeField] private Transform playerSpawnAnchor;
        [SerializeField] private Transform lucasGateAnchor;
        [SerializeField] private Transform justinGateAnchor;
        [SerializeField] private Transform nexusIndicatorAnchor;
        [SerializeField] private Vector2[] localWalkablePolygon;

        public static MichaelIslandLayout Instance { get; private set; }

        public Transform BuildGridAnchor { get { return buildGridAnchor; } }
        public Transform PlayerSpawnAnchor { get { return playerSpawnAnchor; } }
        public Transform LucasGateAnchor { get { return lucasGateAnchor; } }
        public Transform JustinGateAnchor { get { return justinGateAnchor; } }
        public Transform NexusIndicatorAnchor { get { return nexusIndicatorAnchor; } }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Configure(
            Transform grid,
            Transform spawn,
            Transform lucasGate,
            Transform justinGate,
            Transform nexus,
            Vector2[] polygon)
        {
            buildGridAnchor = grid;
            playerSpawnAnchor = spawn;
            lucasGateAnchor = lucasGate;
            justinGateAnchor = justinGate;
            nexusIndicatorAnchor = nexus;
            localWalkablePolygon = polygon;
        }

        public bool ContainsWalkablePoint(Vector3 worldPoint, float edgeMargin = 0f)
        {
            if (localWalkablePolygon == null || localWalkablePolygon.Length < 3)
            {
                return false;
            }

            Vector3 local = transform.InverseTransformPoint(worldPoint);
            Vector2 point = new Vector2(local.x, local.z);
            Vector2 center = Vector2.zero;
            for (int i = 0; i < localWalkablePolygon.Length; i++)
            {
                center += localWalkablePolygon[i];
            }
            center /= localWalkablePolygon.Length;

            bool inside = false;
            for (int i = 0, j = localWalkablePolygon.Length - 1; i < localWalkablePolygon.Length; j = i++)
            {
                Vector2 a = localWalkablePolygon[i];
                Vector2 b = localWalkablePolygon[j];
                if (edgeMargin > 0f)
                {
                    a = Vector2.MoveTowards(a, center, edgeMargin);
                    b = Vector2.MoveTowards(b, center, edgeMargin);
                }

                bool crosses = (a.y > point.y) != (b.y > point.y);
                if (crosses && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        public Vector3 ClampToWalkable(Vector3 worldPoint, float edgeMargin = 0.55f)
        {
            if (ContainsWalkablePoint(worldPoint, edgeMargin))
            {
                return worldPoint;
            }

            Vector3 local3 = transform.InverseTransformPoint(worldPoint);
            Vector2 point = new Vector2(local3.x, local3.z);
            Vector2 center = Vector2.zero;
            for (int i = 0; i < localWalkablePolygon.Length; i++)
            {
                center += localWalkablePolygon[i];
            }
            center /= localWalkablePolygon.Length;

            float bestDistance = float.PositiveInfinity;
            Vector2 best = center;
            for (int i = 0; i < localWalkablePolygon.Length; i++)
            {
                Vector2 a = Vector2.MoveTowards(localWalkablePolygon[i], center, edgeMargin);
                Vector2 b = Vector2.MoveTowards(localWalkablePolygon[(i + 1) % localWalkablePolygon.Length], center, edgeMargin);
                Vector2 candidate = ClosestPointOnSegment(point, a, b);
                float distance = (candidate - point).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            Vector3 clampedLocal = new Vector3(best.x, local3.y, best.y);
            return transform.TransformPoint(clampedLocal);
        }

        private static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 segment = b - a;
            float denominator = segment.sqrMagnitude;
            if (denominator < 0.0001f)
            {
                return a;
            }
            float t = Mathf.Clamp01(Vector2.Dot(point - a, segment) / denominator);
            return a + segment * t;
        }
    }
}
