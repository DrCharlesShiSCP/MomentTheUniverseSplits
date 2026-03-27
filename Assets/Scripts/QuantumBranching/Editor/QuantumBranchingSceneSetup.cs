using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace QuantumBranching.Editor
{
    public static class QuantumBranchingSceneSetup
    {
        private const string ScenePath = "Assets/Scenes/Scene_Laboratory.unity";
        private const string NotePath = "Assets/Scenes/QuantumBranching_SetupNotes.txt";

        [MenuItem("Quantum Branching/Open Lab Scene")]
        public static void OpenLabScene()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Quantum Branching/Select Setup Note")]
        public static void SelectSetupNote()
        {
            var note = AssetDatabase.LoadAssetAtPath<Object>(NotePath);
            if (note == null)
            {
                Debug.LogWarning($"Setup note not found at {NotePath}");
                return;
            }

            Selection.activeObject = note;
            EditorGUIUtility.PingObject(note);
        }
    }
}
