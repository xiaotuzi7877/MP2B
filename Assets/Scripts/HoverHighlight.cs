using UnityEngine;

namespace ArcaneWorkshop
{
    /// <summary>
    /// Pointer feedback for island interactables (bridge gates, receivers, altar, Nexus Core): while the desktop
    /// mouse or VR ray rests on the object its label grows slightly, so players see what they can use.
    /// </summary>
    public sealed class HoverHighlight : MonoBehaviour
    {
        [SerializeField] private TextMesh label;
        [SerializeField] private float hoverScale = 1.18f;

        private float hoverTimer;
        private float blend;
        private Vector3 baseScale;
        private bool captured;

        public void Configure(TextMesh targetLabel)
        {
            label = targetLabel;
        }

        public void Hover()
        {
            hoverTimer = 0.12f;
        }

        private void LateUpdate()
        {
            if (label == null)
            {
                return;
            }
            if (!captured)
            {
                baseScale = label.transform.localScale;
                captured = true;
            }
            hoverTimer -= Time.deltaTime;
            float target = hoverTimer > 0f ? 1f : 0f;
            blend = Mathf.MoveTowards(blend, target, Time.deltaTime * 8f);
            label.transform.localScale = baseScale * Mathf.Lerp(1f, hoverScale, blend);
        }
    }
}
