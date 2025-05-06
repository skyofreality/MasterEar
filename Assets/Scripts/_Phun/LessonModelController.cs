using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using DG.Tweening;

namespace NudleNexus.Classroom
{
    public class LessonModelController : MonoBehaviour
    {
        [SerializeField] PickablePart wholePart;
        [SerializeField] List<PickablePart> partList = new List<PickablePart>();

        private Vector3 originalPosition;
        private Vector3 originalScale;

        private Dictionary<PickablePart, Vector3> explodedPositions = new Dictionary<PickablePart, Vector3>();

        // Cache original positions of each part for resetting
        private Dictionary<PickablePart, Vector3> originalPartPositions = new Dictionary<PickablePart, Vector3>();

        private Dictionary<PickablePart, Quaternion> originalPartRotations = new Dictionary<PickablePart, Quaternion>();
        private Dictionary<PickablePart, Vector3> originalPartScales = new Dictionary<PickablePart, Vector3>();





        // New explosion parameters
        public float explosionForce = 5f; // How far the parts should move
        public float explosionDuration = 1f; // How long the explosion lasts

        ModelInteractionMode currentMode;
        Coroutine applyRoutine;
        bool isExploded;

        void Start()
        {
            originalPosition = transform.position;
            originalScale = transform.localScale;

            CacheOriginalPositions();  // Cache the original positions of the parts
        }

        void Reset()
        {
            FetchPartsFromChildren();
        }

        void Awake()
        {
            ForceSetInteractionMode(ModelInteractionMode.Disabled);
        }

        [Button(Expanded = true)]
        public void SetInteractionMode(ModelInteractionMode mode)
        {
            if (mode == currentMode)
                return;

            ForceSetInteractionMode(mode);
        }

        public List<string> GetChildPartNames()
        {
            List<string> partNames = new List<string>();
            foreach (var part in partList)
            {
                // Use the GameObject name (or a custom name if available)
                partNames.Add(part.gameObject.name);
            }
            return partNames;
        }


        void ForceSetInteractionMode(ModelInteractionMode mode)
        {
            currentMode = mode;
            if (applyRoutine != null)
                StopCoroutine(applyRoutine);

            applyRoutine = StartCoroutine(ApplyInteractionModeRoutine());
        }

        IEnumerator ApplyInteractionModeRoutine()
        {
            yield return new WaitForEndOfFrame();
            switch (currentMode)
            {
                case ModelInteractionMode.WholeModel:
                    wholePart.SetIsColliderEnabled(true);
                    partList.ForEach(p => p.SetIsColliderEnabled(false));
                    break;
                case ModelInteractionMode.SmallParts:
                    wholePart.SetIsColliderEnabled(false);
                    partList.ForEach(p => p.SetIsColliderEnabled(true));
                    break;
                case ModelInteractionMode.Disabled:
                    wholePart.SetIsColliderEnabled(false);
                    partList.ForEach(p => p.SetIsColliderEnabled(false));
                    break;
            }
        }

        [Button]
        void FetchPartsFromChildren()
        {
            partList.Clear();
            if (wholePart != null)
            {
                partList.AddRange(wholePart.GetComponentsInChildren<PickablePart>());
                partList.Remove(wholePart);
            }
            else
            {
                Debug.LogError("Cannot fetch parts from children when there is no whole part");
            }

            CacheOriginalPositions(); // Ensure positions and rotations are cached
            SafeEditorHelper.SetDirty(this);
        }

        [Button]
        public void ToggleExplodedView()
        {
            if (partList == null || partList.Count == 0) return;
            if (isExploded)
            {
                Debug.Log("Reset Exploded View");
                // Reset to exploded position (collapse the explosion)
                ResetExplosion();
            }
            else
            {
                Debug.Log("Trigger Exploded View");
                // Trigger explosion to move parts away from the center
                Explode();
            }
        }

        // Controlled Explosion
        public void Explode()
        {
            foreach (var part in partList)
            {
                // Store the current position before explosion
                explodedPositions[part] = part.transform.position;

                // Calculate target position by adding explosion direction and offset
                Vector3 originalPos = originalPartPositions[part]; // Use original position
                Vector3 targetPosition = originalPos +
                                         part.ExplosionDirection.normalized * explosionForce +
                                         Vector3.Scale(part.ExplosionOffset, new Vector3(explosionForce, explosionForce, explosionForce));

                // Animate the movement using DOTween
                part.transform.DOMove(targetPosition, explosionDuration)
                    .SetEase(Ease.OutQuad);
            }

            isExploded = true;
        }

        public void ResetExplosion()
        {
            foreach (var part in partList)
            {
                if (originalPartPositions.TryGetValue(part, out Vector3 originalPos) &&
                    originalPartRotations.TryGetValue(part, out Quaternion originalRot) &&
                    originalPartScales.TryGetValue(part, out Vector3 originalScale))
                {
                    part.transform.DOMove(originalPos, explosionDuration).SetEase(Ease.InQuad);
                    part.transform.DORotateQuaternion(originalRot, explosionDuration).SetEase(Ease.InQuad);
                    part.transform.DOScale(originalScale, explosionDuration).SetEase(Ease.InQuad); // Restore scale
                }
            }

            explodedPositions.Clear();
            isExploded = false;
        }


        // Cache the original positions of the parts
        private void CacheOriginalPositions()
        {
            foreach (var part in partList)
            {
                originalPartPositions[part] = part.transform.position;
                originalPartRotations[part] = part.transform.rotation;
                originalPartScales[part] = part.transform.localScale; // Store original scale
            }
        }

        // Ensure that the explosion positions are cleared when model is reset
        public void ResetModel()
        {
            // Reset the exploded view if applicable
            ResetExplosion();
            foreach (var part in partList)
            {
                if (originalPartScales.TryGetValue(part, out Vector3 originalScale))
                {
                    part.transform.localScale = originalScale; // Reset scale instantly
                }
            }

            // Reset position, rotation, and scale
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = originalScale;  // Ensure originalScale is set in LessonModelController

            // Reset other custom states as needed, for example, exploded state
            isExploded = false;

            Debug.Log("Model has been reset.");
        }

        public PickablePart GetPartByIndex(int index)
        {
            if (index >= 0 && index < partList.Count)
                return partList[index];
            return null;
        }

        public void BringPartToFront(PickablePart part)
        {
            if (part == null)
                return;

            // Determine a target position relative to the main camera (adjust the distance as needed)
            Transform camTransform = Camera.main.transform;
            Vector3 targetPosition = camTransform.position + camTransform.forward * 0.4f; 

            // Animate the part to the target position over 1 second
            part.transform.DOMove(targetPosition, 1f).SetEase(Ease.OutQuad);

            // Optionally, rotate the part to face the camera
            Quaternion targetRotation = Quaternion.LookRotation(-camTransform.forward);
            part.transform.DORotateQuaternion(targetRotation, 1f).SetEase(Ease.OutQuad);
        }
    }

    public enum ModelInteractionMode
    {
        WholeModel, SmallParts, Disabled
    }
}
