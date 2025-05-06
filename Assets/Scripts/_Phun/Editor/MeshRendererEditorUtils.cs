using UnityEditor;
using UnityEngine;

namespace NudleNexus.Editor.Utils
{
    public static class MeshRendererEditorUtils
    {
        [MenuItem("GameObject/Center MeshRenderer in New Parent", false, 10)]
        static void CenterMeshRendererInNewParent()
        {
            if (Selection.activeGameObject == null || Selection.activeGameObject.GetComponent<MeshRenderer>() == null)
            {
                Debug.LogWarning("Please select a GameObject with a MeshRenderer.");
                return;
            }

            GameObject selectedObject = Selection.activeGameObject;
            MeshRenderer meshRenderer = selectedObject.GetComponent<MeshRenderer>();

            GameObject parentObject = new GameObject(selectedObject.name + "_Wrapper");
            parentObject.transform.position = meshRenderer.bounds.center;

            selectedObject.transform.SetParent(parentObject.transform);
            //selectedObject.transform.localPosition = Vector3.zero;

            Selection.activeGameObject = parentObject;
        }
    }

}