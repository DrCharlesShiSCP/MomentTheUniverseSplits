using System.Collections;
using UnityEngine;

namespace QuantumBranching
{
    public class BranchVisualManager : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private BranchWorld[] branchWorlds = new BranchWorld[0];
        [SerializeField] private GameObject branchPortalRoot;
        [SerializeField] private Transform hubSpawnPoint;

        [Header("Hub Objects")]
        [SerializeField] private GameObject[] activateOnSplit = new GameObject[0];
        [SerializeField] private GameObject[] deactivateOnSplit = new GameObject[0];
        [SerializeField] private TextMesh[] hubStatusLabels = new TextMesh[0];

        [Header("Reveal Timing")]
        [SerializeField] private float splitDelay = 0.35f;
        [SerializeField] private float splitSlowMotionDuration = 0.2f;
        [SerializeField] private float slowMotionScale = 0.2f;

        private bool splitTriggered;
        private Coroutine splitRoutine;
        private bool warnedAboutMissingBranchWorlds;
        private bool warnedAboutMissingHubSpawnPoint;
        private bool loggedRuntimePortalSetup;

        public bool IsSplitTriggered => splitTriggered;

        public void Configure(
            BranchWorld[] worlds,
            GameObject portalRoot,
            Transform hubSpawn,
            GameObject[] splitActivatedObjects,
            GameObject[] splitDeactivatedObjects,
            TextMesh[] statusLabels)
        {
            branchWorlds = worlds;
            branchPortalRoot = portalRoot;
            hubSpawnPoint = hubSpawn;
            activateOnSplit = splitActivatedObjects;
            deactivateOnSplit = splitDeactivatedObjects;
            hubStatusLabels = statusLabels;
        }

        private void Awake()
        {
            EnsureRuntimePortalHub();

            if (branchWorlds == null || branchWorlds.Length == 0)
            {
                Debug.LogWarning($"{nameof(BranchVisualManager)} on {name} has no branch worlds assigned.", this);
                warnedAboutMissingBranchWorlds = true;
            }

            if (hubSpawnPoint == null)
            {
                Debug.LogWarning($"{nameof(BranchVisualManager)} on {name} has no hub spawn point assigned.", this);
                warnedAboutMissingHubSpawnPoint = true;
            }

            InitializeSceneState();
        }

        private void Start()
        {
            if (!splitTriggered)
            {
                InitializeSceneState();
                Debug.Log($"{nameof(BranchVisualManager)} reapplied startup scene state in Start().", this);
            }
        }

        private void OnDestroy()
        {
            if (Time.timeScale != 1f)
            {
                Time.timeScale = 1f;
            }
        }

        public void InitializeSceneState()
        {
            splitTriggered = false;

            if (branchPortalRoot != null)
            {
                branchPortalRoot.SetActive(false);
            }

            for (var index = 0; index < activateOnSplit.Length; index++)
            {
                if (activateOnSplit[index] != null)
                {
                    activateOnSplit[index].SetActive(false);
                }
            }

            for (var index = 0; index < deactivateOnSplit.Length; index++)
            {
                if (deactivateOnSplit[index] != null)
                {
                    deactivateOnSplit[index].SetActive(true);
                }
            }

            for (var index = 0; index < branchWorlds.Length; index++)
            {
                if (branchWorlds[index] == null)
                {
                    continue;
                }

                branchWorlds[index].ResolveRoomRoot();
                branchWorlds[index].SetVisible(false);
            }

            UpdateHubStatus("Singular reality.\nThe apparatus awaits measurement.");
            Debug.Log($"{nameof(BranchVisualManager)} initialized scene state.", this);
        }

        public void TriggerSplit()
        {
            if (splitTriggered)
            {
                Debug.Log($"{nameof(BranchVisualManager)} ignored duplicate split trigger.", this);
                return;
            }

            if (splitRoutine != null)
            {
                StopCoroutine(splitRoutine);
            }

            Debug.Log($"{nameof(BranchVisualManager)} received split trigger.", this);
            splitRoutine = StartCoroutine(TriggerSplitRoutine());
        }

        public void TeleportToBranch(SimplePlayerInteractor interactor, BranchWorld branchWorld)
        {
            if (interactor == null)
            {
                Debug.LogWarning($"{nameof(BranchVisualManager)} cannot teleport to branch because the player interactor is missing.", this);
                return;
            }

            if (branchWorld == null)
            {
                Debug.LogWarning($"{nameof(BranchVisualManager)} cannot teleport to branch because the destination branch is missing.", this);
                return;
            }

            if (branchWorld.SpawnPoint == null)
            {
                Debug.LogWarning($"{nameof(BranchVisualManager)} cannot teleport to {branchWorld.BranchTitle} because it has no spawn point.", branchWorld);
                return;
            }

            Debug.Log($"{nameof(BranchVisualManager)} teleporting player to {branchWorld.BranchTitle}.", branchWorld);
            interactor.TeleportTo(branchWorld.SpawnPoint);
        }

        public void TeleportToHub(SimplePlayerInteractor interactor)
        {
            if (interactor == null)
            {
                Debug.LogWarning($"{nameof(BranchVisualManager)} cannot teleport to hub because the player interactor is missing.", this);
                return;
            }

            if (hubSpawnPoint == null)
            {
                if (!warnedAboutMissingHubSpawnPoint)
                {
                    Debug.LogWarning($"{nameof(BranchVisualManager)} cannot teleport to hub because the hub spawn point is missing.", this);
                    warnedAboutMissingHubSpawnPoint = true;
                }

                return;
            }

            Debug.Log($"{nameof(BranchVisualManager)} teleporting player to the hub.", this);
            interactor.TeleportTo(hubSpawnPoint);
        }

        private IEnumerator TriggerSplitRoutine()
        {
            splitTriggered = true;
            UpdateHubStatus("Measurement observed.\nOne lab becomes four branches.");
            Debug.Log($"{nameof(BranchVisualManager)} split routine started.", this);

            var previousTimeScale = Time.timeScale;
            Time.timeScale = slowMotionScale;
            yield return new WaitForSecondsRealtime(splitSlowMotionDuration);
            Time.timeScale = previousTimeScale;

            yield return new WaitForSeconds(splitDelay);

            for (var index = 0; index < deactivateOnSplit.Length; index++)
            {
                if (deactivateOnSplit[index] != null)
                {
                    deactivateOnSplit[index].SetActive(false);
                }
            }

            for (var index = 0; index < branchWorlds.Length; index++)
            {
                if (branchWorlds[index] == null)
                {
                    if (!warnedAboutMissingBranchWorlds)
                    {
                        Debug.LogWarning($"{nameof(BranchVisualManager)} encountered a missing branch world reference during reveal.", this);
                        warnedAboutMissingBranchWorlds = true;
                    }

                    continue;
                }

                branchWorlds[index].SetVisible(true);
                Debug.Log($"{nameof(BranchVisualManager)} revealed {branchWorlds[index].BranchTitle}.", branchWorlds[index]);
                yield return new WaitForSeconds(0.05f);
            }

            if (branchPortalRoot != null)
            {
                branchPortalRoot.SetActive(true);
            }

            for (var index = 0; index < activateOnSplit.Length; index++)
            {
                if (activateOnSplit[index] != null)
                {
                    activateOnSplit[index].SetActive(true);
                }
            }

            UpdateHubStatus("Branches revealed.\nUse the portal hub or walk into each branch.");
            Debug.Log($"{nameof(BranchVisualManager)} split routine finished and all branches are visible.", this);
            splitRoutine = null;
        }

        private void EnsureRuntimePortalHub()
        {
            var hubParent = transform.Find("PreSplitLab");
            if (hubParent == null)
            {
                hubParent = transform;
            }

            if (hubSpawnPoint == null)
            {
                var existingSpawn = hubParent.Find("QuantumHubSpawn");
                if (existingSpawn == null)
                {
                    existingSpawn = new GameObject("QuantumHubSpawn").transform;
                    existingSpawn.SetParent(hubParent, false);
                    existingSpawn.localPosition = new Vector3(0f, 1.6f, -3.4f);
                    existingSpawn.localRotation = Quaternion.identity;
                }

                hubSpawnPoint = existingSpawn;
            }

            if (branchPortalRoot == null)
            {
                var existingPortalRoot = hubParent.Find("BranchPortalRoot");
                if (existingPortalRoot == null)
                {
                    existingPortalRoot = new GameObject("BranchPortalRoot").transform;
                    existingPortalRoot.SetParent(hubParent, false);
                    existingPortalRoot.localPosition = new Vector3(0f, 0f, 3.4f);
                    existingPortalRoot.localRotation = Quaternion.identity;
                }

                branchPortalRoot = existingPortalRoot.gameObject;
            }

            if (branchPortalRoot == null)
            {
                Debug.LogWarning($"{nameof(BranchVisualManager)} on {name} could not resolve a portal root.", this);
                return;
            }

            EnsurePortalDecor(branchPortalRoot.transform);

            if (!loggedRuntimePortalSetup)
            {
                Debug.Log($"{nameof(BranchVisualManager)} prepared the runtime portal hub.", this);
                loggedRuntimePortalSetup = true;
            }
        }

        private void EnsurePortalDecor(Transform portalRoot)
        {
            EnsurePortalTitle(portalRoot);

            for (var index = 0; index < branchWorlds.Length; index++)
            {
                if (branchWorlds[index] == null)
                {
                    continue;
                }

                var portal = portalRoot.Find($"Portal_{index + 1:00}");
                if (portal == null)
                {
                    portal = new GameObject($"Portal_{index + 1:00}").transform;
                    portal.SetParent(portalRoot, false);
                }

                portal.localPosition = new Vector3(-3.3f + (index * 2.2f), 0f, 0f);
                portal.localRotation = Quaternion.identity;
                EnsurePortalInteractable(portal.gameObject, branchWorlds[index]);
                EnsurePortalVisual(portal, branchWorlds[index]);
            }
        }

        private void EnsurePortalTitle(Transform portalRoot)
        {
            var title = portalRoot.Find("PortalHubTitle");
            if (title == null)
            {
                title = new GameObject("PortalHubTitle").transform;
                title.SetParent(portalRoot, false);
                title.localPosition = new Vector3(0f, 2.2f, 0f);
            }

            var text = title.GetComponent<TextMesh>();
            if (text == null)
            {
                text = title.gameObject.AddComponent<TextMesh>();
            }

            text.text = "Branch Portal Hub";
            text.characterSize = 0.14f;
            text.fontSize = 48;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = new Color(0.8f, 0.88f, 0.98f, 1f);
        }

        private void EnsurePortalInteractable(GameObject portalObject, BranchWorld branchWorld)
        {
            var collider = portalObject.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = portalObject.AddComponent<BoxCollider>();
            }
            collider.isTrigger = true;
            collider.center = new Vector3(0f, 1.05f, 0f);
            collider.size = new Vector3(1.1f, 2.1f, 1.1f);

            var portal = portalObject.GetComponent<QuantumBranchPortal>();
            if (portal == null)
            {
                portal = portalObject.AddComponent<QuantumBranchPortal>();
            }

            var prompt = $"Press E to Enter {GetBranchLabel(branchWorld)}";
            portal.Configure(this, prompt, branchWorld, null, true);
        }

        private void EnsurePortalVisual(Transform portalRoot, BranchWorld branchWorld)
        {
            var color = GetOutcomeColor(branchWorld != null ? branchWorld.OutcomeType : BranchOutcomeType.Stabilized);
            var label = GetBranchLabel(branchWorld);

            var pedestal = EnsurePrimitive(portalRoot, PrimitiveType.Cylinder, "Pedestal", new Vector3(0f, 0.18f, 0f), Vector3.zero, new Vector3(0.75f, 0.12f, 0.75f));
            var frame = EnsurePrimitive(portalRoot, PrimitiveType.Cube, "PortalFrame", new Vector3(0f, 1.05f, 0f), Vector3.zero, new Vector3(0.9f, 1.5f, 0.12f));
            var core = EnsurePrimitive(portalRoot, PrimitiveType.Sphere, "PortalCore", new Vector3(0f, 1.05f, 0f), Vector3.zero, new Vector3(0.35f, 0.75f, 0.16f));

            TintRenderer(pedestal.GetComponent<Renderer>(), new Color(0.22f, 0.24f, 0.28f, 1f));
            TintRenderer(frame.GetComponent<Renderer>(), color * 0.9f);
            TintRenderer(core.GetComponent<Renderer>(), color);

            var textTransform = portalRoot.Find("PortalLabel");
            if (textTransform == null)
            {
                textTransform = new GameObject("PortalLabel").transform;
                textTransform.SetParent(portalRoot, false);
                textTransform.localPosition = new Vector3(0f, 1.9f, 0f);
            }

            var text = textTransform.GetComponent<TextMesh>();
            if (text == null)
            {
                text = textTransform.gameObject.AddComponent<TextMesh>();
            }
            text.text = label;
            text.characterSize = 0.11f;
            text.fontSize = 40;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = color;
        }

        private static GameObject EnsurePrimitive(
            Transform parent,
            PrimitiveType primitiveType,
            string name,
            Vector3 localPosition,
            Vector3 localEulerAngles,
            Vector3 localScale)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var created = GameObject.CreatePrimitive(primitiveType);
            created.name = name;
            created.transform.SetParent(parent, false);
            created.transform.localPosition = localPosition;
            created.transform.localEulerAngles = localEulerAngles;
            created.transform.localScale = localScale;

            var collider = created.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            return created;
        }

        private static void TintRenderer(Renderer renderer, Color color)
        {
            if (renderer == null)
            {
                return;
            }

            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block);
        }

        private static string GetBranchLabel(BranchWorld branchWorld)
        {
            if (branchWorld == null)
            {
                return "Unknown Branch";
            }

            return string.IsNullOrWhiteSpace(branchWorld.BranchTitle) ? branchWorld.OutcomeType.ToString() : branchWorld.BranchTitle;
        }

        private static Color GetOutcomeColor(BranchOutcomeType outcome)
        {
            return outcome switch
            {
                BranchOutcomeType.Stabilized => new Color(0.35f, 1f, 0.65f, 1f),
                BranchOutcomeType.Destabilized => new Color(1f, 0.35f, 0.35f, 1f),
                BranchOutcomeType.Duplicated => new Color(0.35f, 0.95f, 1f, 1f),
                _ => new Color(0.72f, 0.78f, 1f, 1f)
            };
        }

        private void UpdateHubStatus(string message)
        {
            for (var index = 0; index < hubStatusLabels.Length; index++)
            {
                if (hubStatusLabels[index] != null)
                {
                    hubStatusLabels[index].text = message;
                }
            }
        }
    }
}
