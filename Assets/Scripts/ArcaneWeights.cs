using UnityEngine;

namespace ArcaneWorkshop
{
    // The workshop is a 1:20 miniature. Each interactable is given the mass of the full-size object
    // it represents, then scaled for the miniature: same material, every dimension divided by 20,
    // so volume and therefore mass are divided by 20^3 = 8000.
    public static class ArcaneWeights
    {
        public const float ModelScale = 20f;

        public static float RealMassKg(BuildingKind kind)
        {
            switch (kind)
            {
                case BuildingKind.RuneFoundation: return 380f;   // carved stone slab, 1.1 x 1.1 x 0.12 m
                case BuildingKind.MoonPlanter: return 140f;      // wooden planter box with wet soil
                case BuildingKind.ManaStill: return 85f;         // copper still with a glass vessel
                case BuildingKind.StarCauldron: return 260f;     // cast-iron cauldron with ~100 L of brew
                default: return 120f;                            // Crystal Amplifier: quartz on a stone base
            }
        }

        public static float RealMassKg(NodeKind kind)
        {
            switch (kind)
            {
                case NodeKind.MoonHerb: return 0.15f;            // a single herb plant
                case NodeKind.CrystalCluster: return 45f;        // quartz cluster
                default: return 3f;                              // Crystal Shard
            }
        }

        public static float RealMassKg(ProjectileKind kind)
        {
            switch (kind)
            {
                case ProjectileKind.Ingredient: return 0.2f;     // a handful of ingredients
                case ProjectileKind.WaterDrop: return 0.05f;     // a splash of water
                case ProjectileKind.CrystalSpark: return 1f;     // crystal fragment
                case ProjectileKind.FallingStar: return 8f;      // small meteorite
                default: return 0.6f;                            // filled potion bottle
            }
        }

        public const float MerchantGateRealMassKg = 2400f;      // two stone pillars and a gate step

        public static float Miniature(float realKg)
        {
            return realKg / (ModelScale * ModelScale * ModelScale);
        }

        // Interactables move by script, so their bodies are kinematic; the mass is ready for physics
        // interactions such as VR grabbing and throwing.
        public static Rigidbody Apply(GameObject target, float realKg)
        {
            Rigidbody body = target.GetComponent<Rigidbody>();
            if (body == null)
            {
                body = target.AddComponent<Rigidbody>();
            }
            body.mass = Miniature(realKg);
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            return body;
        }
    }
}
