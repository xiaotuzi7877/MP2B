using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class KitPurchase : MonoBehaviour
{
    [SerializeField] private FarmResources resources;
    [SerializeField] private XRSimpleInteractable interactable;
    [SerializeField] private ActionFeedback feedback;
    [SerializeField] private FarmResource kit;
    [SerializeField, Min(1)] private int stoneCost;
    [SerializeField, Min(1)] private int coinCost;

    private void OnEnable() => interactable.selectEntered.AddListener(Buy);
    private void OnDisable() => interactable.selectEntered.RemoveListener(Buy);
    private void Buy(SelectEnterEventArgs args) => TryPurchase();

    public bool TryPurchase()
    {
        if (!resources.TryBuyKit(stoneCost, coinCost, kit, out string failure))
        {
            resources.Progression?.TeachPurchase(kit, stoneCost, coinCost);
            string next = resources.Progression != null ? "\nNext: " + resources.Progression.CurrentGoal : "";
            feedback.Failure(failure + next);
            return false;
        }
        resources.Progression?.ClearGuide();
        if (kit == FarmResource.SprinklerKit)
            resources.Progression?.Teach("Take the sprinkler carrier below the button and touch an empty tilled plot.", () => resources.GetAmount(FarmResource.SprinklerKit) == 0);
        feedback.Success("Kit bought - take its carrier below");
        return true;
    }
}
