using System;
using UnityEngine;

namespace QuantumBranching
{
    [Serializable]
    public class EntanglementVisualState
    {
        public BranchOutcomeType outcome;
        public GameObject[] primaryObjects;
        public GameObject[] secondaryObjects;
        public Renderer[] tintRenderers;
        public Light[] tintLights;
        public string monitorText;
        public Color signalColor = Color.cyan;
    }

    public class EntangledPair : MonoBehaviour
    {
        [Header("Anchors")]
        [SerializeField] private Transform primaryAnchor;
        [SerializeField] private Transform secondaryAnchor;

        [Header("Display")]
        [SerializeField] private TextMesh monitorText;
        [SerializeField] private TextMesh ruleText;
        [SerializeField] private LineRenderer entanglementLine;
        [SerializeField] private string ruleDescription = "Entangled rule: the monitor mirrors the measured core.";

        [Header("Authored States")]
        [SerializeField] private EntanglementVisualState[] authoredStates = new EntanglementVisualState[0];

        public void Configure(
            Transform primary,
            Transform secondary,
            TextMesh monitorLabel,
            TextMesh ruleLabel,
            LineRenderer lineRenderer,
            string rule,
            EntanglementVisualState[] states)
        {
            primaryAnchor = primary;
            secondaryAnchor = secondary;
            monitorText = monitorLabel;
            ruleText = ruleLabel;
            entanglementLine = lineRenderer;
            ruleDescription = rule;
            authoredStates = states;
        }

        private void Start()
        {
            HideAllVisuals();
            UpdateLine();
        }

        private void LateUpdate()
        {
            UpdateLine();
        }

        public void ApplyState(BranchOutcomeType outcome)
        {
            HideAllVisuals();

            EntanglementVisualState selectedState = null;
            for (var index = 0; index < authoredStates.Length; index++)
            {
                if (authoredStates[index].outcome == outcome)
                {
                    selectedState = authoredStates[index];
                    break;
                }
            }

            if (selectedState == null)
            {
                Debug.LogWarning($"{nameof(EntangledPair)} on {name} has no authored state for {outcome}.", this);
                return;
            }

            SetObjectsActive(selectedState.primaryObjects, true);
            SetObjectsActive(selectedState.secondaryObjects, true);

            for (var index = 0; index < selectedState.tintRenderers.Length; index++)
            {
                if (selectedState.tintRenderers[index] != null)
                {
                    selectedState.tintRenderers[index].ApplyEmission(selectedState.signalColor, 1.8f);
                }
            }

            for (var index = 0; index < selectedState.tintLights.Length; index++)
            {
                if (selectedState.tintLights[index] == null)
                {
                    continue;
                }

                selectedState.tintLights[index].enabled = true;
                selectedState.tintLights[index].color = selectedState.signalColor;
            }

            if (monitorText != null)
            {
                monitorText.text = selectedState.monitorText;
                monitorText.color = selectedState.signalColor;
            }

            if (ruleText != null)
            {
                var entanglementState = outcome.ToEntanglementState();
                ruleText.text = $"{ruleDescription}\nConstraint: {entanglementState}";
                ruleText.color = selectedState.signalColor;
            }

            if (entanglementLine != null)
            {
                entanglementLine.enabled = true;
                entanglementLine.startColor = selectedState.signalColor;
                entanglementLine.endColor = selectedState.signalColor;
            }

            Debug.Log($"{nameof(EntangledPair)} on {name} applied {outcome}.", this);
        }

        private void HideAllVisuals()
        {
            for (var index = 0; index < authoredStates.Length; index++)
            {
                SetObjectsActive(authoredStates[index].primaryObjects, false);
                SetObjectsActive(authoredStates[index].secondaryObjects, false);

                for (var lightIndex = 0; lightIndex < authoredStates[index].tintLights.Length; lightIndex++)
                {
                    if (authoredStates[index].tintLights[lightIndex] != null)
                    {
                        authoredStates[index].tintLights[lightIndex].enabled = false;
                    }
                }
            }

            if (entanglementLine != null)
            {
                entanglementLine.enabled = false;
            }
        }

        private void UpdateLine()
        {
            if (entanglementLine == null || primaryAnchor == null || secondaryAnchor == null)
            {
                return;
            }

            entanglementLine.positionCount = 2;
            entanglementLine.SetPosition(0, primaryAnchor.position);
            entanglementLine.SetPosition(1, secondaryAnchor.position);
        }

        private static void SetObjectsActive(GameObject[] targets, bool shouldBeActive)
        {
            if (targets == null)
            {
                return;
            }

            for (var index = 0; index < targets.Length; index++)
            {
                if (targets[index] != null)
                {
                    targets[index].SetActive(shouldBeActive);
                }
            }
        }
    }
}
