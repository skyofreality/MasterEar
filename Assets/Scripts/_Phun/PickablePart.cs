using System;
using System.Linq;
using DG.Tweening;
using Oculus.Interaction;
using Oculus.Interaction.Grab;
using Oculus.Interaction.HandGrab;
using Sirenix.OdinInspector;
using UnityEngine;
using Grabbable = Oculus.Interaction.Grabbable;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NudleNexus.Classroom
{
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class PickablePart : MonoBehaviour
    {
        public ModelPartData Data => data;
        [SerializeField] private ModelPartData data = new ModelPartData();

        [SerializeField, ReadOnly, ShowIf(nameof(IsPlaying))] private Vector3 originalLocalPosition;
        [SerializeField, ReadOnly, ShowIf(nameof(IsPlaying))] private Vector3 originalLocalEulerAngles;
        [SerializeField, ReadOnly, ShowIf(nameof(IsPlaying))] private Vector3 originalLocalScale;

        [SerializeField, HideInInspector] private BoxCollider boxCollider;
        [SerializeField, HideInInspector] private Rigidbody rigidBody;

        // Add ExplosionOffset field and property
        [SerializeField]
        private Vector3 explosionOffset = Vector3.zero; // Default offset value

        public Vector3 ExplosionOffset
        {
            get => explosionOffset;
            set => explosionOffset = value;
        }

        [SerializeField]
        private Vector3 explosionDirection = Vector3.zero;

        public Vector3 ExplosionDirection
        {
            get => explosionDirection;
            set => explosionDirection = value;
        }

        public static event Action<PickablePart> OnPickUpAnyPart;
        public static event Action<PickablePart> OnReleaseAnyPart;

        private bool isOriginalDataRecorded;
        private bool isGrabbed;

#if UNITY_EDITOR
        [Button]
        public void SetupGrabTarget()
        {
            var template = new Template(
                "ISDK_HandGrabInteraction",
                "6ee61821e0d5b094a8d732834b365b21");
            var result = Instantiate(AssetDatabase.LoadMainAssetAtPath(
                AssetDatabase.GUIDToAssetPath(template.AssetGUID)), null, true) as GameObject;
            if (result == null)
                throw new InvalidOperationException("Could not find an object of type " + template.AssetGUID);

            result.transform.SetParent(transform, false);
            result.name = name + "_GrabTarget";
            Undo.RegisterCreatedObjectUndo(result, "Add " + template.DisplayName);

            result.transform.localPosition = Vector3.zero;
            result.transform.localScale = Vector3.one;
            result.transform.localRotation = Quaternion.identity;

            HandGrabInteractable handInteractable = result.GetComponent<HandGrabInteractable>();
            handInteractable.InjectRigidbody(rigidBody);
            handInteractable.InjectSupportedGrabTypes(GrabTypeFlags.All);

            GrabInteractable grabInteractable = result.GetComponent<GrabInteractable>();
            grabInteractable.InjectRigidbody(rigidBody);
            SafeEditorHelper.SetDirty(grabInteractable);

            var grabbable = result.GetComponent<Grabbable>();
            grabbable.InjectOptionalTargetTransform(transform);
            grabbable.InjectOptionalRigidbody(rigidBody);
            SafeEditorHelper.SetDirty(grabbable);

            var transformer = result.AddComponent<GrabAndReleaseTransformer>();
            transformer.AssignTo(this);

            grabbable.InjectOptionalOneGrabTransformer(transformer);
            grabbable.InjectOptionalTwoGrabTransformer(transformer);
            grabbable.InjectOptionalTargetTransform(transform);
        }
#endif

        protected bool IsPlaying => Application.isPlaying;

        protected virtual void Reset()
        {
            data.name = gameObject.name;
            boxCollider = GetComponent<BoxCollider>();
            var targetRigidbody = GetComponent<Rigidbody>();
            targetRigidbody.isKinematic = true;
            targetRigidbody.useGravity = false;
            SafeEditorHelper.MoveComponentToTop(this);
        }

        protected virtual void Awake()
        {
            RecordOriginalData();
        }

        private void RecordOriginalData()
        {
            if (isOriginalDataRecorded)
                return;

            originalLocalPosition = transform.localPosition;
            originalLocalScale = transform.localScale;
            originalLocalEulerAngles = transform.localEulerAngles;
            isOriginalDataRecorded = true;
        }

        [Button, ShowIf(nameof(IsPlaying))]
        public void Release(float duration = 0.45f, Ease ease = Ease.OutQuad)
        {
            OnReleaseAnyPart?.Invoke(this);
            if (!isGrabbed)
                return;

            isGrabbed = false;
            Debug.Log("Release : " + data.name);
            RecordOriginalData();
            transform.DOKill();
            transform.DOLocalMove(originalLocalPosition, duration).SetEase(ease);
            transform.DOScale(originalLocalScale, duration).SetEase(ease);
            transform.DOLocalRotate(originalLocalEulerAngles, duration).SetEase(ease);
        }

        [Button, ShowIf(nameof(IsPlaying))]
        public void Grab()
        {
            OnPickUpAnyPart?.Invoke(this);
            if (isGrabbed)
                return;

            isGrabbed = true;
            Debug.Log("Grabbed : " + data.name);
            RecordOriginalData();
            transform.DOKill();
        }

        public void SetIsColliderEnabled(bool value)
        {
            boxCollider.enabled = value;
        }

        [Button, HideIf(nameof(IsPlaying))]
        private void FitToChildren()
        {
            MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>().ToArray();
            if (meshRenderers.Length == 0)
            {
                Debug.LogWarning("No MeshRenderers found.");
                return;
            }

            Bounds combinedBounds = meshRenderers[0].bounds;
            foreach (var renderer in meshRenderers)
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }

            Transform colliderTransform = transform;
            Matrix4x4 worldToLocal = colliderTransform.worldToLocalMatrix;
            Vector3 localCenter = worldToLocal.MultiplyPoint3x4(combinedBounds.center);

            Vector3 localSize = Vector3.Scale(combinedBounds.size, new Vector3(
                1f / colliderTransform.lossyScale.x,
                1f / colliderTransform.lossyScale.y,
                1f / colliderTransform.lossyScale.z));

            boxCollider.center = localCenter;
            boxCollider.size = localSize;
            SafeEditorHelper.SetDirty(boxCollider);
            SafeEditorHelper.SetDirty(this);
        }
    }

    internal class Template
    {
        public readonly string DisplayName;
        public readonly string AssetGUID;

        public Template(string displayName, string assetGUID)
        {
            DisplayName = displayName;
            AssetGUID = assetGUID;
        }
    }

    [Serializable]
    public class ModelPartData
    {
        public string name;
        [TextArea] public string detail;
    }
}
