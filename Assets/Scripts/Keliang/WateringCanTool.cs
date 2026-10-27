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

    private bool triggerHeld;

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
        waterStream.enabled = grabInteractable.isSelected && triggerHeld;
        if (!waterStream.enabled)
            return;

        Vector3 end = spout.position + Vector3.down * reach;
        if (Physics.Raycast(spout.position, Vector3.down, out var hit, reach, soilMask, QueryTriggerInteraction.Collide))
        {
            end = hit.point;
            if (hit.collider.TryGetComponent<Sprinkler>(out var sprinkler))
                sprinkler.AddWater(wateringRate * Time.deltaTime);
            else
                hit.collider.GetComponent<FarmCell>().Water(wateringRate * Time.deltaTime);
        }
        waterStream.SetPosition(0, spout.position);
        waterStream.SetPosition(1, end);
    }
}
