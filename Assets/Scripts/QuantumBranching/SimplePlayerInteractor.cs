using UnityEngine;

namespace QuantumBranching
{
    public class SimplePlayerInteractor : MonoBehaviour
    {
        [Header("Interaction")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private float interactDistance = 4f;
        [SerializeField] private LayerMask interactionMask = ~0;
        [SerializeField] private Camera interactionCamera;

        [Header("Prompt")]
        [SerializeField] private bool drawPrompt = true;
        [SerializeField] private Vector2 promptSize = new Vector2(360f, 28f);
        [SerializeField] private float promptYOffset = 64f;

        private IQuantumInteractable currentTarget;
        private string currentPrompt = string.Empty;

        public Transform PlayerTransform => GetMovementRoot();

        private void Awake()
        {
            if (interactionCamera == null)
            {
                interactionCamera = GetComponent<Camera>();
            }

            if (interactionCamera == null)
            {
                interactionCamera = Camera.main;
            }

            if (interactionCamera == null)
            {
                Debug.LogWarning($"{nameof(SimplePlayerInteractor)} on {name} could not find an interaction camera.", this);
            }
        }

        public void Configure(Camera targetCamera, float distance)
        {
            interactionCamera = targetCamera;
            interactDistance = distance;
        }

        private void Update()
        {
            RefreshTarget();

            if (currentTarget != null && Input.GetKeyDown(interactKey))
            {
                Debug.Log($"{nameof(SimplePlayerInteractor)} on {name} interacted with {currentTarget.GetType().Name}.", this);
                currentTarget.Interact(this);
            }
        }

        public void TeleportTo(Transform targetPoint)
        {
            if (targetPoint == null)
            {
                Debug.LogWarning($"{nameof(SimplePlayerInteractor)} on {name} cannot teleport because the target point is missing.", this);
                return;
            }

            var movementRoot = GetMovementRoot();
            var bodyController = GetComponentInParent<CharacterController>();
            if (bodyController != null && movementRoot != null)
            {
                var cameraHeight = transform.localPosition.y;
                movementRoot.position = targetPoint.position - new Vector3(0f, cameraHeight, 0f);
                movementRoot.rotation = Quaternion.Euler(0f, targetPoint.eulerAngles.y, 0f);
                transform.localRotation = Quaternion.Euler(targetPoint.eulerAngles.x, 0f, 0f);
                Debug.Log($"{nameof(SimplePlayerInteractor)} on {name} teleported capsule body {movementRoot.name} to {targetPoint.name}.", movementRoot);
                return;
            }

            if (movementRoot != null && movementRoot != transform)
            {
                movementRoot.position = targetPoint.position;
                movementRoot.rotation = targetPoint.rotation;
                Debug.Log($"{nameof(SimplePlayerInteractor)} on {name} teleported movement root {movementRoot.name} to {targetPoint.name}.", movementRoot);
                return;
            }

            transform.position = targetPoint.position;
            transform.rotation = targetPoint.rotation;
            Debug.Log($"{nameof(SimplePlayerInteractor)} on {name} teleported to {targetPoint.name}.", this);
        }

        private void RefreshTarget()
        {
            currentTarget = null;
            currentPrompt = string.Empty;

            if (interactionCamera == null)
            {
                return;
            }

            var ray = new Ray(interactionCamera.transform.position, interactionCamera.transform.forward);
            if (!Physics.Raycast(ray, out var hit, interactDistance, interactionMask, QueryTriggerInteraction.Collide))
            {
                return;
            }

            var interactable = FindInteractable(hit.collider.transform);
            if (interactable == null || !interactable.CanInteract(this))
            {
                return;
            }

            currentTarget = interactable;
            currentPrompt = interactable.GetInteractionPrompt(this);
        }

        private Transform GetMovementRoot()
        {
            var bodyController = GetComponentInParent<CharacterController>();
            if (bodyController != null)
            {
                return bodyController.transform;
            }

            return transform.parent != null ? transform.parent : transform;
        }

        private static IQuantumInteractable FindInteractable(Transform current)
        {
            while (current != null)
            {
                var behaviours = current.GetComponents<MonoBehaviour>();
                for (var index = 0; index < behaviours.Length; index++)
                {
                    if (behaviours[index] is IQuantumInteractable interactable)
                    {
                        return interactable;
                    }
                }

                current = current.parent;
            }

            return null;
        }

        private void OnGUI()
        {
            if (!drawPrompt || string.IsNullOrWhiteSpace(currentPrompt))
            {
                return;
            }

            var rect = new Rect(
                (Screen.width * 0.5f) - (promptSize.x * 0.5f),
                Screen.height - promptYOffset,
                promptSize.x,
                promptSize.y);

            GUI.Label(rect, currentPrompt);
        }
    }
}
