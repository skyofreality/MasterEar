using System;
using System.Threading.Tasks;
using NudleNexus.Classroom;
using Sirenix.OdinInspector;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections.Generic;

namespace NudleNexus.Classroom
{
    public class LessonManager : MonoBehaviour
    {
        // UI Elements to be updated
        public TextMeshProUGUI lessonTitleText;           // Text component for lesson title
        public TextMeshProUGUI lessonDescriptionText;     // Text component for lesson description
        public Image lessonThumbnailImage;                // Image component for lesson thumbnail

        // Prefabs and containers
        public GameObject lessonButtonPrefab;
        public Transform lessonButtonsContainer;
        public Transform modelsContainer;  // New container for the 3D models

        public CanvasGroup canvas1;
        public CanvasGroup canvas2;
        public GameObject panelInteractable1;
        public GameObject panelInteractable2;
        public GameObject canvasInteractables1;
        public GameObject canvasInteractables2;

        private LessonModelController currentModel;
        private Button currentButton;
        public ExplodedViewButton explodedViewButton;

        public TMP_Dropdown partDropdown;

        // Reference to the VideoPlayerController
        public VideoPlayerController videoPlayerController;

        // Skybox materials for each lesson
        public Material[] skyboxMaterials; // Array of skyboxes for each lesson

        // Array to hold data for all lessons
        [System.Serializable]
        public class LessonData
        {
            public string lessonTitle;          // Title of the lesson
            public string description;          // Description of the lesson
            public float durationInMinutes;     // Duration of the lesson in minutes
            public Sprite thumbnailImage;       // Thumbnail image for the lesson
            public string buttonTitle;          // Title for the button representing the lesson
            public string buttonDescription;    // Description for the button representing the lesson

            public GameObject lesson3DModel;    // 3D model associated with this lesson
            public VideoClip videoClip;         // Video clip associated with this lesson
        }

        public LessonData[] lessons; // Array of lessons to store data for each lesson

        private int currentLessonIndex = 0; // Track the current lesson selected (default to the first lesson)
        private bool isMaximizedMode = false; // Track maximized mode state
        private Vector3 originalModelScale; // Store the original scale of the model
        private Vector3 maximizedModelScale = new Vector3(2f, 2f, 2f); // Scale for maximized mode

        // Define an event that will be triggered when the exploded view button is clicked
        public static event Action OnExplodedViewButtonClicked;

        void Start()
        {
            if (lessons.Length > 0)
            {
                UpdateLessonUI(currentLessonIndex);
                InstantiateLessonButtons();
                ChangeLessonModel(currentLessonIndex);
                PlayFirstLessonVideo();  // Play the video for the first lesson
                SetSkybox(currentLessonIndex);  // Set the skybox for the first lesson
            }

            

            // Ensure the currentModel reference is set
            if (currentModel == null)
            {
                Debug.LogError("currentModel is not set in LessonManager.");
            }

            if (partDropdown != null)
            {
                partDropdown.onValueChanged.AddListener(OnPartDropdownChanged);
            }
        }

        void Update()
        {
            ProcessModelInteraction();
        }

        void ProcessModelInteraction()
        {
            if (currentModel == null)
                return;

            var isHoldDownTrigger = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger) || OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger);
            var isHoldDownGrip = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger) || OVRInput.Get(OVRInput.Button.SecondaryHandTrigger);

            if (isHoldDownTrigger || isHoldDownGrip)
            {
                var targetMode = isHoldDownTrigger ? ModelInteractionMode.SmallParts : ModelInteractionMode.WholeModel;
                currentModel.SetInteractionMode(targetMode);
            }
            else
            {
                currentModel.SetInteractionMode(ModelInteractionMode.Disabled);
            }
        }

        // Update the UI when a lesson is selected
        public void UpdateLessonUI(int index)
        {
            if (index < 0 || index >= lessons.Length) return;

            LessonData selectedLesson = lessons[index];
            lessonTitleText.text = selectedLesson.lessonTitle;
            lessonDescriptionText.text = selectedLesson.description;
            lessonThumbnailImage.sprite = selectedLesson.thumbnailImage;
        }

        // Instantiate lesson buttons dynamically
        public void InstantiateLessonButtons()
        {
            foreach (Transform child in lessonButtonsContainer)
            {
                Destroy(child.gameObject);
            }

            for (int i = 0; i < lessons.Length; i++)
            {
                GameObject buttonObj = Instantiate(lessonButtonPrefab, lessonButtonsContainer);
                LessonButton lessonButtonScript = buttonObj.GetComponent<LessonButton>();

                if (lessonButtonScript != null)
                {
                    lessonButtonScript.SetButtonData(lessons[i].buttonTitle, lessons[i].buttonDescription);
                }

                Button button = buttonObj.GetComponent<Button>();
                int index = i;
                button.onClick.AddListener(() => OnLessonButtonClicked(index));

                // If this is the first button, highlight it (set it as selected)
                if (i == currentLessonIndex)
                {
                    HighlightButton(button);
                }
            }
        }



        // Play the video for the first lesson
        public void PlayFirstLessonVideo()
        {
            if (videoPlayerController != null && lessons[currentLessonIndex].videoClip != null)
            {
                videoPlayerController.SetVideoClip(lessons[currentLessonIndex].videoClip);
            }
        }

        // Method called when a lesson button is clicked
        public void OnLessonButtonClicked(int index)
        {
            // Deselect the current button (remove highlight)
            if (currentButton != null)
            {
                DeselectButton(currentButton);
            }

            // Set the current lesson index
            currentLessonIndex = index;

            // Update the lesson UI
            UpdateLessonUI(index);

            ChangeLessonModel(index);

            // Highlight the new button
            currentButton = GetButtonForIndex(index);
            HighlightButton(currentButton);

            // Play the video for the selected lesson
            if (videoPlayerController != null && lessons[index].videoClip != null)
            {
                videoPlayerController.SetVideoClip(lessons[index].videoClip);
            }

            // Change the skybox when a new lesson is selected
            SetSkybox(index);
        }

        // Change the model when a lesson is selected
        void ChangeLessonModel(int targetIndex)
        {
            if (currentModel != null)
                Destroy(currentModel.gameObject);

            GameObject modelPrefab = lessons[targetIndex].lesson3DModel;
            if (modelPrefab != null)
            {
                GameObject instance = Instantiate(modelPrefab, modelsContainer.position, Quaternion.identity);
                LessonModelController controller = instance.GetComponent<LessonModelController>();

                if (controller != null)
                {
                    currentModel = controller;
                    currentModel.transform.SetParent(modelsContainer);
                    originalModelScale = currentModel.transform.localScale;

                    // NEW: Populate the parts dropdown after loading the model
                    PopulatePartDropdown();
                }
                else
                {
                    Debug.LogError($"LessonModelController not found on the prefab: {modelPrefab.name}");
                }
            }
            else
            {
                Debug.LogError("Lesson3DModel prefab is null.");
            }
        }


        // Method to toggle the exploded view for the current model
        public void ToggleCurrentModelExplodedView()
        {
            if (currentModel != null)
            {
                currentModel.ToggleExplodedView();
            }
            else
            {
                Debug.LogError("Current model is null.");
            }
        }

        public void ToggleMaximizedMode()
        {
            if (currentModel == null) return;

            isMaximizedMode = !isMaximizedMode;

            if (isMaximizedMode)
            {
                DisableCanvas();  // Hide canvases when entering maximized mode
                EnterMaximizedMode();
            }
            else
            {
                ExitMaximizedMode();
                EnableCanvas();  // Show canvases when exiting maximized mode
            }
        }

        private void EnterMaximizedMode()
        {
            currentModel.transform.localScale = maximizedModelScale;
        }

        private void ExitMaximizedMode()
        {
            currentModel.transform.localScale = originalModelScale;
        }

        public void DisableCanvas()
        {
            if (panelInteractable1 != null)
            {
                panelInteractable1.SetActive(false);
            }

            if (panelInteractable2 != null)
            {
                panelInteractable2.SetActive(false);
            }
        }

        public void EnableCanvas()
        {
            if (panelInteractable1 != null)
            {
                panelInteractable1.SetActive(true);
            }

            if (panelInteractable2 != null)
            {
                panelInteractable2.SetActive(true);
            }
        }




        // Get the button associated with a lesson index
        private Button GetButtonForIndex(int index)
        {
            return lessonButtonsContainer.GetChild(index).GetComponent<Button>();
        }

        // Highlight a button (change color or add a border)
        private void HighlightButton(Button button)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = Color.yellow;  // Change to desired highlight color
            button.colors = colors;
        }

        // Deselect a button (remove highlight)
        private void DeselectButton(Button button)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;  // Reset to original color
            button.colors = colors;
        }

        // Set the skybox based on the selected lesson
        private void SetSkybox(int index)
        {
            if (skyboxMaterials != null && skyboxMaterials.Length > index && skyboxMaterials[index] != null)
            {
                RenderSettings.skybox = skyboxMaterials[index];
            }
        }

        // This method will be triggered when the exploded view button is clicked
        void HandleExplodedViewButtonClicked()
        {
            Debug.Log("Exploded View button clicked!");

            if (currentModel != null)
            {
                currentModel.ToggleExplodedView();
            }
            else
            {
                Debug.LogError("currentModel is null when handling button click.");
            }
        }

        public void ResetModel() {
            if (currentModel != null) {
                currentModel.ResetModel();  // Call ResetModel on the current model
                ScoreManager.Instance?.ResetScore();
                Debug.Log("Model and score reset.");
            } else {
                Debug.LogError("Current model is null.");
            }
        }


        void OnDestroy()
        {
            // Unsubscribe from the event to avoid memory leaks
            if (explodedViewButton != null)
            {
                ExplodedViewButton.OnButtonClicked -= HandleExplodedViewButtonClicked;
            }
        }

        void PopulatePartDropdown()
        {
            if (partDropdown == null || currentModel == null)
                return;

            // Clear any existing options
            partDropdown.ClearOptions();

            // Get the part names from the current model
            List<string> partNames = currentModel.GetChildPartNames();

            // Add the options to the dropdown
            partDropdown.AddOptions(partNames);
        }

        public void OnPartDropdownChanged(int index)
        {
            if (currentModel == null)
                return;

            // Get the selected part using the helper function
            PickablePart selectedPart = currentModel.GetPartByIndex(index);
            if (selectedPart != null)
            {
                // Animate the selected part to the front
                currentModel.BringPartToFront(selectedPart);
            }
        }

    }
}
