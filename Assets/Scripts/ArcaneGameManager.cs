using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace ArcaneWorkshop
{
    public sealed class ArcaneGameManager : MonoBehaviour
    {
        [Header("Generated Prefabs")]
        public BuildingPrefabEntry[] buildingPrefabs;
        public GatherableNode moonHerbPrefab;
        public GatherableNode crystalClusterPrefab;
        public GatherableNode crystalShardPrefab;
        public MerchantPortal merchantPortal;
        public GameObject potionBottleVisual;
        public Font worldFont;
        public Material worldTextMaterial;
        public Material uiPanelMaterial;

        [Header("Starting Player Resources")]
        [SerializeField] private int herbs = 8;
        [SerializeField] private int crystals = 6;
        [SerializeField] private int water = 5;
        [SerializeField] private int essence = 2;
        [SerializeField] private int potions;
        [SerializeField] private int gold = 35;

        [Header("Grid")]
        [SerializeField] private int gridSize = 7;
        [SerializeField] private float cellSize = 1.28f;

        private readonly BrewRecipe[] recipes =
        {
            new BrewRecipe("Moonlight Draught", 2, 0, 1, 0, 7),
            new BrewRecipe("Starlight Tonic", 1, 2, 0, 0, 11),
            new BrewRecipe("Verdant Elixir", 3, 0, 1, 1, 15)
        };

        private readonly HashSet<string> shownTutorials = new HashSet<string>();
        private readonly Queue<KeyValuePair<string, Vector3>> tutorialQueue = new Queue<KeyValuePair<string, Vector3>>();
        private readonly Queue<int> potionStock = new Queue<int>();

        // Restoration goals: a short campaign that ends with the workshop restored.
        private struct Goal
        {
            public string title;
            public string hint;
            public System.Func<bool> isDone;
        }

        private Goal[] goals;
        private int goalIndex;
        private float goalTimer;
        private bool workshopRestored;
        private bool anyLinkEver;
        private int potionsBrewed;
        private int potionsSold;
        private int lifetimeSales;
        private float sessionStart;
        private float starTimer = 55f;
        private Material waterMaterial;
        private Material sparkMaterial;
        private Material starMaterial;
        private Material trailMaterial;
        private GridCell[,] cells;
        private Transform gridRoot;
        private BuildingKind selectedBuilding = BuildingKind.RuneFoundation;
        private bool placing = true;
        private GridCell hoveredCell;
        private Renderer[,] placementMarkers;
        private Material markerValid;
        private Material markerHover;
        private Material markerInvalid;
        private Material linkMaterial;
        private Transform linkRoot;
        private readonly Dictionary<int, ManaLink> manaLinks = new Dictionary<int, ManaLink>();
        private readonly HashSet<int> activeLinks = new HashSet<int>();
        private int selectedRecipe;
        private float neighborTimer;
        private float wildSpawnTimer = 16f;
        // Crystals gate most buildings, so they get their own faster spawn on top of the mixed one.
        private float crystalSpawnTimer = 10f;
        private const int MaxWildCrystalClusters = 6;
        private float feedbackUntil;
        private string feedback = "Welcome to the Arcane Workshop.";
        private string activeTutorial;
        private Vector3 tutorialWorldPosition;
        private float tutorialUntil;
        private Material gridMaterialA;
        private Material gridMaterialB;
        private Material moteMaterial;
        private GUIStyle titleStyle;
        private GUIStyle smallStyle;
        private GUIStyle resourceStyle;

        public int SelectedRecipe { get { return selectedRecipe; } }
        public BuildingKind SelectedBuilding { get { return selectedBuilding; } }
        public bool Placing { get { return placing; } }
        public BrewRecipe[] Recipes { get { return recipes; } }
        public string Feedback { get { return feedback; } }

        // Set by the VR rig: when true, the desktop mouse no longer drives the pointer.
        public bool ExternalPointerActive { get; set; }
        private TutorialBubble tutorialBubble;
        private SpellbookPanel spellbook;
        private VRRig vrRig;
        private Transform spellbookMount;
        private bool spellbookUsesHeadFallback;
        private bool spellbookCatchingUp;
        private Material uiPageMaterial;
        private Material uiButtonMaterial;

        private readonly List<Bounds> spawnBlockers = new List<Bounds>();
        private static readonly List<XRDisplaySubsystem> XrDisplays = new List<XRDisplaySubsystem>();
        private float nextVrStartupCheck;
        private string xrStatus;
        private const string IntroMessage = "Welcome, apprentice. Select Rune Foundation and click a grid cell. Every machine must be built on a foundation.";

        private void Awake()
        {
            WorldText.Font = worldFont;
            WorldText.Material = worldTextMaterial;
        }

        private void Start()
        {
            CreateRuntimeMaterials();
            CollectSpawnBlockers();
            BuildGrid();
            SpawnInitialWildInstances();
            if (merchantPortal != null)
            {
                merchantPortal.Initialize(this);
            }
            tutorialBubble = TutorialBubble.Create(uiPageMaterial);
            CreateGoals();
            TryStartVRWhenReady();
            ShowTutorial("intro", IntroMessage, Vector3.zero);
        }

        private void Update()
        {
            TryStartVRWhenReady();
            UpdateVRSpellbookAttachment();
            HandleHotkeys();
            if (!ExternalPointerActive && Camera.main != null && DesktopInput.HasMouse)
            {
                Vector2 mouse = DesktopInput.MousePosition;
                bool overPanel = mouse.x < 320f;
                Ray mouseRay = Camera.main.ScreenPointToRay(mouse);
                if (!overPanel)
                {
                    ProcessPointer(mouseRay, DesktopInput.LeftPressed, DesktopInput.RightPressed, DesktopInput.ShiftHeld);
                }
                else
                {
                    hoveredCell = null;
                }
            }
            UpdatePlacementMarkers();

            neighborTimer -= Time.deltaTime;
            if (neighborTimer <= 0f)
            {
                neighborTimer = 0.25f;
                TickNeighborInteractions(0.25f);
            }

            wildSpawnTimer -= Time.deltaTime;
            if (wildSpawnTimer <= 0f)
            {
                wildSpawnTimer = Random.Range(16f, 24f);
                GatherableNode spawned = SpawnWildInstance(Random.value < 0.72f ? NodeKind.MoonHerb : NodeKind.CrystalCluster);
                SetFeedback("A new wild material has appeared around the workshop.");
                if (spawned != null)
                {
                    ShowTutorial("wild-spawn", "New wild materials sprout around the island at random times. Keep an eye on the edges.", spawned.transform.position);
                }
            }

            crystalSpawnTimer -= Time.deltaTime;
            if (crystalSpawnTimer <= 0f)
            {
                crystalSpawnTimer = Random.Range(10f, 15f);
                if (CountWildNodes(NodeKind.CrystalCluster) < MaxWildCrystalClusters)
                {
                    SpawnWildInstance(NodeKind.CrystalCluster);
                }
            }

            goalTimer -= Time.deltaTime;
            if (goalTimer <= 0f)
            {
                goalTimer = 0.5f;
                CheckGoals();
            }

            starTimer -= Time.deltaTime;
            if (starTimer <= 0f)
            {
                starTimer = Random.Range(45f, 75f);
                SpawnFallingStar();
            }

            if (Time.unscaledTime >= tutorialUntil)
            {
                if (tutorialQueue.Count > 0)
                {
                    KeyValuePair<string, Vector3> next = tutorialQueue.Dequeue();
                    DisplayTutorial(next.Key, next.Value);
                }
                else if (tutorialBubble != null && tutorialBubble.gameObject.activeSelf)
                {
                    tutorialBubble.Hide();
                }
            }
        }

        private void CreateRuntimeMaterials()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            gridMaterialA = new Material(shader);
            gridMaterialB = new Material(shader);
            moteMaterial = new Material(shader);
            SetMaterialColor(gridMaterialA, new Color(0.13f, 0.12f, 0.22f));
            SetMaterialColor(gridMaterialB, new Color(0.19f, 0.16f, 0.29f));
            SetMaterialColor(moteMaterial, new Color(0.25f, 0.95f, 1f));
            waterMaterial = new Material(shader);
            SetMaterialColor(waterMaterial, new Color(0.35f, 0.7f, 1f));
            sparkMaterial = new Material(shader);
            SetMaterialColor(sparkMaterial, new Color(0.85f, 0.35f, 1f));
            starMaterial = new Material(shader);
            SetMaterialColor(starMaterial, new Color(1f, 0.92f, 0.55f));
            foreach (Material glowing in new[] { moteMaterial, waterMaterial, sparkMaterial, starMaterial })
            {
                glowing.EnableKeyword("_EMISSION");
                glowing.SetColor("_EmissionColor", glowing.color * 1.6f);
            }

            Shader spriteShader = Shader.Find("Sprites/Default");
            Shader glowShader = Shader.Find("ArcaneWorkshop/Scrolling Additive");
            linkMaterial = new Material(glowShader != null ? glowShader : spriteShader);
            if (linkMaterial.HasProperty("_Scroll"))
            {
                // ManaLink drives the offset itself so the flow direction follows the giving machine.
                linkMaterial.SetVector("_Scroll", Vector4.zero);
                linkMaterial.SetColor("_Color", Color.white);
            }
            trailMaterial = new Material(linkMaterial);
            uiPageMaterial = uiPanelMaterial != null ? new Material(uiPanelMaterial) : new Material(spriteShader);
            uiPageMaterial.color = new Color(0.05f, 0.04f, 0.1f, 0.92f);
            uiButtonMaterial = uiPanelMaterial != null ? new Material(uiPanelMaterial) : new Material(spriteShader);
            // UI layers draw back-to-front by queue, not by distance: pages, then buttons, then text (3000).
            uiPageMaterial.renderQueue = 2900;
            uiButtonMaterial.renderQueue = 2950;
            markerValid = CreateMarkerMaterial(spriteShader, new Color(0.25f, 0.85f, 1f, 0.45f));
            markerHover = CreateMarkerMaterial(spriteShader, new Color(0.75f, 1f, 1f, 0.95f));
            markerInvalid = CreateMarkerMaterial(spriteShader, new Color(1f, 0.25f, 0.3f, 0.85f));
        }

        private static Material CreateMarkerMaterial(Shader shader, Color color)
        {
            Material material = new Material(shader);
            material.mainTexture = CreateMarkerTexture();
            material.color = color;
            return material;
        }

        private static Texture2D markerTexture;

        // A soft rune-frame: bright border, faint interior, so the slab beneath stays readable.
        private static Texture2D CreateMarkerTexture()
        {
            if (markerTexture != null)
            {
                return markerTexture;
            }

            const int size = 64;
            markerTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            markerTexture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float edge = Mathf.Min(Mathf.Min(x, size - 1 - x), Mathf.Min(y, size - 1 - y)) / (size * 0.5f);
                    float border = 1f - Mathf.SmoothStep(0.04f, 0.16f, edge);
                    float alpha = Mathf.Max(border, 0.18f);
                    markerTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            markerTexture.Apply();
            return markerTexture;
        }

        private void BuildGrid()
        {
            gridRoot = new GameObject("Arcane Placement Grid").transform;
            linkRoot = new GameObject("Mana Links").transform;
            cells = new GridCell[gridSize, gridSize];
            placementMarkers = new Renderer[gridSize, gridSize];
            float centerOffset = (gridSize - 1) * cellSize * 0.5f;

            for (int row = 0; row < gridSize; row++)
            {
                for (int column = 0; column < gridSize; column++)
                {
                    GameObject cellObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cellObject.transform.SetParent(gridRoot);
                    cellObject.transform.position = new Vector3(column * cellSize - centerOffset, -0.1f, row * cellSize - centerOffset);
                    cellObject.transform.localScale = new Vector3(cellSize - 0.06f, 0.12f, cellSize - 0.06f);
                    cellObject.GetComponent<Renderer>().sharedMaterial = (row + column) % 2 == 0 ? gridMaterialA : gridMaterialB;
                    GridCell cell = cellObject.AddComponent<GridCell>();
                    cell.Configure(row, column);
                    cells[row, column] = cell;

                    GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    marker.name = "Placement Marker";
                    Destroy(marker.GetComponent<Collider>());
                    marker.transform.SetParent(gridRoot, false);
                    marker.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    marker.transform.localScale = Vector3.one * (cellSize - 0.1f);
                    marker.transform.position = cellObject.transform.position;
                    Renderer markerRenderer = marker.GetComponent<Renderer>();
                    markerRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    markerRenderer.enabled = false;
                    placementMarkers[row, column] = markerRenderer;
                }
            }
        }

        private void HandleHotkeys()
        {
            BuildingKind[] order =
            {
                BuildingKind.RuneFoundation, BuildingKind.MoonPlanter, BuildingKind.ManaStill,
                BuildingKind.StarCauldron, BuildingKind.CrystalAmplifier
            };
            for (int i = 0; i < order.Length; i++)
            {
                if (DesktopInput.KeyPressed(UnityEngine.InputSystem.Key.Digit1 + i))
                {
                    SelectBuilding(order[i]);
                }
            }

            if (DesktopInput.KeyPressed(UnityEngine.InputSystem.Key.F1))
            {
                if (spellbook == null)
                {
                    ShowSpellbook();
                }
                else
                {
                    spellbook.gameObject.SetActive(!spellbook.gameObject.activeSelf);
                }
            }

            if (DesktopInput.KeyPressed(UnityEngine.InputSystem.Key.Escape))
            {
                CancelPlacement();
            }
        }

        public void SelectBuilding(BuildingKind kind)
        {
            if (placing && selectedBuilding == kind)
            {
                placing = false;
                SetFeedback("Placement cancelled.");
                return;
            }

            selectedBuilding = kind;
            placing = true;
            ArcaneAudio.Play2D(SoundId.UiSelect);
            SetFeedback("Placing " + DisplayName(kind) + ". Glowing frames show where it can go.");
        }

        private bool CanPlaceOn(GridCell cell)
        {
            if (selectedBuilding == BuildingKind.RuneFoundation)
            {
                return cell.foundation == null;
            }
            return cell.foundation != null && cell.building == null;
        }

        private void UpdatePlacementMarkers()
        {
            if (placementMarkers == null)
            {
                return;
            }

            float pulse = 0.8f + Mathf.Sin(Time.time * 4f) * 0.2f;
            for (int row = 0; row < gridSize; row++)
            {
                for (int column = 0; column < gridSize; column++)
                {
                    GridCell cell = cells[row, column];
                    Renderer marker = placementMarkers[row, column];
                    bool valid = placing && CanPlaceOn(cell);
                    bool hovered = placing && cell == hoveredCell;
                    marker.enabled = valid || hovered;
                    if (!marker.enabled)
                    {
                        continue;
                    }

                    marker.sharedMaterial = !valid ? markerInvalid : hovered ? markerHover : markerValid;
                    float height = cell.foundation != null ? 0.19f : -0.02f;
                    Vector3 position = cell.transform.position;
                    marker.transform.position = new Vector3(position.x, height, position.z);
                    marker.transform.localScale = Vector3.one * (cellSize - 0.1f) * (hovered ? 1f : pulse * 0.08f + 0.9f);
                }
            }
        }

        // One pointer pipeline for both the desktop mouse ray and the VR controller ray.
        // primary = left click / trigger (gather, build, press), secondary = right click / grip (use).
        public void ProcessPointer(Ray ray, bool primary, bool secondary, bool modifier)
        {
            hoveredCell = null;
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, 200f, ~0, QueryTriggerInteraction.Collide))
            {
                return;
            }

            WorldButton button = hit.collider.GetComponent<WorldButton>();
            if (button != null)
            {
                button.Hover();
                if (primary || secondary)
                {
                    button.Press();
                }
                return;
            }

            if (hit.collider.GetComponent<TutorialBubble>() != null)
            {
                if (primary || secondary)
                {
                    DismissTutorial();
                }
                return;
            }

            if (hit.collider.GetComponent<ArcaneProjectile>() != null)
            {
                return;
            }

            ArcaneBuilding building = hit.collider.GetComponentInParent<ArcaneBuilding>();
            GridCell cell = building != null ? building.Cell : hit.collider.GetComponentInParent<GridCell>();
            if (placing)
            {
                hoveredCell = cell;
            }

            if (!primary && !secondary)
            {
                return;
            }

            GatherableNode node = hit.collider.GetComponentInParent<GatherableNode>();
            if (node != null)
            {
                if (primary)
                {
                    node.Interact(this);
                }
                return;
            }

            MerchantPortal portal = hit.collider.GetComponentInParent<MerchantPortal>();
            if (portal != null)
            {
                if (secondary)
                {
                    ShipPotion();
                }
                else
                {
                    SetFeedback("Use (right-click / grip) the Merchant Gate to send a Potion along the delivery path.");
                }
                return;
            }

            if (building != null)
            {
                if (secondary && modifier)
                {
                    building.Repair();
                }
                else if (secondary)
                {
                    building.Interact();
                }
                else if (placing && building.Kind == BuildingKind.RuneFoundation && selectedBuilding != BuildingKind.RuneFoundation)
                {
                    TryBuild(building.Cell);
                }
                else
                {
                    SetFeedback("Use (right-click / grip) the " + DisplayName(building.Kind) + " to activate it.");
                }
                return;
            }

            if (cell != null && primary)
            {
                if (placing)
                {
                    TryBuild(cell);
                }
                else
                {
                    SetFeedback("Pick a building from the menu (1-5) to start placing.");
                }
            }
        }

        public void SelectRecipe(int index)
        {
            selectedRecipe = Mathf.Clamp(index, 0, recipes.Length - 1);
            ArcaneAudio.Play2D(SoundId.UiSelect);
            SetFeedback("Selected recipe: " + recipes[selectedRecipe].name + ". Use a Star Cauldron to brew.");
        }

        private void StartVR()
        {
            if (ExternalPointerActive || Camera.main == null)
            {
                return;
            }

            Material wand = new Material(Shader.Find("Standard"));
            SetMaterialColor(wand, new Color(0.35f, 0.22f, 0.12f));
            vrRig = VRRig.Create(this, Camera.main, trailMaterial, wand, moteMaterial);
            ExternalPointerActive = true;

            // The spellbook is held in the left hand, tilted toward the player's eyes.
            GameObject mount = new GameObject("Spellbook Mount");
            spellbookMount = mount.transform;
            mount.transform.localScale = Vector3.one * 0.085f;
            spellbook = SpellbookPanel.Create(this, mount.transform, uiPageMaterial, uiButtonMaterial);
            UpdateVRSpellbookAttachment(true);

            // Link often starts after the scene, so restart the intro's 10 seconds once the headset is live.
            if (CountBuildings(BuildingKind.RuneFoundation) == 0)
            {
                DisplayTutorial(IntroMessage, Vector3.zero);
            }
        }

        private void UpdateVRSpellbookAttachment(bool force = false)
        {
            if (vrRig == null || spellbookMount == null || spellbook == null)
            {
                return;
            }

            bool useHeadFallback = !vrRig.LeftHandTrackedRecently;
            if (!force && useHeadFallback == spellbookUsesHeadFallback)
            {
                if (useHeadFallback)
                {
                    FollowHeadLazily(false);
                }
                return;
            }

            spellbookUsesHeadFallback = useHeadFallback;
            if (useHeadFallback)
            {
                // Riding on the rig instead of the head: a menu glued to the face is a strong cause of
                // VR sickness, so it trails the head's yaw only and never tilts or rolls with it.
                spellbookMount.SetParent(vrRig.transform, true);
                spellbookMount.localScale = Vector3.one * 0.075f;
                FollowHeadLazily(true);
            }
            else
            {
                spellbookMount.SetParent(vrRig.LeftHand, false);
                spellbookMount.localPosition = new Vector3(0f, 0.11f, 0.05f);
                spellbookMount.localRotation = Quaternion.Euler(45f, 0f, 0f);
                spellbookMount.localScale = Vector3.one * 0.085f;
            }
            spellbook.SetTrackingFallback(useHeadFallback);
        }

        private void FollowHeadLazily(bool snap)
        {
            Transform head = vrRig.Head;
            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.ProjectOnPlane(vrRig.transform.forward, Vector3.up);
            }
            Quaternion yaw = Quaternion.LookRotation(forward.normalized, Vector3.up);
            // Lower-left of the view so the tutorial scroll and the grid stay visible.
            Vector3 targetPosition = head.position + yaw * new Vector3(-0.36f, -0.35f, 0.7f);
            Quaternion targetRotation = yaw * Quaternion.Euler(25f, -20f, 0f);

            // Hold still while the head only glances around; catch up once the menu drifts out of view.
            float angle = Quaternion.Angle(Quaternion.Euler(0f, spellbookMount.eulerAngles.y + 20f, 0f), yaw);
            if (snap || angle > 35f || Vector3.Distance(spellbookMount.position, targetPosition) > 0.6f)
            {
                spellbookCatchingUp = true;
            }
            if (!spellbookCatchingUp)
            {
                return;
            }

            float t = snap ? 1f : 1f - Mathf.Exp(-4f * Time.deltaTime);
            spellbookMount.SetPositionAndRotation(
                Vector3.Lerp(spellbookMount.position, targetPosition, t),
                Quaternion.Slerp(spellbookMount.rotation, targetRotation, t));
            if (Vector3.Distance(spellbookMount.position, targetPosition) < 0.02f)
            {
                spellbookCatchingUp = false;
            }
        }

        private void TryStartVRWhenReady()
        {
            if (ExternalPointerActive || Camera.main == null || Time.unscaledTime < nextVrStartupCheck)
            {
                return;
            }

            nextVrStartupCheck = Time.unscaledTime + 0.5f;
            XrDisplays.Clear();
            SubsystemManager.GetSubsystems(XrDisplays);
            for (int i = 0; i < XrDisplays.Count; i++)
            {
                if (XrDisplays[i] != null && XrDisplays[i].running)
                {
                    xrStatus = null;
                    StartVR();
                    return;
                }
            }

            if (xrStatus == null && Time.timeSinceLevelLoad > 5f)
            {
                xrStatus = DescribeXrFailure();
                Debug.LogWarning("[Arcane Workshop] VR did not start, staying in desktop mode. " + xrStatus);
            }
        }

        // Shown in the desktop panel so a headset that stays black can be traced to its cause.
        private static string DescribeXrFailure()
        {
            XRGeneralSettings general = XRGeneralSettings.Instance;
            if (general == null || general.Manager == null)
            {
                return "No XR settings for this platform: enable OpenXR under XR Plug-in Management for the current platform tab.";
            }
            if (general.Manager.activeLoader == null)
            {
                return "OpenXR loader failed to start: open Meta Horizon Link, connect the headset with Link, and set Meta Horizon Link as the active OpenXR runtime.";
            }
            return "XR loader " + general.Manager.activeLoader.name + " is active but its display is not running: put on the headset and accept Link, then restart Play Mode.";
        }

        public void CancelPlacement()
        {
            if (placing)
            {
                placing = false;
                ArcaneAudio.Play2D(SoundId.UiBack);
                SetFeedback("Placement cancelled. Choose a building in the spellbook to place again.");
            }
        }

        private void ShowSpellbook()
        {
            if (spellbook != null)
            {
                spellbook.gameObject.SetActive(true);
                return;
            }

            GameObject station = GameObject.Find("Player Station");
            if (station == null)
            {
                station = new GameObject("Player Station");
                station.transform.position = new Vector3(-6.4f, 0f, -6.4f);
                station.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
            }

            GameObject mount = new GameObject("Spellbook Mount");
            mount.transform.SetParent(station.transform, false);
            mount.transform.localPosition = new Vector3(-2.9f, 5.6f, 3.2f);
            mount.transform.localRotation = Quaternion.Euler(28f, -32f, 0f);
            spellbook = SpellbookPanel.Create(this, mount.transform, uiPageMaterial, uiButtonMaterial);
        }

        public string ResourceSummary()
        {
            return "Moon Herbs  " + herbs + "\nCrystals    " + crystals + "\nWater       " + water +
                "\nEssence     " + essence + "\nPotions     " + potions + "\nGold        " + gold;
        }

        public string GoalSummary()
        {
            if (goals == null)
            {
                return "";
            }
            if (workshopRestored)
            {
                return "WORKSHOP RESTORED!\nKeep brewing - the merchant\nis always buying.";
            }
            Goal goal = goals[goalIndex];
            return "GOAL " + (goalIndex + 1) + "/" + goals.Length + ": " + goal.title + "\n" + Wrap(goal.hint, 34);
        }

        private void CreateGoals()
        {
            sessionStart = Time.time;
            goals = new[]
            {
                new Goal { title = "Lay the First Rune", hint = "Build a Rune Foundation on the grid.", isDone = () => CountBuildings(BuildingKind.RuneFoundation) > 0 },
                new Goal { title = "Awaken the Garden", hint = "Build a Moon Planter on a foundation.", isDone = () => CountBuildings(BuildingKind.MoonPlanter) > 0 },
                new Goal { title = "Kindle a Mana Link", hint = "Place a Mana Still right next to your planter.", isDone = () => anyLinkEver },
                new Goal { title = "First Brew", hint = "Build a Star Cauldron and brew any recipe.", isDone = () => potionsBrewed > 0 },
                new Goal { title = "Open Trade", hint = "Sell a Potion at the Merchant Gate.", isDone = () => potionsSold > 0 },
                new Goal { title = "Complete the Chain", hint = "Own a Planter, Still, Amplifier and Cauldron at the same time.", isDone = HasFullChain },
                new Goal { title = "Master Alchemist", hint = "Earn 100 Gold from potion sales.", isDone = () => lifetimeSales >= 100 }
            };
        }

        private void CheckGoals()
        {
            if (goals == null || workshopRestored)
            {
                return;
            }

            while (goalIndex < goals.Length && goals[goalIndex].isDone())
            {
                gold += 5;
                ArcaneAudio.Play2D(SoundId.GoalComplete);
                SetFeedback("Goal complete: " + goals[goalIndex].title + "! (+5 Gold)");
                CelebrationSpark.Burst(new Vector3(0f, 1.5f, 0f), 12, moteMaterial);
                goalIndex++;
            }

            if (goalIndex >= goals.Length)
            {
                goalIndex = goals.Length - 1;
                workshopRestored = true;
                TriggerEnding();
            }
        }

        private int CountBuildings(BuildingKind kind)
        {
            int count = 0;
            for (int row = 0; row < gridSize; row++)
            {
                for (int column = 0; column < gridSize; column++)
                {
                    GridCell cell = cells[row, column];
                    if ((cell.foundation != null && cell.foundation.Kind == kind) || (cell.building != null && cell.building.Kind == kind))
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        private bool HasFullChain()
        {
            return CountBuildings(BuildingKind.MoonPlanter) > 0 && CountBuildings(BuildingKind.ManaStill) > 0 &&
                CountBuildings(BuildingKind.CrystalAmplifier) > 0 && CountBuildings(BuildingKind.StarCauldron) > 0;
        }

        private void TriggerEnding()
        {
            int minutes = Mathf.FloorToInt((Time.time - sessionStart) / 60f);
            string summary = "The moon workshop hums with life again.\n\nPotions brewed: " + potionsBrewed + "\nPotions sold: " + potionsSold +
                "\nGold from sales: " + lifetimeSales + "\nTime: " + minutes + " min\n\nThe island is yours - keep building!";
            SetFeedback("The Moon Workshop is restored!");
            ArcaneAudio.Play2D(SoundId.Ending);
            DismissTutorial();
            tutorialQueue.Clear();
            if (tutorialBubble != null)
            {
                tutorialBubble.Show("THE MOON WORKSHOP IS RESTORED", summary, new Vector3(0f, 0.2f, 0f), 1.4f);
                tutorialUntil = Time.unscaledTime + 30f;
            }
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI / 3f;
                CelebrationSpark.Burst(new Vector3(Mathf.Cos(angle) * 4f, 1f, Mathf.Sin(angle) * 4f), 16, i % 2 == 0 ? sparkMaterial : starMaterial);
            }
        }

        public static string CostLabel(BuildingKind kind)
        {
            switch (kind)
            {
                case BuildingKind.RuneFoundation: return "2 gold";
                case BuildingKind.MoonPlanter: return "2 herb + 3 gold";
                case BuildingKind.ManaStill: return "2 crystal + 5 gold";
                case BuildingKind.StarCauldron: return "2 crystal + 7 gold";
                default: return "3 crystal + 8 gold";
            }
        }

        public static string Wrap(string text, int width)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "";
            }

            if (text.IndexOf('\n') >= 0)
            {
                string[] paragraphs = text.Split('\n');
                for (int i = 0; i < paragraphs.Length; i++)
                {
                    paragraphs[i] = Wrap(paragraphs[i], width);
                }
                return string.Join("\n", paragraphs);
            }

            System.Text.StringBuilder result = new System.Text.StringBuilder();
            int lineLength = 0;
            foreach (string word in text.Split(' '))
            {
                if (lineLength > 0 && lineLength + word.Length + 1 > width)
                {
                    result.Append('\n');
                    lineLength = 0;
                }
                else if (lineLength > 0)
                {
                    result.Append(' ');
                    lineLength++;
                }
                result.Append(word);
                lineLength += word.Length;
            }
            return result.ToString();
        }

        private void TryBuild(GridCell cell)
        {
            if (cell == null)
            {
                return;
            }

            bool isFoundation = selectedBuilding == BuildingKind.RuneFoundation;
            if (isFoundation && cell.foundation != null)
            {
                SetFeedback("This cell already has a Rune Foundation.");
                return;
            }

            if (!isFoundation && cell.foundation == null)
            {
                ArcaneAudio.Play2D(SoundId.UiError);
                SetFeedback("Foundation required: build a Rune Foundation here first.");
                ShowTutorial("foundation", "Foundation Buildings: workshop machines can only be placed on a Rune Foundation.", cell.transform.position);
                return;
            }

            if (!isFoundation && cell.building != null)
            {
                SetFeedback("This foundation already supports a machine.");
                return;
            }

            ArcaneBuilding prefab = GetBuildingPrefab(selectedBuilding);
            if (prefab == null)
            {
                SetFeedback("Missing generated prefab for " + selectedBuilding + ". Run MP2B > Rebuild Arcane Workshop Prototype.");
                return;
            }

            if (!TryPayBuildCost(selectedBuilding))
            {
                ArcaneAudio.Play2D(SoundId.UiError);
                SetFeedback("Not enough player resources for " + DisplayName(selectedBuilding) + ".");
                return;
            }

            // Machines rest on the top face of the rune slab (slab top = foundation height + 0.08).
            float height = isFoundation ? 0.05f : 0.13f;
            ArcaneBuilding instance = Instantiate(prefab, cell.transform.position + Vector3.up * height, Quaternion.identity);
            instance.name = DisplayName(selectedBuilding) + " [" + cell.row + "," + cell.column + "]";
            instance.Initialize(this, cell);
            ArcaneAudio.Play(isFoundation ? SoundId.BuildFoundation : SoundId.BuildMachine, instance.transform.position);

            if (isFoundation)
            {
                cell.foundation = instance;
                ShowTutorial("building", "Building Affordance: clicking a grid cell spawned a Rune Foundation and deducted Gold. Placement snaps to the grid.", instance.transform.position);
            }
            else
            {
                cell.building = instance;
                cell.foundation.SetLabelVisible(false);
                ShowTutorial("neighbors", "Spatial Dynamics: place different machines in neighboring cells. Still/Planter, Still/Amplifier, and Amplifier/Cauldron pairs exchange stats.", instance.transform.position);
            }

            SetFeedback("Built " + DisplayName(selectedBuilding) + ".");
        }

        private bool TryPayBuildCost(BuildingKind kind)
        {
            int herbCost = 0;
            int crystalCost = 0;
            int goldCost = 0;

            switch (kind)
            {
                case BuildingKind.RuneFoundation:
                    goldCost = 2;
                    break;
                case BuildingKind.MoonPlanter:
                    herbCost = 2;
                    goldCost = 3;
                    break;
                case BuildingKind.ManaStill:
                    crystalCost = 2;
                    goldCost = 5;
                    break;
                case BuildingKind.StarCauldron:
                    crystalCost = 2;
                    goldCost = 7;
                    break;
                case BuildingKind.CrystalAmplifier:
                    crystalCost = 3;
                    goldCost = 8;
                    break;
            }

            if (herbs < herbCost || crystals < crystalCost || gold < goldCost)
            {
                return false;
            }

            herbs -= herbCost;
            crystals -= crystalCost;
            gold -= goldCost;
            return true;
        }

        private void TickNeighborInteractions(float deltaTime)
        {
            if (cells == null)
            {
                return;
            }

            activeLinks.Clear();
            for (int row = 0; row < gridSize; row++)
            {
                for (int column = 0; column < gridSize; column++)
                {
                    ArcaneBuilding current = cells[row, column].building;
                    if (current == null)
                    {
                        continue;
                    }

                    if (column + 1 < gridSize && cells[row, column + 1].building != null)
                    {
                        ApplyNeighborPair(current, cells[row, column + 1].building, deltaTime);
                    }
                    if (row + 1 < gridSize && cells[row + 1, column].building != null)
                    {
                        ApplyNeighborPair(current, cells[row + 1, column].building, deltaTime);
                    }
                }
            }

            foreach (KeyValuePair<int, ManaLink> link in manaLinks)
            {
                if (!activeLinks.Contains(link.Key) && link.Value.gameObject.activeSelf)
                {
                    link.Value.gameObject.SetActive(false);
                }
            }
        }

        private void ApplyNeighborPair(ArcaneBuilding first, ArcaneBuilding second, float deltaTime)
        {
            ArcaneBuilding source = first.ApplyNeighborInteraction(second, deltaTime);
            if (source == null)
            {
                return;
            }

            ArcaneBuilding target = source == first ? second : first;
            anyLinkEver = true;
            int key = CellIndex(first.Cell) * gridSize * gridSize + CellIndex(second.Cell);
            activeLinks.Add(key);

            ManaLink link;
            if (!manaLinks.TryGetValue(key, out link))
            {
                link = ManaLink.Create(linkRoot, linkMaterial);
                manaLinks.Add(key, link);
            }

            if (!link.gameObject.activeSelf || link.name != "Mana Link " + key)
            {
                link.name = "Mana Link " + key;
                Vector3 lift = Vector3.up * 0.75f;
                link.Connect(source.transform.position + lift, target.transform.position + lift, LinkColor(source.Kind, target.Kind));
            }

            Vector3 midpoint = (source.transform.position + target.transform.position) * 0.5f;
            if (target.Kind == BuildingKind.MoonPlanter)
            {
                ShowTutorial("link-still-planter", "Neighbor link: the Mana Still pours Mana into the adjacent Moon Planter, raising its Moisture and Growth. Watch the stream flow.", midpoint);
            }
            else if (target.Kind == BuildingKind.StarCauldron)
            {
                ShowTutorial("link-amp-cauldron", "Neighbor link: the Crystal Amplifier spends Charge to heat the adjacent Star Cauldron and speed up its brew.", midpoint);
            }
            else if (target.Kind == BuildingKind.CrystalAmplifier)
            {
                ShowTutorial("link-still-amp", "Neighbor link: the Mana Still feeds Mana into the adjacent Crystal Amplifier, restoring Charge and Resonance.", midpoint);
            }
        }

        private int CellIndex(GridCell cell)
        {
            return cell.row * gridSize + cell.column;
        }

        private static Color LinkColor(BuildingKind source, BuildingKind target)
        {
            if (target == BuildingKind.MoonPlanter)
            {
                return new Color(0.35f, 1f, 0.75f);
            }
            if (target == BuildingKind.StarCauldron)
            {
                return new Color(1f, 0.45f, 0.95f);
            }
            return new Color(0.45f, 0.7f, 1f);
        }

        private void SpawnInitialWildInstances()
        {
            for (int i = 0; i < 6; i++)
            {
                SpawnWildInstance(NodeKind.MoonHerb);
            }
            for (int i = 0; i < 3; i++)
            {
                SpawnWildInstance(NodeKind.CrystalCluster);
            }
        }

        private static int CountWildNodes(NodeKind kind)
        {
            int count = 0;
            foreach (GatherableNode node in FindObjectsByType<GatherableNode>())
            {
                if (node.Kind == kind)
                {
                    count++;
                }
            }
            return count;
        }

        private GatherableNode SpawnWildInstance(NodeKind kind)
        {
            GatherableNode prefab = kind == NodeKind.MoonHerb ? moonHerbPrefab : crystalClusterPrefab;
            if (prefab == null)
            {
                return null;
            }

            Vector3 position;
            if (!FindSpawnPoint(out position))
            {
                return null;
            }
            GatherableNode instance = Instantiate(prefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            instance.name = DisplayNodeName(kind) + " (Wild)";
            return instance;
        }

        // Footprints of the furniture, walls, trees, rocks and the Merchant Gate, so wild materials
        // never sprout inside them.
        private void CollectSpawnBlockers()
        {
            spawnBlockers.Clear();
            List<Renderer> renderers = new List<Renderer>();
            GameObject environment = GameObject.Find("Arcane Workshop Environment");
            if (environment != null)
            {
                renderers.AddRange(environment.GetComponentsInChildren<Renderer>());
            }
            if (merchantPortal != null)
            {
                renderers.AddRange(merchantPortal.GetComponentsInChildren<Renderer>());
            }

            foreach (Renderer blocker in renderers)
            {
                Bounds bounds = blocker.bounds;
                bool groundDisc = bounds.size.x > 6f && bounds.size.z > 6f;
                bool belowFloor = bounds.max.y < 0.05f;
                if (!groundDisc && !belowFloor)
                {
                    spawnBlockers.Add(bounds);
                }
            }
        }

        private bool FindSpawnPoint(out Vector3 position)
        {
            for (int attempt = 0; attempt < 40; attempt++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float radius = Random.Range(6.6f, 8.6f);
                position = new Vector3(Mathf.Cos(angle) * radius, -0.06f, Mathf.Sin(angle) * radius);
                if (IsSpawnPointFree(position))
                {
                    return true;
                }
            }
            position = Vector3.zero;
            return false;
        }

        private bool IsSpawnPointFree(Vector3 position)
        {
            const float clearance = 0.7f;
            // The VR station ring (radius 7.5) lies inside the wild spawn ring, so keep the player's feet clear.
            if (vrRig != null && vrRig.Head != null)
            {
                Vector3 toPlayer = vrRig.Head.position - position;
                toPlayer.y = 0f;
                if (toPlayer.magnitude < 1.8f)
                {
                    return false;
                }
            }
            foreach (Bounds bounds in spawnBlockers)
            {
                if (position.x > bounds.min.x - clearance && position.x < bounds.max.x + clearance &&
                    position.z > bounds.min.z - clearance && position.z < bounds.max.z + clearance)
                {
                    return false;
                }
            }

            foreach (GatherableNode other in FindObjectsByType<GatherableNode>())
            {
                Vector3 offset = other.transform.position - position;
                offset.y = 0f;
                if (offset.magnitude < 1.4f)
                {
                    return false;
                }
            }
            return true;
        }

        public void ShatterCrystal(Vector3 position)
        {
            if (crystalShardPrefab == null)
            {
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                float angle = i * Mathf.PI * 2f / 3f;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.55f;
                Instantiate(crystalShardPrefab, position + offset, Quaternion.Euler(0f, i * 120f, 0f));
            }
        }

        public void SpawnIngredientMotes(ArcaneBuilding cauldron)
        {
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * 0.5f;
                Vector3 start = cauldron.transform.position + new Vector3(Mathf.Cos(angle) * 2f, 0.9f + i * 0.12f, Mathf.Sin(angle) * 2f);
                ArcaneProjectile mote = ArcaneProjectile.Spawn(ProjectileKind.Ingredient, start, cauldron, Vector3.up * 0.55f, null, 0.16f);
                mote.GetComponent<Renderer>().sharedMaterial = moteMaterial;
            }
        }

        public void SpawnWaterDrops(ArcaneBuilding planter)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector3 offset = new Vector3((i - 1) * 0.18f, 0f, (i % 2) * 0.12f - 0.06f);
                Vector3 start = planter.transform.position + offset + Vector3.up * (1.7f + i * 0.35f);
                ArcaneProjectile drop = ArcaneProjectile.Spawn(ProjectileKind.WaterDrop, start, planter, offset + Vector3.up * 0.4f, null, 0.08f);
                drop.GetComponent<Renderer>().sharedMaterial = waterMaterial;
            }
        }

        public void SpawnCrystalSpark(ArcaneBuilding amplifier)
        {
            Vector3 start = amplifier.transform.position + new Vector3(-1.4f, 1.6f, -1.4f);
            ArcaneProjectile spark = ArcaneProjectile.Spawn(ProjectileKind.CrystalSpark, start, amplifier, Vector3.up * 0.7f, null, 0.14f);
            spark.GetComponent<Renderer>().sharedMaterial = sparkMaterial;
            spark.transform.localScale = new Vector3(0.1f, 0.2f, 0.1f);
            AddTrail(spark.gameObject, new Color(0.85f, 0.4f, 1f), 0.3f);
        }

        private void SpawnFallingStar()
        {
            List<ArcaneBuilding> targets = new List<ArcaneBuilding>();
            for (int row = 0; row < gridSize; row++)
            {
                for (int column = 0; column < gridSize; column++)
                {
                    GridCell cell = cells[row, column];
                    ArcaneBuilding top = cell.building != null ? cell.building : cell.foundation;
                    if (top != null)
                    {
                        targets.Add(top);
                    }
                }
            }

            if (targets.Count == 0)
            {
                return;
            }

            ArcaneBuilding target = targets[Random.Range(0, targets.Count)];
            Vector3 offset = target.Kind == BuildingKind.RuneFoundation ? Vector3.up * 0.04f : Vector3.up * 0.55f;
            Vector3 start = target.transform.position + new Vector3(Random.Range(-5f, 5f), 11f, Random.Range(-5f, 5f));
            ArcaneProjectile star = ArcaneProjectile.Spawn(ProjectileKind.FallingStar, start, target, offset, null, 0.22f);
            star.GetComponent<Renderer>().sharedMaterial = starMaterial;
            AddTrail(star.gameObject, new Color(1f, 0.85f, 0.45f), 0.8f);
            ArcaneAudio.Play(SoundId.StarWhoosh, Vector3.Lerp(start, target.transform.position, 0.6f));
            SetFeedback("A falling star streaks toward the workshop!");
        }

        private void AddTrail(GameObject owner, Color color, float time)
        {
            TrailRenderer trail = owner.AddComponent<TrailRenderer>();
            trail.sharedMaterial = trailMaterial;
            trail.time = time;
            trail.widthMultiplier = 0.14f;
            trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private void ShipPotion()
        {
            if (merchantPortal == null)
            {
                return;
            }

            if (potionStock.Count == 0)
            {
                ArcaneAudio.Play2D(SoundId.UiError);
                SetFeedback("No Potions to sell yet. Brew one in a Star Cauldron first.");
                return;
            }

            int baseGold = potionStock.Dequeue();
            potions = potionStock.Count;
            Vector3[] path = merchantPortal.DeliveryPath;
            Vector3 start = path != null && path.Length > 0 ? path[0] : merchantPortal.transform.position + Vector3.up * 3f;
            ArcaneProjectile bottle = ArcaneProjectile.Spawn(ProjectileKind.Potion, start, merchantPortal, Vector3.up * 1.3f, potionBottleVisual, 0.9f)
                .WithPayload(baseGold)
                .AlongPath(path);
            AddTrail(bottle.gameObject, new Color(0.4f, 1f, 0.7f), 0.6f);
            ArcaneAudio.Play(SoundId.PotionSend, start);
            SetFeedback("A Potion leaves the shelf for the Merchant Gate (current offer: " + merchantPortal.PriceFor(baseGold) + " Gold).");
            ShowTutorial("merchant-sell", "Selling: right-click the Merchant Gate to send one Potion. It flies along the delivery route; payment depends on the gate's Demand and Reputation.", merchantPortal.transform.position);
        }

        public void ReceiveSale(int payment, Vector3 position)
        {
            gold += payment;
            potionsSold++;
            lifetimeSales += payment;
            SetFeedback("The merchant paid " + payment + " Gold for the Potion.");
            ShowTutorial("delivery", "Delivered! Each sale lowers Demand (it recovers over time) and raises Reputation. Spread out your sales for better prices.", position);
        }

        public void DestroyBuilding(ArcaneBuilding building)
        {
            GridCell cell = building.Cell;
            if (cell != null)
            {
                if (cell.building == building)
                {
                    cell.building = null;
                    if (cell.foundation != null)
                    {
                        cell.foundation.SetLabelVisible(true);
                    }
                }
                else if (cell.foundation == building)
                {
                    cell.foundation = null;
                }
            }

            ArcaneAudio.Play(SoundId.BuildingCrumble, building.transform.position);
            SetFeedback("The " + DisplayName(building.Kind) + " crumbled after losing all of its Integrity.");
            ShowTutorial("building-death", "Death: when a building's Integrity reaches 0 it is destroyed. Repair damaged buildings before the next star falls.", building.transform.position);
            Destroy(building.gameObject);
        }

        public bool TryConsumeRecipe(int recipeIndex)
        {
            if (recipeIndex < 0 || recipeIndex >= recipes.Length)
            {
                return false;
            }

            BrewRecipe recipe = recipes[recipeIndex];
            if (herbs < recipe.herbs || crystals < recipe.crystals || water < recipe.water || essence < recipe.essence)
            {
                return false;
            }

            herbs -= recipe.herbs;
            crystals -= recipe.crystals;
            water -= recipe.water;
            essence -= recipe.essence;
            return true;
        }

        public void CompleteBrew(int recipeIndex, Vector3 position)
        {
            if (recipeIndex < 0 || recipeIndex >= recipes.Length)
            {
                return;
            }

            potionStock.Enqueue(recipes[recipeIndex].goldReward);
            potionsBrewed++;
            ArcaneAudio.Play(SoundId.BrewComplete, position);
            potions = potionStock.Count;
            SetFeedback(recipes[recipeIndex].name + " completed: +1 Potion (worth about " + recipes[recipeIndex].goldReward + " Gold). Sell it at the Merchant Gate.");
            ShowTutorial("brew-complete", "Brew complete: the ingredients became a Potion. Right-click the Merchant Gate at the island edge to sell it for Gold.", position);
        }

        public bool TrySpendWater(int amount)
        {
            if (water < amount)
            {
                return false;
            }
            water -= amount;
            return true;
        }

        public bool TrySpendCrystals(int amount)
        {
            if (crystals < amount)
            {
                return false;
            }
            crystals -= amount;
            return true;
        }

        public void AddHerbs(int amount) { herbs += amount; }
        public void AddCrystals(int amount) { crystals += amount; }
        public void AddWater(int amount) { water += amount; }
        public void AddEssence(int amount) { essence += amount; }

        public void SetFeedback(string message)
        {
            feedback = message;
            feedbackUntil = Time.unscaledTime + 4.5f;
        }

        public void ShowTutorial(string key, string message, Vector3 worldPosition)
        {
            if (shownTutorials.Contains(key))
            {
                return;
            }

            shownTutorials.Add(key);
            if (Time.unscaledTime < tutorialUntil)
            {
                tutorialQueue.Enqueue(new KeyValuePair<string, Vector3>(message, worldPosition));
                return;
            }
            DisplayTutorial(message, worldPosition);
        }

        private void DisplayTutorial(string message, Vector3 worldPosition)
        {
            activeTutorial = message;
            tutorialWorldPosition = worldPosition;
            tutorialUntil = Time.unscaledTime + 10f;
            ArcaneAudio.Play(SoundId.TutorialPage, worldPosition);
            if (tutorialBubble != null)
            {
                tutorialBubble.Show("NEW MECHANIC", message, worldPosition, 1f);
            }
        }

        private void DismissTutorial()
        {
            tutorialUntil = 0f;
            if (tutorialBubble != null)
            {
                tutorialBubble.Hide();
            }
        }

#if UNITY_EDITOR
        // Editor-only shortcut for testing: builds the ideal Planter <- Still -> Amplifier -> Cauldron chain.
        private string DebugBuildDemoLayout()
        {
            gold += 100;
            crystals += 20;
            herbs += 10;
            water += 10;
            essence += 5;
            BuildingKind[] chain = { BuildingKind.MoonPlanter, BuildingKind.ManaStill, BuildingKind.CrystalAmplifier, BuildingKind.StarCauldron };
            for (int i = 0; i < chain.Length; i++)
            {
                GridCell cell = cells[3, 2 + i];
                selectedBuilding = BuildingKind.RuneFoundation;
                TryBuild(cell);
                selectedBuilding = chain[i];
                TryBuild(cell);
            }
            selectedBuilding = BuildingKind.MoonPlanter;
            placing = true;
            cells[3, 5].building.Interact();
            return "demo layout built";
        }

        private string InteractFirstNode(NodeKind kind)
        {
            foreach (GatherableNode node in FindObjectsByType<GatherableNode>())
            {
                if (node.Kind == kind)
                {
                    node.Interact(this);
                    return kind + " interacted";
                }
            }
            return "no " + kind;
        }

        private string DebugAction(string action)
        {
            switch (action)
            {
                case "star": SpawnFallingStar(); break;
                case "water": cells[3, 2].building.Interact(); break;
                case "charge": cells[3, 4].building.Interact(); break;
                case "sell": potionStock.Enqueue(15); ShipPotion(); break;
                case "shatter": return InteractFirstNode(NodeKind.CrystalCluster);
                case "pickshard": return InteractFirstNode(NodeKind.CrystalShard);
                case "pickherb": return InteractFirstNode(NodeKind.MoonHerb);
                case "ripen": cells[3, 2].building.DebugSetGrowth(100f); cells[3, 2].building.Interact(); break;
                case "kill": cells[3, 4].building.DebugDamage(200f); break;
                case "nodes": return "nodes=" + FindObjectsByType<GatherableNode>().Length + " blockers=" + spawnBlockers.Count;
                case "goals": potionsBrewed++; potionsSold++; lifetimeSales += 200; anyLinkEver = true; return "goals forced";
                case "stats": return "herbs=" + herbs + " crystals=" + crystals + " water=" + water + " essence=" + essence + " potions=" + potions + " gold=" + gold + " tut=" + string.Join(",", shownTutorials);
            }
            return action + " ok";
        }
#endif

        private ArcaneBuilding GetBuildingPrefab(BuildingKind kind)
        {
            if (buildingPrefabs == null)
            {
                return null;
            }

            for (int i = 0; i < buildingPrefabs.Length; i++)
            {
                if (buildingPrefabs[i].kind == kind)
                {
                    return buildingPrefabs[i].prefab;
                }
            }
            return null;
        }

        private void OnGUI()
        {
            if (ExternalPointerActive)
            {
                return;
            }

            EnsureGuiStyles();
            if (!string.IsNullOrEmpty(xrStatus))
            {
                GUI.Label(new Rect(316f, Screen.height - 58f, Screen.width - 328f, 48f), "VR not running: " + xrStatus, smallStyle);
            }
            GUI.Box(new Rect(12f, 12f, 292f, Screen.height - 24f), GUIContent.none);
            GUILayout.BeginArea(new Rect(26f, 22f, 264f, Screen.height - 44f));

            GUILayout.Label("ARCANE WORKSHOP", titleStyle);
            GUILayout.Label("Player Resources", resourceStyle);
            GUILayout.Label("Moon Herbs: " + herbs + "    Crystals: " + crystals, smallStyle);
            GUILayout.Label("Water: " + water + "    Essence: " + essence, smallStyle);
            GUILayout.Label("Potions: " + potions + "    Gold: " + gold, smallStyle);
            GUILayout.Space(8f);
            GUILayout.Label(GoalSummary(), resourceStyle);
            GUILayout.Space(12f);

            GUILayout.Label("Build on the Grid", resourceStyle);
            DrawBuildButton(BuildingKind.RuneFoundation, "1", "2 gold");
            DrawBuildButton(BuildingKind.MoonPlanter, "2", "2 herb + 3 gold");
            DrawBuildButton(BuildingKind.ManaStill, "3", "2 crystal + 5 gold");
            DrawBuildButton(BuildingKind.StarCauldron, "4", "2 crystal + 7 gold");
            DrawBuildButton(BuildingKind.CrystalAmplifier, "5", "3 crystal + 8 gold");

            GUILayout.Space(12f);
            GUILayout.Label("Cauldron Recipe", resourceStyle);
            for (int i = 0; i < recipes.Length; i++)
            {
                string marker = selectedRecipe == i ? "● " : "○ ";
                if (GUILayout.Button(marker + recipes[i].name + "\n" + recipes[i].CostLabel(), GUILayout.Height(42f)))
                {
                    SelectRecipe(i);
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label("Controls", resourceStyle);
            GUILayout.Label("1-5: pick a building, Esc: cancel\nLeft click: gather or build\nRight click: use a building\nShift + right click: repair\nMiddle drag: orbit camera\nMouse wheel: zoom", smallStyle);
            GUILayout.Space(6f);
            GUILayout.Label("Metaphor: a tabletop magical workshop. Glowing labels expose changing Stats; the rune floor communicates valid placement.", smallStyle);
            GUILayout.EndArea();

            if (Time.unscaledTime < feedbackUntil)
            {
                GUI.Box(new Rect(330f, 18f, Mathf.Min(620f, Screen.width - 350f), 42f), feedback);
            }

        }

        private void DrawBuildButton(BuildingKind kind, string hotkey, string cost)
        {
            string marker = placing && selectedBuilding == kind ? "● " : "○ ";
            if (GUILayout.Button(marker + hotkey + "  " + DisplayName(kind) + " — " + cost, GUILayout.Height(31f)))
            {
                SelectBuilding(kind);
            }
        }

        private void EnsureGuiStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 22;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = new Color(0.55f, 0.95f, 1f);

            resourceStyle = new GUIStyle(GUI.skin.label);
            resourceStyle.fontSize = 15;
            resourceStyle.fontStyle = FontStyle.Bold;
            resourceStyle.normal.textColor = new Color(0.92f, 0.80f, 1f);

            smallStyle = new GUIStyle(GUI.skin.label);
            smallStyle.fontSize = 12;
            smallStyle.wordWrap = true;
        }

        public static string DisplayName(BuildingKind kind)
        {
            switch (kind)
            {
                case BuildingKind.RuneFoundation: return "Rune Foundation";
                case BuildingKind.MoonPlanter: return "Moon Planter";
                case BuildingKind.ManaStill: return "Mana Still";
                case BuildingKind.StarCauldron: return "Star Cauldron";
                case BuildingKind.CrystalAmplifier: return "Crystal Amplifier";
                default: return kind.ToString();
            }
        }

        private static string DisplayNodeName(NodeKind kind)
        {
            switch (kind)
            {
                case NodeKind.MoonHerb: return "Moon Herb";
                case NodeKind.CrystalCluster: return "Crystal Cluster";
                case NodeKind.CrystalShard: return "Crystal Shard";
                default: return kind.ToString();
            }
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }
    }
}
