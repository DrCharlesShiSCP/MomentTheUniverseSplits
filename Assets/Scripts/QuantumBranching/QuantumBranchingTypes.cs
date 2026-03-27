using UnityEngine;

namespace QuantumBranching
{
    public enum BranchID
    {
        Branch01 = 0,
        Branch02 = 1,
        Branch03 = 2,
        Branch04 = 3
    }

    public enum BranchOutcomeType
    {
        Stabilized = 0,
        Destabilized = 1,
        Duplicated = 2,
        NullState = 3
    }

    public enum EntanglementState
    {
        StableSignal = 0,
        GlitchSignal = 1,
        DualSignal = 2,
        NullSignal = 3
    }

    public static class QuantumBranchingUtility
    {
        public static EntanglementState ToEntanglementState(this BranchOutcomeType outcome)
        {
            return outcome switch
            {
                BranchOutcomeType.Stabilized => EntanglementState.StableSignal,
                BranchOutcomeType.Destabilized => EntanglementState.GlitchSignal,
                BranchOutcomeType.Duplicated => EntanglementState.DualSignal,
                _ => EntanglementState.NullSignal
            };
        }

        public static void ApplyEmission(this Renderer renderer, Color color, float intensity = 1.5f)
        {
            if (renderer == null)
            {
                return;
            }

            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            block.SetColor("_EmissionColor", color * intensity);
            renderer.SetPropertyBlock(block);
        }
    }
}
