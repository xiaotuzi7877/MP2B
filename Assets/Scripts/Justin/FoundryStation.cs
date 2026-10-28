using UnityEngine;

namespace ArcaneWorkshop
{
    public enum FoundryStationKind
    {
        StarCatcher,
        RuneWell,
        AstralForge
    }

    /// <summary>
    /// Author: Michael (Ziyang Li), implementing Justin's part of the team design.
    /// One clickable machine on Justin's Astral Foundry island. The mouse or VR ray press (trigger / grip) is routed
    /// here by ArcaneGameManager.ProcessPointer, exactly like the bridge gates and supply receivers on Michael Island.
    /// </summary>
    public sealed class FoundryStation : MonoBehaviour
    {
        [SerializeField] private FoundryStationKind kind;
        [SerializeField] private AstralFoundry foundry;
        [SerializeField] private TextMesh label;

        public FoundryStationKind Kind { get { return kind; } }
        public TextMesh Label { get { return label; } }

        public void Configure(FoundryStationKind stationKind, AstralFoundry owner, TextMesh statusLabel)
        {
            kind = stationKind;
            foundry = owner;
            label = statusLabel;
        }

        public void Interact(ArcaneGameManager manager)
        {
            if (foundry == null)
            {
                foundry = GetComponentInParent<AstralFoundry>();
            }
            if (foundry != null)
            {
                foundry.Use(kind, manager);
            }
        }
    }
}
