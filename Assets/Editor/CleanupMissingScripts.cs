using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class CleanupMissingScripts
{
    [MenuItem("Mage/Clean Missing Script References")]
    public static void Clean()
    {
        int removed = 0;
        var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in allObjects)
        {
            if (!go.scene.IsValid()) continue;

            var so = new SerializedObject(go);
            var sp = so.FindProperty("m_Component");
            if (sp == null) continue;

            for (int i = sp.arraySize - 1; i >= 0; i--)
            {
                var comp = sp.GetArrayElementAtIndex(i);
                var compRef = comp.FindPropertyRelative("component");
                if (compRef != null && compRef.objectReferenceValue == null)
                {
                    sp.DeleteArrayElementAtIndex(i);
                    removed++;
                }
            }
            so.ApplyModifiedProperties();
        }

        if (removed > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[Cleanup] Removed {removed} missing script references");
            EditorUtility.DisplayDialog("Cleanup Done", $"Removed {removed} missing script references.", "OK");
        }
        else
        {
            Debug.Log("[Cleanup] No missing script references found");
        }
    }
}
