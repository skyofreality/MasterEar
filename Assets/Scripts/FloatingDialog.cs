using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

namespace NudleNexus.Classroom
{
    public class FloatingDialog : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text detailText;

        private PickablePart currentPartData = null; // To keep track of the current part being interacted with

        private void Start()
        {
            PickablePart.OnPickUpAnyPart += ShowModelPartData;
            PickablePart.OnReleaseAnyPart += ReleaseModelPartData;  // Changed to ReleaseModelPartData
        }

        private void OnDestroy()
        {
            PickablePart.OnPickUpAnyPart -= ShowModelPartData;
            PickablePart.OnReleaseAnyPart -= ReleaseModelPartData;
        }

        private void ShowModelPartData(PickablePart partData)
        {
            if (partData != null)
            {
                // Only update the text if a new part is picked up
                if (currentPartData != partData)
                {
                    currentPartData = partData;
                    nameText.text = partData.Data.name;
                    detailText.text = partData.Data.detail;
                }
            }
        }

        private void ReleaseModelPartData(PickablePart partData)
        {
            // Don't clear the text when the part is released; only clear when a different part is picked up
            if (currentPartData == partData)
            {
                // We leave the text as it is until another part is picked up
            }
        }
    }
}
