using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ArcaneWorkshop.Editor
{
    public static class MichaelProgressionValidator
    {
        private static int checksRun;

        [MenuItem("MP2B/Validate Michael Progression")]
        public static void Validate()
        {
            checksRun = 0;
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

            RunManagerContractChecks(failures);

            if (failures.Count == 0)
            {
                Debug.Log("MP2B Michael validation PASS: " + checksRun + "/" + checksRun +
                    " progression, resource, recipe, ritual, identity, and scene checks passed.");
                return;
            }

            for (int i = 0; i < failures.Count; i++)
            {
                Debug.LogError("MP2B Michael validation FAIL: " + failures[i]);
            }
            Debug.LogError("MP2B Michael validation finished with " + failures.Count + " failure(s) across " +
                checksRun + " checks.");
        }

        private static void RunManagerContractChecks(List<string> failures)
        {
            GameObject host = null;
            try
            {
                host = new GameObject("MP2B Michael Validation Host");
                host.hideFlags = HideFlags.HideAndDontSave;
                ArcaneGameManager manager = host.AddComponent<ArcaneGameManager>();

                FieldInfo progressionField = typeof(ArcaneGameManager).GetField(
                    "progression", BindingFlags.Instance | BindingFlags.NonPublic);
                Check(progressionField != null, "ArcaneGameManager progression field is missing.", failures);
                if (progressionField == null)
                {
                    return;
                }

                WorkshopProgression progression = progressionField.GetValue(manager) as WorkshopProgression;
                if (progression == null)
                {
                    MethodInfo awake = typeof(ArcaneGameManager).GetMethod(
                        "Awake", BindingFlags.Instance | BindingFlags.NonPublic);
                    Check(awake != null, "ArcaneGameManager.Awake could not be found for validation.", failures);
                    if (awake == null)
                    {
                        return;
                    }
                    awake.Invoke(manager, null);
                    progression = progressionField.GetValue(manager) as WorkshopProgression;
                }

                Check(progression != null, "ArcaneGameManager did not initialize progression.", failures);
                if (progression == null)
                {
                    return;
                }

                int resourceEvents = 0;
                manager.ResourceChanged += (kind, oldValue, newValue) => resourceEvents++;

                int waterBefore = manager.GetResource(WorkshopResourceKind.Water);
                bool partialSpend = manager.TrySpendResources(
                    new WorkshopResourceAmount(WorkshopResourceKind.Water, 4),
                    new WorkshopResourceAmount(WorkshopResourceKind.Water, 2));
                Check(!partialSpend, "Combined duplicate costs should fail when their total exceeds inventory.", failures);
                Check(manager.GetResource(WorkshopResourceKind.Water) == waterBefore,
                    "A failed multi-resource spend must not change inventory.", failures);

                const int convergenceRecipeIndex = 3;
                Check(!manager.IsRecipeUnlocked(convergenceRecipeIndex),
                    "Convergence Elixir should begin locked.", failures);
                Check(manager.RegisterTeammateOutput(WorkshopResourceKind.ConservatoryBloom, 1, "Justin"),
                    "Justin's Conservatory Bloom delivery should be accepted.", failures);
                Check(manager.GetResource(WorkshopResourceKind.ConservatoryBloom) == 1,
                    "The shared inventory did not record Justin's Bloom.", failures);
                Check(!manager.IsRecipeUnlocked(convergenceRecipeIndex),
                    "One teammate output must not unlock Convergence Alchemy by itself.", failures);
                Check(manager.RegisterTeammateOutput(WorkshopResourceKind.AstralCore, 1, "Lucas"),
                    "Lucas's Astral Core delivery should be accepted.", failures);
                Check(manager.GetResource(WorkshopResourceKind.AstralCore) == 1,
                    "The shared inventory did not record Lucas's Astral Core.", failures);
                Check(manager.IsRecipeUnlocked(convergenceRecipeIndex),
                    "Both teammate outputs should unlock Convergence Alchemy.", failures);
                Check(resourceEvents == 2,
                    "Each teammate delivery should raise one ResourceChanged event.", failures);

                int herbsBefore = manager.GetResource(WorkshopResourceKind.MoonHerb);
                int crystalsBefore = manager.GetResource(WorkshopResourceKind.Crystal);
                int essenceBefore = manager.GetResource(WorkshopResourceKind.Essence);
                Check(manager.TryConsumeRecipe(convergenceRecipeIndex),
                    "Convergence Elixir should consume its complete recipe atomically.", failures);
                Check(manager.GetResource(WorkshopResourceKind.MoonHerb) == herbsBefore - 1 &&
                      manager.GetResource(WorkshopResourceKind.Crystal) == crystalsBefore - 1 &&
                      manager.GetResource(WorkshopResourceKind.Essence) == essenceBefore - 1,
                    "Convergence Elixir did not consume its basic ingredients correctly.", failures);
                Check(manager.GetResource(WorkshopResourceKind.ConservatoryBloom) == 0 &&
                      manager.GetResource(WorkshopResourceKind.AstralCore) == 0,
                    "Convergence Elixir did not consume both teammate outputs.", failures);

                Check(!manager.FinalRitualReady, "The Final Ritual should begin locked.", failures);
                Check(progression.Unlock(WorkshopUnlock.FinalRitual),
                    "Completing the campaign should unlock the Final Ritual.", failures);
                Check(manager.FinalRitualReady && manager.FinalRitualLabel().Contains("BEGIN FINAL RITUAL"),
                    "The unlocked Final Ritual should be exposed as a deliberate player action.", failures);
            }
            catch (Exception exception)
            {
                failures.Add("Manager contract validation threw: " + exception.GetBaseException().Message);
            }
            finally
            {
                if (host != null)
                {
                    UnityEngine.Object.DestroyImmediate(host);
                }
            }
        }

        private static void Check(bool condition, string message, List<string> failures)
        {
            checksRun++;
            if (!condition)
            {
                failures.Add(message);
            }
        }
    }
}
