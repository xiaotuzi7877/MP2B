using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using InputDevice = UnityEngine.XR.InputDevice;
using CommonUsages = UnityEngine.XR.CommonUsages;
using TrackedPoseDriver = UnityEngine.InputSystem.XR.TrackedPoseDriver;
using XRHMD = UnityEngine.InputSystem.XR.XRHMD;

namespace ArcaneWorkshop
{
    // Player rig for Quest. Tracking poses remain at Unity's one-unit-per-meter scale so the head
    // and hands stay at their real-world height. The right hand holds a wand whose ray drives the
    // same pointer pipeline as the desktop mouse; the left hand holds the spellbook menu.
    //
    // Right trigger: gather / build / press   Right grip: use a machine or the Merchant Gate
    // Right B: repair the pointed building     Left X: cancel placement
    // Left stick: walk where the head faces   Right stick forward: aim a teleport, release to jump
    // Right stick left/right: snap turn 30 degrees
    // Comfort: walking narrows the view with a vignette; teleports and turns hide behind a short blink.
    public sealed class VRRig : MonoBehaviour
    {
        // Just outside the 7x7 grid (half-width ~4.5 m) and inside the ring of rocks and trees
        // (radius ~9 m), so the head never starts inside scenery.
        private const float StationRadius = 7.5f;
        private const float StationAngle = 270f;
        // The floating island has an 11 m radius; keep the head a little inside its rim.
        private const float WalkableRadius = 10.3f;
        // Slower than a brisk walk: vection grows with speed and is the main cause of VR sickness.
        private const float WalkSpeed = 1.3f;
        private const float TeleportLaunchSpeed = 7f;
        private const float TeleportTimeStep = 0.04f;
        private const int TeleportMaxSteps = 60;
        private const float StickDeadZone = 0.15f;
        private const float SnapTurnDegrees = 30f;
        private const float DeviceTrackingEyeHeight = 1.65f;
        private const float RayLength = 60f;

        private static readonly InputFeatureUsage<Vector3> PointerPosition = new InputFeatureUsage<Vector3>("PointerPosition");
        private static readonly InputFeatureUsage<Quaternion> PointerRotation = new InputFeatureUsage<Quaternion>("PointerRotation");

        private ArcaneGameManager manager;
        private Transform head;
        private Transform leftHand;
        private Transform rightHand;
        private LineRenderer laser;
        private Transform reticle;
        private bool previousTrigger;
        private bool previousGrip;
        private bool previousRepair;
        private bool previousCancel;
        private bool stickCentered = true;
        private bool aimingTeleport;
        private bool teleportValid;
        private Vector3 teleportTarget;
        private LineRenderer teleportArc;
        private Transform teleportMarker;
        private ComfortVignette comfort;
        private float trackingOriginYOffset;
        private float lastLeftHandTrackedTime = float.NegativeInfinity;

        public Transform LeftHand { get { return leftHand; } }
        public Transform Head { get { return head; } }
        public bool LeftHandTrackedRecently { get { return Time.unscaledTime - lastLeftHandTrackedTime < 0.75f; } }

        public static VRRig Create(ArcaneGameManager manager, Camera camera, Material laserMaterial, Material wandMaterial, Material tipMaterial)
        {
            GameObject rigObject = new GameObject("VR Rig");
            VRRig rig = rigObject.AddComponent<VRRig>();
            rig.manager = manager;
            rigObject.transform.localScale = Vector3.one;

            OrbitCameraController orbit = camera.GetComponent<OrbitCameraController>();
            if (orbit != null)
            {
                orbit.enabled = false;
            }
            camera.transform.SetParent(rigObject.transform, false);
            camera.nearClipPlane = 0.05f;
            camera.transform.localPosition = Vector3.up * DeviceTrackingEyeHeight;
            camera.transform.localRotation = Quaternion.identity;
            rig.head = camera.transform;
            AddHeadTracking(camera.gameObject);
            rig.comfort = ComfortVignette.Create(rig.head);

            rig.leftHand = CreateHand("Left Hand (Spellbook)", rigObject.transform, null, null);
            rig.rightHand = CreateHand("Right Hand (Wand)", rigObject.transform, wandMaterial, tipMaterial);

            GameObject laserObject = new GameObject("Wand Ray");
            laserObject.transform.SetParent(rigObject.transform, false);
            rig.laser = laserObject.AddComponent<LineRenderer>();
            rig.laser.useWorldSpace = true;
            rig.laser.positionCount = 2;
            rig.laser.widthMultiplier = 0.035f;
            rig.laser.sharedMaterial = laserMaterial;
            rig.laser.startColor = new Color(0.55f, 0.9f, 1f, 0.9f);
            rig.laser.endColor = new Color(0.55f, 0.9f, 1f, 0.1f);
            rig.laser.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dot.name = "Wand Reticle";
            Destroy(dot.GetComponent<Collider>());
            dot.GetComponent<Renderer>().sharedMaterial = tipMaterial;
            dot.transform.localScale = Vector3.one * 0.12f;
            rig.reticle = dot.transform;

            GameObject arcObject = new GameObject("Teleport Arc");
            arcObject.transform.SetParent(rigObject.transform, false);
            rig.teleportArc = arcObject.AddComponent<LineRenderer>();
            rig.teleportArc.useWorldSpace = true;
            rig.teleportArc.widthMultiplier = 0.02f;
            rig.teleportArc.sharedMaterial = laserMaterial;
            rig.teleportArc.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rig.teleportArc.enabled = false;

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Teleport Marker";
            Destroy(marker.GetComponent<Collider>());
            marker.GetComponent<Renderer>().sharedMaterial = tipMaterial;
            marker.transform.localScale = new Vector3(0.5f, 0.01f, 0.5f);
            marker.SetActive(false);
            rig.teleportMarker = marker.transform;

            rig.ConfigureTrackingOrigin();
            rig.PlaceStation();
            return rig;
        }

        // The Input System TrackedPoseDriver is the standard OpenXR head path; it updates the camera
        // in Update and right before rendering, so the view follows the headset in both eyes.
        private static void AddHeadTracking(GameObject cameraObject)
        {
            TrackedPoseDriver driver = cameraObject.GetComponent<TrackedPoseDriver>();
            if (driver == null)
            {
                driver = cameraObject.AddComponent<TrackedPoseDriver>();
            }
            driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            driver.positionInput = new InputActionProperty(new InputAction("Head Position", InputActionType.Value, "<XRHMD>/centerEyePosition", expectedControlType: "Vector3"));
            driver.rotationInput = new InputActionProperty(new InputAction("Head Rotation", InputActionType.Value, "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion"));
        }

        private static Transform CreateHand(string name, Transform parent, Material wandMaterial, Material tipMaterial)
        {
            GameObject hand = new GameObject(name);
            hand.transform.SetParent(parent, false);
            if (wandMaterial != null)
            {
                GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shaft.name = "Wand Shaft";
                Destroy(shaft.GetComponent<Collider>());
                shaft.transform.SetParent(hand.transform, false);
                shaft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                shaft.transform.localPosition = new Vector3(0f, 0f, 0.06f);
                shaft.transform.localScale = new Vector3(0.012f, 0.09f, 0.012f);
                shaft.GetComponent<Renderer>().sharedMaterial = wandMaterial;

                GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                tip.name = "Wand Crystal";
                Destroy(tip.GetComponent<Collider>());
                tip.transform.SetParent(hand.transform, false);
                tip.transform.localPosition = new Vector3(0f, 0f, 0.155f);
                tip.transform.localScale = Vector3.one * 0.022f;
                tip.GetComponent<Renderer>().sharedMaterial = tipMaterial;
            }
            return hand.transform;
        }

        private void PlaceStation()
        {
            float radians = StationAngle * Mathf.Deg2Rad;
            Vector3 horizontalPosition = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians)) * StationRadius;
            transform.position = horizontalPosition + Vector3.up * trackingOriginYOffset;
            Vector3 toCenter = -horizontalPosition;
            transform.rotation = Quaternion.LookRotation(new Vector3(toCenter.x, 0f, toCenter.z));
        }

        private void ConfigureTrackingOrigin()
        {
            List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            bool floorTrackingActive = false;
            foreach (XRInputSubsystem subsystem in subsystems)
            {
                if (subsystem != null && subsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor))
                {
                    floorTrackingActive = true;
                }
            }

            if (!floorTrackingActive)
            {
                foreach (XRInputSubsystem subsystem in subsystems)
                {
                    if (subsystem != null)
                    {
                        subsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Device);
                    }
                }
            }

            // Device-origin poses are centered near the headset instead of the floor. Raising the
            // origin keeps the fallback camera at a normal standing eye height rather than y = 0.
            trackingOriginYOffset = floorTrackingActive ? 0f : DeviceTrackingEyeHeight;
        }

        private void OnEnable()
        {
            Application.onBeforeRender += UpdatePoses;
        }

        private void OnDisable()
        {
            Application.onBeforeRender -= UpdatePoses;
        }

        private void Update()
        {
            UpdatePoses();

            InputDevice right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            InputDevice left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            bool trigger = ReadButton(right, CommonUsages.triggerButton);
            bool grip = ReadButton(right, CommonUsages.gripButton);
            bool repair = ReadButton(right, CommonUsages.secondaryButton);
            bool cancel = ReadButton(left, CommonUsages.primaryButton);

            bool triggerDown = trigger && !previousTrigger;
            bool gripDown = grip && !previousGrip;
            bool repairDown = repair && !previousRepair;
            previousTrigger = trigger;
            previousGrip = grip;
            previousRepair = repair;

            if (cancel && !previousCancel)
            {
                manager.CancelPlacement();
            }
            previousCancel = cancel;

            Vector2 rightStick;
            right.TryGetFeatureValue(CommonUsages.primary2DAxis, out rightStick);
            UpdateTeleport(rightStick);

            Ray ray = new Ray(rightHand.position, rightHand.forward);
            laser.enabled = !aimingTeleport;
            if (aimingTeleport)
            {
                reticle.gameObject.SetActive(false);
            }
            else
            {
                manager.ProcessPointer(ray, triggerDown, gripDown || repairDown, repairDown);
                UpdateLaser(ray);
            }

            Vector2 stick;
            float motion = 0f;
            if (left.TryGetFeatureValue(CommonUsages.primary2DAxis, out stick))
            {
                motion = Walk(stick);
            }
            comfort.SetMotion(motion);

            if (!aimingTeleport)
            {
                if (stickCentered && Mathf.Abs(rightStick.x) > 0.7f)
                {
                    // Turn around the head so the view pivots in place instead of swinging the player.
                    comfort.Blink();
                    transform.RotateAround(head.position, Vector3.up, Mathf.Sign(rightStick.x) * SnapTurnDegrees);
                    stickCentered = false;
                }
                else if (rightStick.magnitude < 0.3f)
                {
                    stickCentered = true;
                }
            }
        }

        // Push the right stick forward to show an arc from the wand; letting go jumps there behind a blink.
        private void UpdateTeleport(Vector2 stick)
        {
            if (!aimingTeleport)
            {
                if (stickCentered && stick.y > 0.7f && Mathf.Abs(stick.x) < 0.5f)
                {
                    aimingTeleport = true;
                    stickCentered = false;
                }
                else
                {
                    return;
                }
            }

            if (stick.magnitude > 0.3f)
            {
                DrawTeleportArc();
                return;
            }

            aimingTeleport = false;
            stickCentered = true;
            teleportArc.enabled = false;
            teleportMarker.gameObject.SetActive(false);
            if (teleportValid)
            {
                comfort.Blink();
                Vector3 headFlat = new Vector3(head.position.x, 0f, head.position.z);
                transform.position += new Vector3(teleportTarget.x, 0f, teleportTarget.z) - headFlat;
            }
        }

        // A ballistic arc from the wand, ending where it drops through the floor the rig stands on.
        private void DrawTeleportArc()
        {
            float floorY = transform.position.y;
            Vector3 position = rightHand.position;
            Vector3 velocity = rightHand.forward * TeleportLaunchSpeed;
            List<Vector3> points = new List<Vector3> { position };
            teleportValid = false;
            for (int i = 0; i < TeleportMaxSteps; i++)
            {
                Vector3 next = position + velocity * TeleportTimeStep;
                velocity += Physics.gravity * TeleportTimeStep;
                if (next.y <= floorY)
                {
                    float t = Mathf.InverseLerp(position.y, next.y, floorY);
                    next = Vector3.Lerp(position, next, t);
                    points.Add(next);
                    Vector3 flat = new Vector3(next.x, 0f, next.z);
                    teleportValid = flat.magnitude <= WalkableRadius;
                    teleportTarget = next;
                    break;
                }
                points.Add(next);
                position = next;
            }

            teleportArc.enabled = true;
            teleportArc.positionCount = points.Count;
            teleportArc.SetPositions(points.ToArray());
            Color color = teleportValid ? new Color(0.55f, 0.9f, 1f, 0.9f) : new Color(1f, 0.35f, 0.35f, 0.6f);
            teleportArc.startColor = color;
            teleportArc.endColor = color;
            teleportMarker.gameObject.SetActive(teleportValid);
            teleportMarker.position = teleportTarget + Vector3.up * 0.02f;
        }

        // Smooth locomotion relative to where the head is looking, flattened onto the floor.
        // Returns how hard the stick is pushed so the comfort vignette can follow it.
        private float Walk(Vector2 stick)
        {
            if (stick.magnitude < StickDeadZone)
            {
                return 0f;
            }

            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            Vector3 sideways = Vector3.Cross(Vector3.up, forward);
            Vector3 move = (forward * stick.y + sideways * stick.x) * WalkSpeed * Time.deltaTime;

            // Clamp the head rather than the rig so room-scale steps cannot carry the player off the island.
            Vector3 headFlat = new Vector3(head.position.x, 0f, head.position.z);
            Vector3 target = Vector3.ClampMagnitude(headFlat + move, WalkableRadius);
            transform.position += target - headFlat;
            return Mathf.Clamp01(stick.magnitude);
        }

        private void UpdateLaser(Ray ray)
        {
            RaycastHit hit;
            Vector3 end = ray.origin + ray.direction * RayLength;
            bool hasHit = Physics.Raycast(ray, out hit, RayLength, ~0, QueryTriggerInteraction.Collide);
            if (hasHit)
            {
                end = hit.point;
            }
            laser.SetPosition(0, ray.origin);
            laser.SetPosition(1, end);
            reticle.gameObject.SetActive(hasHit);
            reticle.position = end;
        }

        private void UpdatePoses()
        {
            // Fallback only: the TrackedPoseDriver owns the head whenever the Input System sees the HMD.
            if (InputSystem.GetDevice<XRHMD>() == null && !UpdateNode(XRNode.CenterEye, head, false))
            {
                UpdateNode(XRNode.Head, head, false);
            }
            if (UpdateNode(XRNode.LeftHand, leftHand, false))
            {
                lastLeftHandTrackedTime = Time.unscaledTime;
            }
            UpdateNode(XRNode.RightHand, rightHand, true);
        }

        // Poses are reported in tracking space, which is the rig's local space.
        private static bool UpdateNode(XRNode node, Transform target, bool preferPointer)
        {
            if (target == null)
            {
                return false;
            }

            InputDevice device = InputDevices.GetDeviceAtXRNode(node);
            if (!device.isValid)
            {
                return false;
            }

            bool isTracked;
            if (device.TryGetFeatureValue(CommonUsages.isTracked, out isTracked) && !isTracked)
            {
                return false;
            }

            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            bool hasPosition = false;
            bool hasRotation = false;
            if (preferPointer)
            {
                hasPosition = device.TryGetFeatureValue(PointerPosition, out position);
                hasRotation = device.TryGetFeatureValue(PointerRotation, out rotation);
                if (hasPosition && hasRotation)
                {
                    target.localPosition = position;
                    target.localRotation = rotation;
                    return true;
                }
            }

            if (node == XRNode.CenterEye || node == XRNode.Head)
            {
                hasPosition = device.TryGetFeatureValue(CommonUsages.centerEyePosition, out position);
                hasRotation = device.TryGetFeatureValue(CommonUsages.centerEyeRotation, out rotation);
            }
            if (!hasPosition && !hasRotation)
            {
                hasPosition = device.TryGetFeatureValue(CommonUsages.devicePosition, out position);
                hasRotation = device.TryGetFeatureValue(CommonUsages.deviceRotation, out rotation);
            }

            if (hasPosition)
            {
                target.localPosition = position;
            }
            if (hasRotation)
            {
                target.localRotation = rotation;
            }
            return hasPosition || hasRotation;
        }

        private static bool ReadButton(InputDevice device, InputFeatureUsage<bool> usage)
        {
            bool value;
            return device.isValid && device.TryGetFeatureValue(usage, out value) && value;
        }
    }
}
