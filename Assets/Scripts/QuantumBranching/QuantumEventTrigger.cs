using UnityEngine;

namespace QuantumBranching
{
    public class QuantumEventTrigger : MonoBehaviour, IQuantumInteractable
    {
        [Header("Setup")]
        [SerializeField] private BranchVisualManager visualManager;
        [SerializeField] private Transform rotatingCore;
        [SerializeField] private string interactPrompt = "Press E to Measure";

        [Header("Idle Motion")]
        [SerializeField] private float idleSpinSpeed = 45f;
        [SerializeField] private float idleBobHeight = 0.08f;
        [SerializeField] private float idleBobSpeed = 1.2f;

        [Header("Split Feedback")]
        [SerializeField] private Light[] pulseLights = new Light[0];
        [SerializeField] private Renderer[] emissiveRenderers = new Renderer[0];

        private bool hasTriggered;
        private Vector3 baseLocalPosition;
        private bool loggedRuntimePresentation;

        public void Configure(BranchVisualManager manager, Transform corePivot, Light[] lights, Renderer[] renderers)
        {
            visualManager = manager;
            rotatingCore = corePivot;
            pulseLights = lights;
            emissiveRenderers = renderers;
        }

        private void Awake()
        {
            if (visualManager == null)
            {
                Debug.LogWarning($"{nameof(QuantumEventTrigger)} on {name} is missing a {nameof(BranchVisualManager)} reference.", this);
            }

            EnsureRuntimePresentation();

            if (rotatingCore == null)
            {
                rotatingCore = transform;
            }

            baseLocalPosition = rotatingCore.localPosition;
        }

        private void Update()
        {
            if (rotatingCore == null)
            {
                return;
            }

            rotatingCore.Rotate(Vector3.up, idleSpinSpeed * Time.deltaTime, Space.Self);
            var bobOffset = Mathf.Sin(Time.time * idleBobSpeed) * idleBobHeight;
            rotatingCore.localPosition = baseLocalPosition + (Vector3.up * bobOffset);

            if (hasTriggered)
            {
                for (var index = 0; index < pulseLights.Length; index++)
                {
                    if (pulseLights[index] == null)
                    {
                        continue;
                    }

                    pulseLights[index].intensity = Mathf.Lerp(pulseLights[index].intensity, 2f, Time.deltaTime * 2f);
                }
            }
        }

        public bool CanInteract(SimplePlayerInteractor interactor)
        {
            return !hasTriggered;
        }

        public string GetInteractionPrompt(SimplePlayerInteractor interactor)
        {
            return hasTriggered ? "Measurement recorded" : interactPrompt;
        }

        public void Interact(SimplePlayerInteractor interactor)
        {
            if (hasTriggered)
            {
                Debug.Log($"{nameof(QuantumEventTrigger)} on {name} ignored duplicate interaction.", this);
                return;
            }

            hasTriggered = true;
            Debug.Log($"{nameof(QuantumEventTrigger)} on {name} triggered the quantum measurement event.", this);

            for (var index = 0; index < emissiveRenderers.Length; index++)
            {
                if (emissiveRenderers[index] == null)
                {
                    continue;
                }

                emissiveRenderers[index].ApplyEmission(Color.white, 4f);
            }

            if (visualManager != null)
            {
                visualManager.TriggerSplit();
            }
            else
            {
                Debug.LogWarning($"{nameof(QuantumEventTrigger)} on {name} cannot trigger the split because the visual manager reference is missing.", this);
            }
        }

        private void EnsureRuntimePresentation()
        {
            EnsureInteractionCollider();

            if (rotatingCore != null && pulseLights.Length > 0 && emissiveRenderers.Length > 0)
            {
                return;
            }

            var rigRoot = transform.Find("RuntimeExperimentRig");
            if (rigRoot == null)
            {
                rigRoot = new GameObject("RuntimeExperimentRig").transform;
                rigRoot.SetParent(transform, false);
            }

            var pedestal = EnsurePrimitive(rigRoot, PrimitiveType.Cylinder, "ExperimentPedestal", new Vector3(0f, 0.85f, 0f), Vector3.zero, new Vector3(0.9f, 0.12f, 0.9f));
            var core = EnsurePrimitive(rigRoot, PrimitiveType.Sphere, "ExperimentCore", new Vector3(0f, 1.45f, 0f), Vector3.zero, new Vector3(0.5f, 0.5f, 0.5f));
            var ring = EnsurePrimitive(rigRoot, PrimitiveType.Cylinder, "ExperimentField", new Vector3(0f, 1.15f, 0f), Vector3.zero, new Vector3(1.15f, 0.02f, 1.15f));

            var pedestalRenderer = pedestal.GetComponent<Renderer>();
            if (pedestalRenderer != null)
            {
                pedestalRenderer.ApplyEmission(new Color(0.65f, 0.75f, 0.9f, 1f), 0.3f);
            }

            var ringRenderer = ring.GetComponent<Renderer>();
            if (ringRenderer != null)
            {
                ringRenderer.ApplyEmission(new Color(0.2f, 0.9f, 1f, 1f), 1.25f);
            }

            var coreRenderer = core.GetComponent<Renderer>();
            if (coreRenderer != null)
            {
                coreRenderer.ApplyEmission(new Color(0.35f, 0.95f, 1f, 1f), 1.75f);
            }

            var lightTransform = rigRoot.Find("ExperimentLight");
            Light light = null;
            if (lightTransform != null)
            {
                light = lightTransform.GetComponent<Light>();
            }

            if (light == null)
            {
                var lightObject = lightTransform != null ? lightTransform.gameObject : new GameObject("ExperimentLight");
                lightObject.transform.SetParent(rigRoot, false);
                lightObject.transform.localPosition = new Vector3(0f, 1.45f, 0f);
                light = lightObject.GetComponent<Light>();
                if (light == null)
                {
                    light = lightObject.AddComponent<Light>();
                }
            }

            light.type = LightType.Point;
            light.color = new Color(0.35f, 0.95f, 1f, 1f);
            light.intensity = 1.5f;
            light.range = 4f;

            rotatingCore = core.transform;
            pulseLights = new[] { light };
            emissiveRenderers = new[] { coreRenderer, ringRenderer };

            if (!loggedRuntimePresentation)
            {
                Debug.Log($"{nameof(QuantumEventTrigger)} on {name} created its runtime experiment presentation.", this);
                loggedRuntimePresentation = true;
            }
        }

        private void EnsureInteractionCollider()
        {
            var collider = GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<BoxCollider>();
            }

            collider.center = new Vector3(0f, 1.1f, 0f);
            collider.size = new Vector3(2.2f, 2.4f, 2.2f);
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
                Object.Destroy(collider);
            }

            return created;
        }
    }
}
