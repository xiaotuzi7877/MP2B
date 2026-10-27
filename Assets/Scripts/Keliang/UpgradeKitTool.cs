using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class UpgradeKitTool : MonoBehaviour
{
    [SerializeField] private XRGrabInteractable grabInteractable;
    [SerializeField] private FarmResource kit;

    private void OnTriggerEnter(Collider other)
    {
        if (!grabInteractable.isSelected)
            return;
        switch (kit)
        {
            case FarmResource.SprinklerKit:
                if (other.TryGetComponent<FarmCell>(out var buildCell)) buildCell.BuildSprinkler();
                break;
            case FarmResource.PickaxeKit:
                if (other.TryGetComponent<PickaxeTool>(out var pickaxe)) pickaxe.Upgrade();
                break;
            case FarmResource.TankKit:
                if (other.TryGetComponent<Sprinkler>(out var sprinkler)) sprinkler.Upgrade();
                break;
            case FarmResource.SoilKit:
                if (other.TryGetComponent<FarmCell>(out var soilCell)) soilCell.UpgradeSoil();
                break;
        }
    }
}
