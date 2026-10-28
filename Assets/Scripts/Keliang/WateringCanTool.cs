using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class WateringCanTool : MonoBehaviour
{
    [SerializeField] private XRGrabInteractable grabInteractable;
    [SerializeField] private Transform spout;
    [SerializeField] private LineRenderer waterStream;
    [SerializeField] private LayerMask soilMask;
    [SerializeField] private float wateringRate = 40f;
    [SerializeField] private float reach = 1.5f;
    // Pour like a real can (Michael): tipping the spout below the body pours; TRIGGER still works.
    [SerializeField] private float pourHeight = .05f;
    private const float MinReach = 2.5f;

    private bool triggerHeld;

    public bool Pouring => grabInteractable.isSelected && (triggerHeld || Tilted);
    private bool Tilted => spout.position.y - transform.position.y < pourHeight;

    private void OnEnable()
    {
        grabInteractable.activated.AddListener(StartPouring);
        grabInteractable.deactivated.AddListener(StopPouring);
        grabInteractable.selectExited.AddListener(Release);
    }

    private void OnDisable()
    {
        grabInteractable.activated.RemoveListener(StartPouring);
        grabInteractable.deactivated.RemoveListener(StopPouring);
        grabInteractable.selectExited.RemoveListener(Release);
        triggerHeld = false;
        waterStream.enabled = false;
    }

    private void StartPouring(ActivateEventArgs args) => triggerHeld = true;
    private void StopPouring(DeactivateEventArgs args) => triggerHeld = false;
    private void Release(SelectExitEventArgs args) => triggerHeld = false;

    private void Update()
    {
        waterStream.enabled = Pouring;
        if (!waterStream.enabled)
            return;

        float distance = Mathf.Max(reach, MinReach);
        Vector3 end = spout.position + Vector3.down * distance;
        if (Physics.SphereCast(spout.position, .12f, Vector3.down, out var hit, distance, soilMask, QueryTriggerInteraction.Collide))
        {
            end = spout.position + Vector3.down * hit.distance;
            var sprinkler = hit.collider.GetComponentInParent<Sprinkler>();
            var cell = hit.collider.GetComponentInParent<FarmCell>();
            if (sprinkler != null)
                sprinkler.AddWater(wateringRate * Time.deltaTime);
            else if (cell != null)
                cell.Water(wateringRate * Time.deltaTime);
        }
        waterStream.SetPosition(0, spout.position);
        waterStream.SetPosition(1, end);
    }
}
