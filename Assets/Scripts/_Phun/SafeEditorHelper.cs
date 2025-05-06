using NudleNexus.Classroom;
using UnityEngine;

namespace NudleNexus
{
    public static class SafeEditorHelper
    {
        public static void SetDirty(Component target)
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(target);
#endif
        }

        public static void MoveComponentToTop(PickablePart pickablePart)
        {
#if UNITY_EDITOR
            while (UnityEditorInternal.ComponentUtility.MoveComponentUp(pickablePart)){}
#endif
        }
    }
}