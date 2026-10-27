using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class FertilizerTool : MonoBehaviour
{
    [SerializeField] private XRGrabInteractable grabInteractable;

    private readonly HashSet<FarmCell> attemptedPlots = new HashSet<FarmCell>();

    private void OnTriggerStay(Collider other)
    {
        if (!grabInteractable.isSelected) { attemptedPlots.Clear(); return; }
        if (other.TryGetComponent<FarmCell>(out var cell) && attemptedPlots.Add(cell))
            cell.Fertilize();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<FarmCell>(out var cell)) attemptedPlots.Remove(cell);
    }
    private void OnDisable() => attemptedPlots.Clear();
}
