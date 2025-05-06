using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NudleNexus.Classroom
{
    public class ControllerInputExample : MonoBehaviour
    {
        [SerializeField] private GameObject[] lessonModelPrefabs; // Array of prefabs to instantiate
        private List<LessonModelController> lessonModelControllers = new List<LessonModelController>(); // List to store references to instantiated controllers

        void Start()
        {
            // Instantiate each prefab and store its LessonModelController component
            foreach (var prefab in lessonModelPrefabs)
            {
                GameObject lessonModelInstance = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                LessonModelController controller = lessonModelInstance.GetComponent<LessonModelController>();

                if (controller != null)
                {
                    lessonModelControllers.Add(controller);
                    controller.SetInteractionMode(ModelInteractionMode.WholeModel); // Set the initial interaction mode to WholeModel
                }
                else
                {
                    Debug.LogError("LessonModelController component is missing from the instantiated prefab.");
                }
            }
        }
    }
}
