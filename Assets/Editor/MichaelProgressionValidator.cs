using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ArcaneWorkshop.Editor
{
    public static class MichaelProgressionValidator
    {
        [MenuItem("MP2B/Validate Michael Progression")]
        public static void Validate()
        {
            List<string> failures = new List<string>();
            WorkshopProgression progression = new WorkshopProgression();

            Check(progression.IsUnlocked(WorkshopUnlock.RuneFoundation), "Rune Foundation should be unlocked initially.", failures);
            Check(progression.IsUnlocked(WorkshopUnlock.BasicAlchemy), "Basic Alchemy should be unlocked initially.", failures);
            Check(!progression.IsUnlocked(WorkshopUnlock.MoonPlanter), "Moon Planter should be locked initially.", failures);
            Check(progression.Unlock(WorkshopUnlock.MoonPlanter), "First Moon Planter unlock should change state.", failures);
            Check(!progression.Unlock(WorkshopUnlock.MoonPlanter), "Duplicate unlock should be idempotent.", failures);

            BrewRecipe convergence = new BrewRecipe(
                "Convergence Elixir", 1, 1, 0, 1, 1, 1, 30, WorkshopUnlock.AdvancedAlchemy);
            Check(convergence.requiredUnlock == WorkshopUnlock.AdvancedAlchemy, "Advanced recipe unlock gate is missing.", failures);
            Check(convergence.CostLabel().Contains("bloom") && convergence.CostLabel().Contains("astral core"),
                "Advanced recipe must expose both teammate costs.", failures);

            Check(PlayerSettings.productName.StartsWith("MP2B"), "Player Settings product name must begin with MP2B.", failures);
            Check(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) == "com.cs417.arcaneworkshop.mp2b",
                "Android application identifier is not the MP2B identifier.", failures);
            Check(AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/ArcaneWorkshop.unity") != null,
                "ArcaneWorkshop scene is missing.", failures);

            if (failures.Count == 0)
            {
                Debug.Log("MP2B Michael validation PASS: 10/10 progression, recipe, identity, and scene checks passed.");
                return;
            }

            for (int i = 0; i < failures.Count; i++)
            {
                Debug.LogError("MP2B Michael validation FAIL: " + failures[i]);
            }
            Debug.LogError("MP2B Michael validation finished with " + failures.Count + " failure(s).");
        }

        private static void Check(bool condition, string message, List<string> failures)
        {
            if (!condition)
            {
                failures.Add(message);
            }
        }
    }
}
