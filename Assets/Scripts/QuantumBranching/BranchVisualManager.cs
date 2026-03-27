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
                if (branchWorlds[index] != null)
                {
                    branchWorlds[index].SetVisible(false);
                }
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

            UpdateHubStatus("Branches revealed.\nInspect each outcome and its entangled monitor.");
            Debug.Log($"{nameof(BranchVisualManager)} split routine finished and all branches are visible.", this);
            splitRoutine = null;
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
