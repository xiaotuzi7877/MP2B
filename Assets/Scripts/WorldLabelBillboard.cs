using UnityEngine;

namespace ArcaneWorkshop
{
    public sealed class WorldLabelBillboard : MonoBehaviour
    {
        // Labels keep their authored world size even when the parent shrinks (wilting herbs), and
        // grow with distance so stats stay readable from the desktop view and across the VR island.
        private const float ReferenceDistance = 9f;
        private const float MaxDistanceScale = 1.6f;

        private float baseWorldScale = -1f;
        [SerializeField] private bool scaleWithDistance = true;

        public bool ScaleWithDistance
        {
            get { return scaleWithDistance; }
            set { scaleWithDistance = value; }
        }

        private void LateUpdate()
        {
            if (Camera.main == null)
            {
                return;
            }

            Vector3 cameraPosition = Camera.main.transform.position;
            transform.rotation = Quaternion.LookRotation(transform.position - cameraPosition, Vector3.up);

            if (!scaleWithDistance)
            {
                return;
            }
            if (baseWorldScale < 0f)
            {
                baseWorldScale = transform.lossyScale.x;
            }

            float distanceScale = Mathf.Clamp(Vector3.Distance(transform.position, cameraPosition) / ReferenceDistance, 1f, MaxDistanceScale);
            float parentScale = transform.parent != null ? transform.parent.lossyScale.x : 1f;
            if (parentScale > 0.0001f)
            {
                transform.localScale = Vector3.one * (baseWorldScale * distanceScale / parentScale);
            }
        }
    }
}
