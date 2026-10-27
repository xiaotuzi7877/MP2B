using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class TradeButton : MonoBehaviour
{
    [SerializeField] private FarmResources resources;
    [SerializeField] private XRSimpleInteractable interactable;
    [SerializeField] private FarmResource cost;
    [SerializeField, Min(1)] private int costAmount = 1;
    [SerializeField] private FarmResource reward;
    [SerializeField, Min(1)] private int rewardAmount = 1;
    [SerializeField] private TextMesh statusText;

    private void OnEnable() => interactable.selectEntered.AddListener(Trade);
    private void OnDisable() => interactable.selectEntered.RemoveListener(Trade);

    private void Trade(SelectEnterEventArgs args)
    {
        statusText.text = resources.TryTrade(cost, costAmount, reward, rewardAmount)
            ? $"-{costAmount} {cost} / +{rewardAmount} {reward}"
            : $"Not enough {cost}";
    }
}
