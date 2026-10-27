using System.Collections;
using System.Linq;
using ArcaneWorkshop;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

// XRI owns poses, locomotion and grabbing. Dispatch after XRI selection in LateUpdate.
[DefaultExecutionOrder(-100)]
public sealed class FarmWorkshopRig : MonoBehaviour
{
    [SerializeField] private ArcaneGameManager workshop;
    [SerializeField] private Transform head;
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightAim;
    [SerializeField] private GameObject farmHud;
    private SpellbookPanel book;
    private Transform bookMount, wand, reticle;
    private LineRenderer laser;
    private Material pointerMaterial;
    private XRBaseInteractor[] rightInteractors, leftInteractors;
    private XRRayInteractor[] teleportRays;
    private CurveVisualController[] rightCurves;
    private LocomotionProvider[] blinkProviders;
    private ComfortVignette comfort;
    private bool wasTrigger, wasGrip, wasRepair, wasCancel, wasMenu;
    private bool armed, wasInWorkshop, bookWanted, usingHeadFallback;
    private float teleportCooldown;
    private Vector3 priorPosition;

    public void Configure(ArcaneGameManager manager, Transform camera, Transform left, Transform aim, GameObject hud)
    {
        workshop = manager;
        head = camera;
        leftHand = left;
        rightAim = aim;
        farmHud = hud;
    }

    private void Awake() => workshop.UseSceneRig(head);

    private IEnumerator Start()
    {
        // The shared player uses the spellbook; retire the old fixed farm HUD.
        if (farmHud != null) farmHud.SetActive(false);
        var rightHand = rightAim.GetComponentInParent<ControllerInputActionManager>(true).transform;
        rightInteractors = rightHand.GetComponentsInChildren<XRBaseInteractor>(true);
        leftInteractors = leftHand.GetComponentsInChildren<XRBaseInteractor>(true);
        teleportRays = GetComponentsInChildren<XRRayInteractor>(true).Where(r => r.name == "teleportInteractor").ToArray();
        rightCurves = rightAim.GetComponentsInChildren<CurveVisualController>(true);
        comfort = ComfortVignette.Create(head);
        blinkProviders = GetComponentsInChildren<LocomotionProvider>(true)
            .Where(p => p is SnapTurnProvider || p is TeleportationProvider).ToArray();
        foreach (var provider in blinkProviders) provider.locomotionStarted += Blink;
        priorPosition = transform.position;
        yield return null;
        bookMount = new GameObject("workshopSpellbookMount").transform;
        book = workshop.CreateSceneRigSpellbook(bookMount);
        book.gameObject.SetActive(false);
        AttachBookToHand();
        CreatePointer();
    }

    private void Update()
    {
        workshop.ExternalPointerActive = Tracked(InputDevices.GetDeviceAtXRNode(XRNode.RightHand));
    }

    private void LateUpdate()
    {
        if (book == null) return;
        var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        RouteInput(Tracked(right), Tracked(left), Read(right, CommonUsages.triggerButton),
            Read(right, CommonUsages.gripButton), Read(right, CommonUsages.secondaryButton),
            Read(left, CommonUsages.primaryButton), Read(left, CommonUsages.secondaryButton));
        float speed = Vector3.ProjectOnPlane(transform.position - priorPosition, Vector3.up).magnitude / Mathf.Max(Time.deltaTime, .001f);
        comfort.SetMotion(speed > 5f ? 0f : Mathf.Clamp01(speed / 1.3f));
        priorPosition = transform.position;
    }

    private void RouteInput(bool rightTracked, bool leftTracked, bool trigger, bool grip, bool repair, bool cancel, bool menu)
    {
        bool inWorkshop = new Vector2(head.position.x, head.position.z).magnitude < 12f;
        if (inWorkshop != wasInWorkshop) bookWanted = inWorkshop;
        wasInWorkshop = inWorkshop;
        bool teleporting = teleportRays.Any(r => r.isActiveAndEnabled);
        if (teleporting) teleportCooldown = Time.unscaledTime + .15f;
        bool teleportBusy = teleporting || Time.unscaledTime < teleportCooldown;
        bool leftBusy = Busy(leftInteractors);
        bool rightBusy = Busy(rightInteractors);
        if (menu && !wasMenu && leftTracked && !leftBusy && !teleportBusy) bookWanted = !bookWanted;
        bool showBook = bookWanted && rightTracked && !leftBusy && !teleportBusy;
        book.gameObject.SetActive(showBook);
        if (showBook) UpdateBookPose(leftTracked);

        // Consume blocked presses, then require release before changing input ownership.
        if (!rightTracked || rightBusy || teleportBusy) armed = false;
        else if (!trigger && !grip && !repair) armed = true;
        Ray ray = new Ray(rightAim.position, rightAim.forward);
        bool hasHit = workshop.TryGetPointerHit(ray, out RaycastHit hit);
        bool bookTarget = hasHit && hit.collider.transform.IsChildOf(book.transform);
        bool pointerActive = rightTracked && !rightBusy && !teleportBusy && (inWorkshop || (showBook && bookTarget));
        if (pointerActive)
        {
            bool repairDown = armed && repair && !wasRepair;
            workshop.ProcessPointer(ray, armed && trigger && !wasTrigger && !repairDown,
                (armed && grip && !wasGrip) || repairDown, repairDown);
        }
        else workshop.ClearPointerHover();
        if (cancel && !wasCancel && leftTracked && !leftBusy && !teleportBusy) workshop.CancelPlacement();
        SetPointerVisible(pointerActive, hasHit, ray, hit);
        wasTrigger = trigger;
        wasGrip = grip;
        wasRepair = repair;
        wasCancel = cancel;
        wasMenu = menu;
    }

    private static bool Busy(XRBaseInteractor[] interactors) => interactors != null &&
        interactors.Any(i => i.isActiveAndEnabled && (i.hasSelection || i.hasHover));

    private void AttachBookToHand()
    {
        bookMount.SetParent(leftHand, false);
        bookMount.localPosition = new Vector3(0f, .11f, .05f);
        bookMount.localRotation = Quaternion.Euler(45f, 0f, 0f);
        bookMount.localScale = Vector3.one * .085f;
    }

    private void UpdateBookPose(bool leftTracked)
    {
        if (leftTracked)
        {
            if (usingHeadFallback) AttachBookToHand();
            usingHeadFallback = false;
        }
        else
        {
            bool snap = !usingHeadFallback;
            if (snap) bookMount.SetParent(transform, true);
            usingHeadFallback = true;
            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if (forward.sqrMagnitude < .001f) forward = transform.forward;
            Quaternion yaw = Quaternion.LookRotation(forward.normalized);
            Vector3 position = head.position + yaw * new Vector3(-.36f, -.35f, .7f);
            Quaternion rotation = yaw * Quaternion.Euler(25f, -20f, 0f);
            float t = snap ? 1f : 1f - Mathf.Exp(-4f * Time.unscaledDeltaTime);
            bookMount.SetPositionAndRotation(Vector3.Lerp(bookMount.position, position, t), Quaternion.Slerp(bookMount.rotation, rotation, t));
            bookMount.localScale = Vector3.one * .075f;
        }
        book.SetTrackingFallback(usingHeadFallback);
    }

    private void CreatePointer()
    {
        pointerMaterial = new Material(Shader.Find("Sprites/Default")) { color = new Color(.55f, .9f, 1f, .85f) };
        wand = new GameObject("workshopWand").transform;
        wand.SetParent(rightAim, false);
        var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(shaft.GetComponent<Collider>());
        shaft.transform.SetParent(wand, false);
        shaft.transform.localPosition = new Vector3(0f, 0f, .07f);
        shaft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        shaft.transform.localScale = new Vector3(.012f, .09f, .012f);
        shaft.GetComponent<Renderer>().sharedMaterial = pointerMaterial;
        var line = new GameObject("workshopPointer");
        line.transform.SetParent(transform, false);
        laser = line.AddComponent<LineRenderer>();
        laser.sharedMaterial = pointerMaterial;
        laser.positionCount = 2;
        laser.widthMultiplier = .004f;
        laser.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(dot.GetComponent<Collider>());
        reticle = dot.transform;
        reticle.name = "workshopReticle";
        reticle.SetParent(transform, false);
        reticle.localScale = Vector3.one * .018f;
        dot.GetComponent<Renderer>().sharedMaterial = pointerMaterial;
        wand.gameObject.SetActive(false);
        laser.enabled = false;
        dot.SetActive(false);
    }

    private void SetPointerVisible(bool visible, bool hit, Ray ray, RaycastHit result)
    {
        wand.gameObject.SetActive(visible);
        laser.enabled = visible;
        reticle.gameObject.SetActive(visible && hit);
        foreach (var curve in rightCurves) curve.gameObject.SetActive(!visible);
        if (!visible) return;
        Vector3 end = hit ? result.point : ray.GetPoint(12f);
        laser.SetPosition(0, ray.origin + ray.direction * .16f);
        laser.SetPosition(1, end);
        reticle.position = end;
    }

    private void Blink(LocomotionProvider provider) => comfort.Blink();

    private void OnDestroy()
    {
        if (blinkProviders != null)
            foreach (var provider in blinkProviders) if (provider != null) provider.locomotionStarted -= Blink;
        if (pointerMaterial != null) Destroy(pointerMaterial);
        if (bookMount != null) Destroy(bookMount.gameObject);
    }

    private static bool Tracked(InputDevice device) => device.isValid &&
        (!device.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) || tracked);

    private static bool Read(InputDevice device, InputFeatureUsage<bool> button) =>
        device.TryGetFeatureValue(button, out bool pressed) && pressed;
}

