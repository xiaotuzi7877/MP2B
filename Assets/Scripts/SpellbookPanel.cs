using UnityEngine;

namespace ArcaneWorkshop
{
    // The floating spellbook beside the player: resources on the left page, building and recipe
    // runes on the right page. It is the in-world replacement for the desktop side panel.
    public sealed class SpellbookPanel : MonoBehaviour
    {
        private static readonly BuildingKind[] BuildOrder =
        {
            BuildingKind.RuneFoundation, BuildingKind.MoonPlanter, BuildingKind.ManaStill,
            BuildingKind.StarCauldron, BuildingKind.CrystalAmplifier
        };

        private ArcaneGameManager manager;
        private TextMesh resourceText;
        private TextMesh feedbackText;
        private TextMesh goalText;
        private TextMesh trackingStatusText;
        private WorldButton[] buildButtons;
        private WorldButton[] recipeButtons;
        private WorldButton ritualButton;
        private GameObject[] workshopHeaders;
        private TextMesh guideTitle;
        private TextMesh guideText;
        private float refreshTimer;

        public static SpellbookPanel Create(ArcaneGameManager manager, Transform anchor, Material pageMaterial, Material buttonMaterial)
        {
            GameObject root = new GameObject("Spellbook Panel");
            root.transform.SetParent(anchor, false);
            SpellbookPanel panel = root.AddComponent<SpellbookPanel>();
            panel.manager = manager;
            panel.Build(pageMaterial, buttonMaterial);
            return panel;
        }

        private void Build(Material pageMaterial, Material buttonMaterial)
        {
            // Two open pages, 4 x 2.6 units in total.
            CreatePage(new Vector3(-1.02f, 0f, 0.03f), pageMaterial);
            CreatePage(new Vector3(1.02f, 0f, 0.03f), pageMaterial);

            AddText("MP2B: ARCANE WORKSHOP", new Vector3(-1.95f, 1.1f, 0f), 0.03f, new Color(0.55f, 0.95f, 1f));
            resourceText = AddText("", new Vector3(-1.95f, 0.78f, 0f), 0.022f, Color.white);
            goalText = AddText("", new Vector3(-1.95f, -0.2f, 0f), 0.019f, new Color(1f, 0.9f, 0.6f));
            feedbackText = AddText("", new Vector3(-1.95f, -0.66f, 0f), 0.017f, new Color(0.8f, 0.85f, 1f));
            trackingStatusText = AddText("", new Vector3(-1.95f, -1.2f, 0f), 0.015f, new Color(1f, 0.72f, 0.3f));
            ritualButton = WorldButton.Create(transform, manager.FinalRitualLabel(),
                new Vector3(-1.02f, -1.02f, 0f), new Vector2(1.84f, 0.18f), 0.015f, buttonMaterial,
                manager.TryPerformFinalRitual);

            TextMesh buildHeader = AddText("BUILD  (point + trigger)", new Vector3(0.1f, 1.14f, 0f), 0.022f, new Color(0.92f, 0.8f, 1f));
            buildButtons = new WorldButton[BuildOrder.Length];
            for (int i = 0; i < BuildOrder.Length; i++)
            {
                BuildingKind kind = BuildOrder[i];
                buildButtons[i] = WorldButton.Create(transform, ArcaneGameManager.DisplayName(kind) + "  -  " + ArcaneGameManager.CostLabel(kind),
                    new Vector3(1.02f, 0.88f - i * 0.27f, 0f), new Vector2(1.84f, 0.22f), 0.02f, buttonMaterial,
                    () => manager.SelectBuilding(kind));
            }

            TextMesh recipeHeader = AddText("RECIPE", new Vector3(0.1f, -0.45f, 0f), 0.022f, new Color(0.92f, 0.8f, 1f));
            BrewRecipe[] recipes = manager.Recipes;
            recipeButtons = new WorldButton[recipes.Length];
            for (int i = 0; i < recipes.Length; i++)
            {
                int index = i;
                recipeButtons[i] = WorldButton.Create(transform, recipes[i].name + "  ->  " + recipes[i].goldReward + "g\n" + recipes[i].CostLabel(),
                    new Vector3(1.02f, -0.64f - i * 0.2f, 0f), new Vector2(1.84f, 0.17f), 0.014f, buttonMaterial,
                    () => manager.SelectRecipe(index));
            }

            // On a teammate island the right page becomes that island's step-by-step guide.
            workshopHeaders = new[] { buildHeader.gameObject, recipeHeader.gameObject };
            guideTitle = AddText("", new Vector3(0.1f, 1.14f, 0f), 0.022f, new Color(0.92f, 0.8f, 1f));
            guideText = AddText("", new Vector3(0.1f, 0.96f, 0f), 0.018f, Color.white);
        }

        public void SetTrackingFallback(bool active)
        {
            if (trackingStatusText != null)
            {
                trackingStatusText.text = active ? "LEFT HAND LOST - MENU PINNED TO VIEW" : "";
            }
        }

        private void CreatePage(Vector3 localPosition, Material material)
        {
            GameObject page = GameObject.CreatePrimitive(PrimitiveType.Cube);
            page.name = "Spellbook Page";
            Destroy(page.GetComponent<Collider>());
            page.transform.SetParent(transform, false);
            page.transform.localPosition = localPosition;
            page.transform.localScale = new Vector3(2f, 2.6f, 0.04f);
            page.GetComponent<Renderer>().sharedMaterial = material;
        }

        private TextMesh AddText(string text, Vector3 localPosition, float size, Color color)
        {
            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(transform, false);
            textObject.transform.localPosition = localPosition + new Vector3(0f, 0f, -0.03f);
            TextMesh mesh = textObject.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.UpperLeft;
            mesh.fontSize = 48;
            mesh.characterSize = size;
            mesh.color = color;
            WorldText.Style(mesh);
            return mesh;
        }

        private void Update()
        {
            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f)
            {
                return;
            }
            refreshTimer = 0.15f;

            resourceText.text = manager.ResourceSummary();
            goalText.text = manager.GoalSummary();
            feedbackText.text = ArcaneGameManager.Wrap(manager.Feedback, 46);
            for (int i = 0; i < buildButtons.Length; i++)
            {
                bool unlocked = manager.IsBuildingUnlocked(BuildOrder[i]);
                buildButtons[i].SetInteractable(unlocked);
                buildButtons[i].SetSelected(unlocked && manager.Placing && manager.SelectedBuilding == BuildOrder[i]);
                string suffix = unlocked ? ArcaneGameManager.CostLabel(BuildOrder[i]) : "LOCKED";
                buildButtons[i].SetText(ArcaneGameManager.DisplayName(BuildOrder[i]) + "  -  " + suffix);
            }
            for (int i = 0; i < recipeButtons.Length; i++)
            {
                bool unlocked = manager.IsRecipeUnlocked(i);
                recipeButtons[i].SetInteractable(unlocked);
                recipeButtons[i].SetSelected(unlocked && manager.SelectedRecipe == i);
                string details = unlocked ? manager.Recipes[i].CostLabel() : "LOCKED - TEAM OUTPUTS";
                recipeButtons[i].SetText(manager.Recipes[i].name + "  ->  " + manager.Recipes[i].goldReward + "g\n" + details);
            }
            ritualButton.SetText(manager.FinalRitualLabel());
            ritualButton.SetInteractable(manager.FinalRitualReady);

            IIslandGuide guide = IslandGuides.At(transform.position);
            bool guiding = guide != null;
            if (workshopHeaders[0].activeSelf == guiding)
            {
                foreach (GameObject header in workshopHeaders)
                {
                    header.SetActive(!guiding);
                }
                foreach (WorldButton button in buildButtons)
                {
                    button.SetVisible(!guiding);
                }
                foreach (WorldButton button in recipeButtons)
                {
                    button.SetVisible(!guiding);
                }
            }
            guideTitle.text = guiding ? guide.GuideTitle : "";
            guideText.text = guiding ? guide.GuideText() : "";
        }
    }
}
