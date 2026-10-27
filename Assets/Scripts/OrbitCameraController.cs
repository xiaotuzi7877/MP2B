using UnityEngine;

namespace ArcaneWorkshop
{
    public sealed class OrbitCameraController : MonoBehaviour
    {
        [SerializeField] private Vector3 target = new Vector3(0f, 0f, 1.4f);
        [SerializeField] private float distance = 24f;
        [SerializeField] private float yaw;
        [SerializeField] private float pitch = 38f;

        public void ConfigureFrame(Vector3 lookTarget, float orbitDistance, float orbitYaw, float orbitPitch)
        {
            target = lookTarget;
            distance = orbitDistance;
            yaw = orbitYaw;
            pitch = orbitPitch;
            ApplyTransform();
        }

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
            distance = Mathf.Clamp(distance, 8f, 28f);
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
