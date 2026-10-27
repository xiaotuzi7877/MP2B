using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class PickaxeTool : MonoBehaviour
{
    [SerializeField] private FarmResources resources;
    [SerializeField] private XRGrabInteractable grabInteractable;
    [SerializeField] private GameObject basicVisual;
    [SerializeField] private GameObject upgradedVisual;
    [SerializeField] private ActionFeedback feedback;
    [SerializeField] private AudioSource impactSource;
    [SerializeField] private AudioClip impactSound;
    private float nextHit;
    public bool IsUpgraded { get; private set; }

    public bool Strike(Boulder boulder)
    {
        if (!grabInteractable.isSelected || Time.time < nextHit)
            return false;
        if (MiningLocked()) { nextHit = Time.time + .75f; return false; }
        if (!boulder.Hit(IsUpgraded ? 1.5f : 1f))
            return false;
        nextHit = Time.time + .25f;
        impactSource.pitch = IsUpgraded ? .75f : 1f;
        impactSource.PlayOneShot(impactSound);
        return true;
    }

    public bool Upgrade()
    {
        if (MiningLocked()) return false;
        if (IsUpgraded) { feedback.Failure("Pickaxe already upgraded"); return false; }
        if (!resources.TrySpend(FarmResource.PickaxeKit, 1))
        { feedback.Failure("Buy a pickaxe kit first"); return false; }
        IsUpgraded = true;
        basicVisual.SetActive(false);
        upgradedVisual.SetActive(true);
        feedback.Success("Pickaxe upgraded: 3 hits -> 2");
        return true;
    }

    private bool MiningLocked()
    {
        if (resources.Progression == null || resources.Progression.MiningUnlocked) return false;
        resources.Progression.ClearGuide();
        feedback.Failure("Mining locked. Plant and water a Moon Herb first. Nothing spent.\nNext: " + resources.Progression.CurrentGoal);
        return true;
    }
}
