using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class StonePickup : MonoBehaviour
{
    [SerializeField] private FarmResources resources;
    [SerializeField] private XRSimpleInteractable interactable;
    private bool collected;

    public void Initialize(FarmResources inventory) => resources = inventory;

    private void OnEnable() => interactable.selectEntered.AddListener(Collect);
    private void OnDisable() => interactable.selectEntered.RemoveListener(Collect);

    private void Collect(SelectEnterEventArgs args)
    {
        if (collected)
            return;

        collected = true;
        resources.Add(FarmResource.Stones, 1);
        Destroy(gameObject);
    }
}
