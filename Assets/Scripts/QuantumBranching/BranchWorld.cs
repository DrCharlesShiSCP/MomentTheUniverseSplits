using UnityEngine;

namespace QuantumBranching
{
    public class BranchWorld : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private BranchID branchId;
        [SerializeField] private BranchOutcomeType outcomeType;
        [SerializeField] private string branchTitle;
        [SerializeField] private Color branchTint = Color.white;

        [Header("Scene References")]
        [SerializeField] private GameObject roomRoot;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private TextMesh[] titleLabels = new TextMesh[0];
        [SerializeField] private Renderer[] tintRenderers = new Renderer[0];
        [SerializeField] private EntangledPair[] entangledPairs = new EntangledPair[0];

        private bool branchStateApplied;
        private bool warnedAboutMissingRoomRoot;
        private bool warnedAboutMissingSpawnPoint;
        private bool loggedRuntimeSetup;

        public BranchID BranchId => branchId;
        public BranchOutcomeType OutcomeType => outcomeType;
        public Transform SpawnPoint => spawnPoint;
        public string BranchTitle => branchTitle;
        public string ExpectedRoomName => branchId switch
        {
            BranchID.Branch01 => "Room02",
            BranchID.Branch02 => "Room03",
            BranchID.Branch03 => "Room04",
            _ => "Room05"
        };

        private void Awake()
        {
            EnsureRuntimeBranchSetup();
        }

        private void Start()
        {
            EnsureRuntimeBranchSetup();

            var manager = FindFirstObjectByType<BranchVisualManager>();
            if (manager != null && !manager.IsSplitTriggered)
            {
                SetVisible(false);
                Debug.Log($"{nameof(BranchWorld)} {branchTitle} enforced hidden startup state in Start().", this);
            }
        }

        public void Configure(
            BranchID id,
            BranchOutcomeType outcome,
            string title,
            Color tint,
            GameObject targetRoomRoot,
            Transform targetSpawn,
            TextMesh[] labels,
            Renderer[] renderers,
            EntangledPair[] pairs)
        {
            branchId = id;
            outcomeType = outcome;
            branchTitle = title;
            branchTint = tint;
            roomRoot = targetRoomRoot;
            spawnPoint = targetSpawn;
            titleLabels = labels;
            tintRenderers = renderers;
            entangledPairs = pairs;
        }

        public void ResolveRoomRoot()
        {
            TryAutoAssignRoomRoot();
        }

        public void SetVisible(bool isVisible)
        {
            EnsureRuntimeBranchSetup();

            if (roomRoot != null)
            {
                roomRoot.SetActive(isVisible);
                Debug.Log($"{nameof(BranchWorld)} {branchTitle} visibility set to {isVisible} on {roomRoot.name}.", this);
            }
            else
            {
                Debug.LogWarning($"{nameof(BranchWorld)} {branchTitle} has no room root to toggle.", this);
            }

            if (isVisible)
            {
                ApplyBranchState();
            }
            else
            {
                branchStateApplied = false;
            }
        }

        public void ApplyBranchState()
        {
            EnsureRuntimeBranchSetup();

            if (branchStateApplied)
            {
                return;
            }

            for (var index = 0; index < titleLabels.Length; index++)
            {
                if (titleLabels[index] != null)
                {
                    titleLabels[index].text = $"{branchTitle}\n{outcomeType}";
                    titleLabels[index].color = branchTint;
                }
            }

            for (var index = 0; index < tintRenderers.Length; index++)
            {
                if (tintRenderers[index] != null)
                {
                    tintRenderers[index].ApplyEmission(branchTint, 1.5f);
                }
            }

            for (var index = 0; index < entangledPairs.Length; index++)
            {
                if (entangledPairs[index] != null)
                {
                    entangledPairs[index].ApplyState(outcomeType);
                }
            }

            branchStateApplied = true;
            Debug.Log($"{nameof(BranchWorld)} applied {outcomeType} state for {branchTitle}.", this);
        }

        private void EnsureRuntimeBranchSetup()
        {
            ApplyDefaultIdentity();
            TryAutoAssignRoomRoot();

            if (roomRoot == null)
            {
                if (!warnedAboutMissingRoomRoot)
                {
                    Debug.LogWarning($"{nameof(BranchWorld)} {name} could not find a room root for {branchId}. Expected {ExpectedRoomName}.", this);
                    warnedAboutMissingRoomRoot = true;
                }

                return;
            }

            if (spawnPoint == null)
            {
                var spawn = roomRoot.transform.Find("QuantumBranchSpawn");
                if (spawn == null)
                {
                    spawn = new GameObject("QuantumBranchSpawn").transform;
                    spawn.SetParent(roomRoot.transform, false);
                    spawn.localPosition = new Vector3(0f, 1.5f, -2.2f);
                    spawn.localRotation = Quaternion.identity;
                }

                spawnPoint = spawn;
            }

            if (spawnPoint == null)
            {
                if (!warnedAboutMissingSpawnPoint)
                {
                    Debug.LogWarning($"{nameof(BranchWorld)} {branchTitle} has no spawn point.", this);
                    warnedAboutMissingSpawnPoint = true;
                }
                return;
            }

            if (entangledPairs != null && entangledPairs.Length > 0 && entangledPairs[0] != null)
            {
                return;
            }

            var rootName = $"RuntimeBranchDisplay_{branchId}";
            var branchDisplay = roomRoot.transform.Find(rootName);
            if (branchDisplay == null)
            {
                branchDisplay = new GameObject(rootName).transform;
                branchDisplay.SetParent(roomRoot.transform, false);
                branchDisplay.localPosition = Vector3.zero;
                branchDisplay.localRotation = Quaternion.identity;
            }

            var pair = branchDisplay.GetComponent<EntangledPair>();
            if (pair == null)
            {
                pair = branchDisplay.gameObject.AddComponent<EntangledPair>();
            }

            var coreAnchor = EnsureChild(branchDisplay, "CoreAnchor", new Vector3(0f, 1.2f, 0f));
            var monitorAnchor = EnsureChild(branchDisplay, "MonitorAnchor", new Vector3(1.35f, 1.05f, -0.75f));
            var monitorLabel = EnsureMonitorLabel(monitorAnchor);
            var visuals = BuildOutcomeVisuals(coreAnchor, monitorAnchor);

            var state = new EntanglementVisualState
            {
                outcome = outcomeType,
                primaryObjects = visuals.PrimaryObjects,
                secondaryObjects = visuals.SecondaryObjects,
                tintRenderers = visuals.Renderers,
                tintLights = new Light[0],
                monitorText = GetMonitorText(),
                signalColor = branchTint
            };

            pair.Configure(coreAnchor, monitorAnchor, monitorLabel, null, null, GetRuleText(), new[] { state });
            entangledPairs = new[] { pair };
            tintRenderers = visuals.Renderers;

            if (!loggedRuntimeSetup)
            {
                Debug.Log($"{nameof(BranchWorld)} created runtime setup for {branchTitle} in {roomRoot.name}.", this);
                loggedRuntimeSetup = true;
            }
        }

        private void ApplyDefaultIdentity()
        {
            if (!string.IsNullOrWhiteSpace(branchTitle) && !branchTitle.StartsWith("BranchWorld_"))
            {
                return;
            }

            switch (outcomeType)
            {
                case BranchOutcomeType.Stabilized:
                    branchTitle = "Stabilized Branch";
                    branchTint = new Color(0.35f, 1f, 0.65f, 1f);
                    break;
                case BranchOutcomeType.Destabilized:
                    branchTitle = "Destabilized Branch";
                    branchTint = new Color(1f, 0.35f, 0.35f, 1f);
                    break;
                case BranchOutcomeType.Duplicated:
                    branchTitle = "Duplicated Branch";
                    branchTint = new Color(0.35f, 0.95f, 1f, 1f);
                    break;
                default:
                    branchTitle = "Null-State Branch";
                    branchTint = new Color(0.6f, 0.7f, 0.95f, 1f);
                    break;
            }
        }

        private void TryAutoAssignRoomRoot()
        {
            if (roomRoot != null)
            {
                return;
            }

            roomRoot = FindSceneObjectByName(ExpectedRoomName);
            if (roomRoot != null)
            {
                Debug.Log($"{nameof(BranchWorld)} auto-assigned {roomRoot.name} to {branchTitle}.", this);
            }
        }

        private static GameObject FindSceneObjectByName(string targetName)
        {
            var activeObject = GameObject.Find(targetName);
            if (activeObject != null)
            {
                return activeObject;
            }

            var allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
            for (var index = 0; index < allTransforms.Length; index++)
            {
                var current = allTransforms[index];
                if (current == null || current.name != targetName)
                {
                    continue;
                }

                if (current.gameObject.scene.IsValid())
                {
                    return current.gameObject;
                }
            }

            return null;
        }

        private OutcomeVisuals BuildOutcomeVisuals(Transform coreAnchor, Transform monitorAnchor)
        {
            switch (outcomeType)
            {
                case BranchOutcomeType.Stabilized:
                    return new OutcomeVisuals(
                        new[]
                        {
                            CreateVisualPrimitive(PrimitiveType.Sphere, "StableCore", coreAnchor, Vector3.zero, Vector3.zero, new Vector3(0.5f, 0.5f, 0.5f))
                        },
                        CreateMonitorStand(monitorAnchor, "StableMonitor"));

                case BranchOutcomeType.Destabilized:
                    return new OutcomeVisuals(
                        new[]
                        {
                            CreateVisualPrimitive(PrimitiveType.Capsule, "UnstableCore", coreAnchor, Vector3.zero, new Vector3(18f, 20f, 42f), new Vector3(0.38f, 0.72f, 0.38f)),
                            CreateVisualPrimitive(PrimitiveType.Cube, "GlitchFragment", coreAnchor, new Vector3(0.25f, 0.15f, 0f), new Vector3(20f, 15f, 35f), new Vector3(0.1f, 0.1f, 0.1f))
                        },
                        CreateMonitorStand(monitorAnchor, "WarningMonitor"));

                case BranchOutcomeType.Duplicated:
                    return new OutcomeVisuals(
                        new[]
                        {
                            CreateVisualPrimitive(PrimitiveType.Sphere, "DuplicateCore_A", coreAnchor, new Vector3(-0.26f, 0f, 0f), Vector3.zero, new Vector3(0.38f, 0.38f, 0.38f)),
                            CreateVisualPrimitive(PrimitiveType.Sphere, "DuplicateCore_B", coreAnchor, new Vector3(0.26f, 0f, 0f), Vector3.zero, new Vector3(0.38f, 0.38f, 0.38f))
                        },
                        CreateMonitorStand(monitorAnchor, "DualMonitor"));

                default:
                    return new OutcomeVisuals(
                        new[]
                        {
                            CreateVisualPrimitive(PrimitiveType.Cylinder, "NullField", coreAnchor, new Vector3(0f, -0.2f, 0f), Vector3.zero, new Vector3(0.65f, 0.03f, 0.65f))
                        },
                        CreateMonitorStand(monitorAnchor, "NullMonitor"));
            }
        }

        private string GetRuleText()
        {
            return outcomeType switch
            {
                BranchOutcomeType.Stabilized => "Entangled rule: a stable core forces a stable green monitor.",
                BranchOutcomeType.Destabilized => "Entangled rule: an unstable core forces a red warning monitor.",
                BranchOutcomeType.Duplicated => "Entangled rule: duplicated cores force mirrored dual monitor signals.",
                _ => "Entangled rule: a vanished core forces a null monitor state."
            };
        }

        private string GetMonitorText()
        {
            return outcomeType switch
            {
                BranchOutcomeType.Stabilized => "STABLE",
                BranchOutcomeType.Destabilized => "FAULT",
                BranchOutcomeType.Duplicated => "DUAL",
                _ => "NULL"
            };
        }

        private GameObject[] CreateMonitorStand(Transform monitorAnchor, string namePrefix)
        {
            return new[]
            {
                CreateVisualPrimitive(PrimitiveType.Cube, $"{namePrefix}_Body", monitorAnchor, new Vector3(0f, -0.02f, 0f), Vector3.zero, new Vector3(0.62f, 0.4f, 0.08f)),
                CreateVisualPrimitive(PrimitiveType.Cube, $"{namePrefix}_Stem", monitorAnchor, new Vector3(0f, -0.28f, 0f), Vector3.zero, new Vector3(0.08f, 0.18f, 0.08f)),
                CreateVisualPrimitive(PrimitiveType.Cube, $"{namePrefix}_Base", monitorAnchor, new Vector3(0f, -0.38f, 0f), Vector3.zero, new Vector3(0.24f, 0.04f, 0.18f))
            };
        }

        private TextMesh EnsureMonitorLabel(Transform monitorAnchor)
        {
            var labelTransform = monitorAnchor.Find("MonitorLabel");
            if (labelTransform == null)
            {
                labelTransform = new GameObject("MonitorLabel").transform;
                labelTransform.SetParent(monitorAnchor, false);
                labelTransform.localPosition = new Vector3(0f, 0f, -0.05f);
                labelTransform.localRotation = Quaternion.identity;
            }

            var label = labelTransform.GetComponent<TextMesh>();
            if (label == null)
            {
                label = labelTransform.gameObject.AddComponent<TextMesh>();
            }

            label.text = GetMonitorText();
            label.characterSize = 0.05f;
            label.fontSize = 42;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = branchTint;
            return label;
        }

        private GameObject CreateVisualPrimitive(
            PrimitiveType primitiveType,
            string name,
            Transform parent,
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

        private static Transform EnsureChild(Transform parent, string name, Vector3 localPosition)
        {
            var child = parent.Find(name);
            if (child != null)
            {
                return child;
            }

            child = new GameObject(name).transform;
            child.SetParent(parent, false);
            child.localPosition = localPosition;
            child.localRotation = Quaternion.identity;
            return child;
        }

        private readonly struct OutcomeVisuals
        {
            public OutcomeVisuals(GameObject[] primaryObjects, GameObject[] secondaryObjects)
            {
                PrimaryObjects = primaryObjects;
                SecondaryObjects = secondaryObjects;

                Renderers = new Renderer[primaryObjects.Length + secondaryObjects.Length];
                var index = 0;
                for (var primaryIndex = 0; primaryIndex < primaryObjects.Length; primaryIndex++)
                {
                    Renderers[index++] = primaryObjects[primaryIndex].GetComponent<Renderer>();
                }

                for (var secondaryIndex = 0; secondaryIndex < secondaryObjects.Length; secondaryIndex++)
                {
                    Renderers[index++] = secondaryObjects[secondaryIndex].GetComponent<Renderer>();
                }
            }

            public GameObject[] PrimaryObjects { get; }
            public GameObject[] SecondaryObjects { get; }
            public Renderer[] Renderers { get; }
        }
    }
}
