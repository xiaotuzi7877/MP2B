using System;
using System.Collections.Generic;

namespace ArcaneWorkshop
{
    public enum WorkshopUnlock
    {
        None,
        RuneFoundation,
        MoonPlanter,
        ManaStill,
        StarCauldron,
        CrystalAmplifier,
        BasicAlchemy,
        AdvancedAlchemy,
        FinalRitual
    }

    public enum WorkshopResourceKind
    {
        MoonHerb,
        Crystal,
        Water,
        Essence,
        Gold,
        ConservatoryBloom,
        AstralCore
    }

    [Serializable]
    public struct WorkshopResourceAmount
    {
        public WorkshopResourceKind kind;
        public int amount;

        public WorkshopResourceAmount(WorkshopResourceKind kind, int amount)
        {
            this.kind = kind;
            this.amount = amount;
        }
    }

    /// <summary>
    /// Small, engine-independent unlock model owned by the integration layer.
    /// Teammate features query this contract instead of depending on goal indexes.
    /// </summary>
    public sealed class WorkshopProgression
    {
        private readonly HashSet<WorkshopUnlock> unlocked = new HashSet<WorkshopUnlock>();

        public event Action<WorkshopUnlock> FeatureUnlocked;

        public WorkshopProgression()
        {
            unlocked.Add(WorkshopUnlock.RuneFoundation);
            unlocked.Add(WorkshopUnlock.BasicAlchemy);
        }

        public bool IsUnlocked(WorkshopUnlock feature)
        {
            return feature == WorkshopUnlock.None || unlocked.Contains(feature);
        }

        public bool Unlock(WorkshopUnlock feature)
        {
            if (feature == WorkshopUnlock.None || !unlocked.Add(feature))
            {
                return false;
            }

            FeatureUnlocked?.Invoke(feature);
            return true;
        }

        public static string DisplayName(WorkshopUnlock feature)
        {
            switch (feature)
            {
                case WorkshopUnlock.RuneFoundation: return "Rune Foundations";
                case WorkshopUnlock.MoonPlanter: return "Moon Planters";
                case WorkshopUnlock.ManaStill: return "Mana Stills";
                case WorkshopUnlock.StarCauldron: return "Star Cauldrons";
                case WorkshopUnlock.CrystalAmplifier: return "Crystal Amplifiers";
                case WorkshopUnlock.BasicAlchemy: return "Basic Alchemy";
                case WorkshopUnlock.AdvancedAlchemy: return "Convergence Alchemy";
                case WorkshopUnlock.FinalRitual: return "Final Restoration Ritual";
                default: return feature.ToString();
            }
        }

        public static string ResourceDisplayName(WorkshopResourceKind kind)
        {
            switch (kind)
            {
                case WorkshopResourceKind.MoonHerb: return "Moon Herb";
                case WorkshopResourceKind.Crystal: return "Crystal";
                case WorkshopResourceKind.Water: return "Water";
                case WorkshopResourceKind.Essence: return "Essence";
                case WorkshopResourceKind.Gold: return "Gold";
                case WorkshopResourceKind.ConservatoryBloom: return "Conservatory Bloom";
                case WorkshopResourceKind.AstralCore: return "Astral Core";
                default: return kind.ToString();
            }
        }
    }
}
