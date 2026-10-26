using UnityEngine;

namespace ArcaneWorkshop
{
    public sealed class OrbitCameraController : MonoBehaviour
    {
        [SerializeField] private Vector3 target = Vector3.zero;
        [SerializeField] private float distance = 13f;
        [SerializeField] private float yaw = 38f;
        [SerializeField] private float pitch = 42f;

        private void Start()
        {
            ApplyTransform();
        }

        private void LateUpdate()
        {
            if (DesktopInput.MiddleHeld)
            {
                Vector2 delta = DesktopInput.MouseDelta * 0.1f;
                yaw += delta.x * 4f;
                pitch -= delta.y * 3f;
                pitch = Mathf.Clamp(pitch, 18f, 78f);
            }

            float scroll = DesktopInput.Scroll;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                distance -= Mathf.Sign(scroll) * 0.7f;
            }
            distance = Mathf.Clamp(distance, 7f, 21f);
            ApplyTransform();
        }

        // The stone walls stand along the north edge (z ~ 8.5); never orbit behind them.
        private const float MaxCameraZ = 7.6f;

        private void ApplyTransform()
        {
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 back = -(rotation * Vector3.forward);
            float allowed = distance;
            if (back.z > 0.01f)
            {
                allowed = Mathf.Min(distance, (MaxCameraZ - target.z) / back.z);
            }
            transform.position = target + back * Mathf.Max(allowed, 2f);
            transform.rotation = rotation;
        }
    }
}
