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
    public int StoneCost => stoneCost;
    public int CoinCost => coinCost;

    public void RefreshAvailabilityLabel()
    {
        var label = GetComponentInChildren<TextMesh>(true);
        if (!label) return;
        bool locked = resources.Progression != null && !resources.Progression.CanBuyKit(kit);
        string name = kit == FarmResource.TankKit ? "TANK UPGRADE" : "PICKAXE UPGRADE";
        label.text = locked ? "LOCKED\n" + (kit == FarmResource.TankKit ? "Irrigate a nearby plot" : "Plant + water Moon Herb") + "\nGRIP for help"
            : name + "\n" + stoneCost + " stone / " + coinCost + " coins";
    }

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
        else if (kit == FarmResource.TankKit)
            resources.Progression?.Teach("Take the tank upgrade carrier and touch your sprinkler to increase capacity to 200.", () => resources.GetAmount(FarmResource.TankKit) == 0);
        feedback.Success("Kit bought - take its carrier below");
        return true;
    }
}
