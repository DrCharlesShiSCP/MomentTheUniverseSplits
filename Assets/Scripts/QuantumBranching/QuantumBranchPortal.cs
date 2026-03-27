using UnityEngine;

namespace QuantumBranching
{
    public class QuantumBranchPortal : MonoBehaviour, IQuantumInteractable
    {
        [Header("Destination")]
        [SerializeField] private BranchVisualManager visualManager;
        [SerializeField] private BranchWorld branchDestination;
        [SerializeField] private Transform explicitDestination;
        [SerializeField] private bool requiresSplit = true;

        [Header("Prompt")]
        [SerializeField] private string interactionPrompt = "Press E to Enter Branch";

        private void Awake()
        {
            if (requiresSplit && visualManager == null)
            {
                Debug.LogWarning($"{nameof(QuantumBranchPortal)} on {name} requires a {nameof(BranchVisualManager)} reference because split gating is enabled.", this);
            }

            if (explicitDestination == null && branchDestination == null && visualManager == null)
            {
                Debug.LogWarning($"{nameof(QuantumBranchPortal)} on {name} has no destination configured.", this);
            }
        }

        public void Configure(
            BranchVisualManager manager,
            string prompt,
            BranchWorld targetBranch,
            Transform targetPoint,
            bool splitRequired)
        {
            visualManager = manager;
            interactionPrompt = prompt;
            branchDestination = targetBranch;
            explicitDestination = targetPoint;
            requiresSplit = splitRequired;
        }

        public bool CanInteract(SimplePlayerInteractor interactor)
        {
            if (!requiresSplit)
            {
                return true;
            }

            return visualManager == null || visualManager.IsSplitTriggered;
        }

        public string GetInteractionPrompt(SimplePlayerInteractor interactor)
        {
            return interactionPrompt;
        }

        public void Interact(SimplePlayerInteractor interactor)
        {
            if (!CanInteract(interactor))
            {
                Debug.Log($"{nameof(QuantumBranchPortal)} on {name} blocked interaction because the split has not happened yet.", this);
                return;
            }

            if (explicitDestination != null)
            {
                Debug.Log($"{nameof(QuantumBranchPortal)} on {name} sent the player to an explicit destination.", this);
                interactor.TeleportTo(explicitDestination);
                return;
            }

            if (branchDestination != null && visualManager != null)
            {
                Debug.Log($"{nameof(QuantumBranchPortal)} on {name} sent the player to {branchDestination.BranchTitle}.", this);
                visualManager.TeleportToBranch(interactor, branchDestination);
                return;
            }

            if (visualManager != null)
            {
                Debug.Log($"{nameof(QuantumBranchPortal)} on {name} sent the player back to the hub.", this);
                visualManager.TeleportToHub(interactor);
                return;
            }

            Debug.LogWarning($"{nameof(QuantumBranchPortal)} on {name} could not resolve a destination.", this);
        }
    }
}
