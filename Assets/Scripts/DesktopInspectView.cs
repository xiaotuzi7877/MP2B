using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace ArcaneWorkshop
{
    /// <summary>
    /// Desktop inspection camera for Play Mode without a headset. The shared XR rig's eye cannot move without an HMD,
    /// so this orbit camera takes over as the main camera (the mouse pointer still clicks through it).
    /// Middle drag or Alt + left drag: look around · wheel: zoom · WASD: move · Q/E: down/up ·
    /// Tab: next island (workshop, Lucas's farm, Justin's foundry, ritual islet) · V: back to the player's eye.
    /// Right click stays "use", as everywhere else in the workshop. It never activates while a headset is running.
    /// </summary>
    public sealed class DesktopInspectView : MonoBehaviour
    {
        private static readonly string[] FocusNames =
        {
            "Floating Workshop Island", "keliangFarmIsland", "Justin Astral Foundry", "Central Ritual Islet"
        };
        private static readonly string[] FocusTitles =
        {
            "Michael's Workshop", "Lucas's Farm", "Justin's Astral Foundry", "Ritual Islet"
        };
        private static readonly float[] FocusDistances = { 15f, 26f, 18f, 14f };

        private Camera eyeCamera;
        private AudioListener eyeListener;
        private Camera inspectCamera;
        private AudioListener inspectListener;
        private Vector3 target;
        private float yaw = 0f;
        private float pitch = 35f;
        private float distance = 15f;
        private int focus;
        private bool inspecting;
        private string eyeTag;
        private GUIStyle style;

        public bool Inspecting { get { return inspecting; } }

        public static DesktopInspectView Attach(Transform rigHead)
        {
            Camera eye = rigHead != null ? rigHead.GetComponent<Camera>() : null;
            if (eye == null)
            {
                return null;
            }
            GameObject host = new GameObject("Desktop Inspect View");
            DesktopInspectView view = host.AddComponent<DesktopInspectView>();
            view.eyeCamera = eye;
            view.eyeListener = rigHead.GetComponent<AudioListener>();
            return view;
        }

        private void Start()
        {
            inspectCamera = gameObject.AddComponent<Camera>();
            inspectCamera.CopyFrom(eyeCamera);
            inspectCamera.stereoTargetEye = StereoTargetEyeMask.None;
            inspectCamera.targetTexture = null;
            inspectCamera.fieldOfView = 60f;
            inspectCamera.nearClipPlane = 0.05f;
            inspectCamera.farClipPlane = 600f;
            inspectCamera.enabled = false;
            inspectListener = gameObject.AddComponent<AudioListener>();
            inspectListener.enabled = false;
            eyeTag = eyeCamera.tag;
            Focus(0, true);
            SetInspecting(!HeadsetRunning());
        }

        private static bool HeadsetRunning()
        {
            return XRSettings.isDeviceActive;
        }

        private void SetInspecting(bool on)
        {
            if (on && HeadsetRunning())
            {
                on = false;
            }
            inspecting = on;
            inspectCamera.enabled = on;
            inspectCamera.tag = on ? "MainCamera" : "Untagged";
            eyeCamera.enabled = !on;
            eyeCamera.tag = on ? "Untagged" : eyeTag;
            if (eyeListener != null)
            {
                eyeListener.enabled = !on;
            }
            inspectListener.enabled = on;
        }

        private void Focus(int index, bool snap)
        {
            for (int tries = 0; tries < FocusNames.Length; tries++)
            {
                int candidate = (index + tries) % FocusNames.Length;
                GameObject found = GameObject.Find(FocusNames[candidate]);
                if (found == null)
                {
                    continue;
                }
                focus = candidate;
                target = found.transform.position + Vector3.up * 0.5f;
                distance = FocusDistances[candidate];
                if (snap || candidate == 0)
                {
                    // Look at the island from Michael Island's side so its front faces the camera.
                    Vector3 fromHome = target - new Vector3(0f, target.y, 0f);
                    yaw = candidate == 0 ? 0f : Mathf.Atan2(fromHome.x, fromHome.z) * Mathf.Rad2Deg;
                }
                return;
            }
        }

        private void LateUpdate()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.vKey.wasPressedThisFrame)
            {
                SetInspecting(!inspecting);
            }
            if (!inspecting)
            {
                return;
            }
            if (HeadsetRunning())
            {
                SetInspecting(false);
                return;
            }

            if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
            {
                Focus(focus + 1, true);
            }

            Mouse mouse = Mouse.current;
            bool alt = keyboard != null && (keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed);
            if (mouse != null && (mouse.middleButton.isPressed || (alt && mouse.leftButton.isPressed)))
            {
                Vector2 delta = mouse.delta.ReadValue();
                yaw += delta.x * 0.25f;
                pitch = Mathf.Clamp(pitch - delta.y * 0.2f, -10f, 85f);
            }
            float scroll = DesktopInput.Scroll;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                distance = Mathf.Clamp(distance * (scroll > 0f ? 0.88f : 1.14f), 2f, 80f);
            }

            if (keyboard != null)
            {
                Quaternion flatYaw = Quaternion.Euler(0f, yaw, 0f);
                Vector3 move = Vector3.zero;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move += flatYaw * Vector3.forward;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move -= flatYaw * Vector3.forward;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move += flatYaw * Vector3.right;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move -= flatYaw * Vector3.right;
                if (keyboard.eKey.isPressed) move += Vector3.up;
                if (keyboard.qKey.isPressed) move -= Vector3.up;
                target += move * Mathf.Max(4f, distance * 0.6f) * Time.unscaledDeltaTime;
            }

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.SetPositionAndRotation(target - rotation * Vector3.forward * distance, rotation);
        }

        private void OnGUI()
        {
            if (inspecting == false && HeadsetRunning())
            {
                return;
            }
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 13, wordWrap = true };
                style.normal.textColor = Color.white;
            }
            string text = inspecting
                ? "INSPECT VIEW — " + FocusTitles[focus] + "\nMiddle drag / Alt + left drag: look   Wheel: zoom   WASD / QE: move\n" +
                  "Tab: next island   V: player eye   Left click / Right click: same as in the game"
                : "PLAYER EYE — V: inspect view";
            GUI.Box(new Rect(Screen.width - 470f, Screen.height - (inspecting ? 72f : 30f), 460f, inspecting ? 64f : 24f), text, style);
        }
    }
}
