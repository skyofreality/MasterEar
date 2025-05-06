using UnityEngine;
using UnityEngine.UI;

namespace NudleNexus.Classroom
{
    public class ExplodedViewButton : MonoBehaviour
    {
        private Button explodeButton;
        private LessonManager lessonManager;

        // Static event that will notify any listeners when the button is clicked
        public static event System.Action OnButtonClicked;

        void Start()
        {
            explodeButton = GetComponent<Button>();

            // Listen for button click
            explodeButton.onClick.AddListener(OnClick);
        }

        // This method will be called when the button is clicked
        public void OnClick()
        {
            // Trigger the event to inform the LessonManager
            OnButtonClicked?.Invoke();
        }

        public void Initialize(LessonManager manager)
        {
            lessonManager = manager;
            lessonManager.ToggleCurrentModelExplodedView();
        }
    }
}
