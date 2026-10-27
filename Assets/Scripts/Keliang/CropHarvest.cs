using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class CropHarvest : MonoBehaviour
{
    [SerializeField] private XRSimpleInteractable interactable;
    [SerializeField] private CropGrowth crop;
    private FarmCell cell;
    private InteractionPrompt prompt;
    private bool wasMature;
    private void Awake() => prompt = GetComponent<InteractionPrompt>();

    public void Initialize(FarmCell plantedCell)
    {
        cell = plantedCell;
        RefreshPrompt();
    }

    private void Update()
    {
        if (crop.IsMature != wasMature) RefreshPrompt();
    }
    private void RefreshPrompt()
    {
        wasMature = crop.IsMature;
        if (prompt != null) prompt.SetMessages(wasMature ? "MATURE " + crop.name + "\nGRIP to harvest" : "GROWING " + crop.name + "\nGRIP to check - keep soil moist");
    }

    private void OnEnable() => interactable.selectEntered.AddListener(Harvest);
    private void OnDisable() => interactable.selectEntered.RemoveListener(Harvest);

    private void Harvest(SelectEnterEventArgs args) => cell.Harvest(crop);
}
