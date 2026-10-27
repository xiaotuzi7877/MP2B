using System;
using UnityEngine;

namespace ArcaneWorkshop
{
    public enum BuildingKind
    {
        RuneFoundation,
        MoonPlanter,
        ManaStill,
        StarCauldron,
        CrystalAmplifier
    }

    public enum NodeKind
    {
        MoonHerb,
        CrystalCluster,
        CrystalShard
    }

    public enum ProjectileKind
    {
        Ingredient,
        WaterDrop,
        CrystalSpark,
        FallingStar,
        Potion
    }

    [Serializable]
    public struct BuildingPrefabEntry
    {
        public BuildingKind kind;
        public ArcaneBuilding prefab;
    }

    [Serializable]
    public struct BrewRecipe
    {
        public string name;
        public int herbs;
        public int crystals;
        public int water;
        public int essence;
        public int conservatoryBlooms;
        public int astralCores;
        public int goldReward;
        public WorkshopUnlock requiredUnlock;

        public BrewRecipe(string name, int herbs, int crystals, int water, int essence, int goldReward)
        {
            this.name = name;
            this.herbs = herbs;
            this.crystals = crystals;
            this.water = water;
            this.essence = essence;
            conservatoryBlooms = 0;
            astralCores = 0;
            this.goldReward = goldReward;
            requiredUnlock = WorkshopUnlock.BasicAlchemy;
        }

        public BrewRecipe(
            string name,
            int herbs,
            int crystals,
            int water,
            int essence,
            int conservatoryBlooms,
            int astralCores,
            int goldReward,
            WorkshopUnlock requiredUnlock)
        {
            this.name = name;
            this.herbs = herbs;
            this.crystals = crystals;
            this.water = water;
            this.essence = essence;
            this.conservatoryBlooms = conservatoryBlooms;
            this.astralCores = astralCores;
            this.goldReward = goldReward;
            this.requiredUnlock = requiredUnlock;
        }

        public string CostLabel()
        {
            System.Collections.Generic.List<string> parts = new System.Collections.Generic.List<string>();
            if (herbs > 0) parts.Add(herbs + " herb");
            if (crystals > 0) parts.Add(crystals + " crystal");
            if (water > 0) parts.Add(water + " water");
            if (essence > 0) parts.Add(essence + " essence");
            if (conservatoryBlooms > 0) parts.Add(conservatoryBlooms + " bloom");
            if (astralCores > 0) parts.Add(astralCores + " astral core");
            return string.Join(", ", parts.ToArray());
        }
    }
}
