using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace NudleNexus.Lessons.Space
{
    public class OrbitController : MonoBehaviour
    {
        [SerializeField] RotateAxis axis = RotateAxis.Y;
        [SerializeField] float speed = 1f;
        [SerializeField] int direction = 1;
        
        void Start()
        {
            float rotation = Random.Range(0f, 360f);
            Rotate(rotation);
        }

        void Rotate(float rotation)
        {
            switch (axis)
            {
                case RotateAxis.Y:
                    transform.Rotate(0, rotation, 0);
                    break;
                case RotateAxis.X:
                    transform.Rotate(rotation, 0, 0);
                    break;
                case RotateAxis.Z:
                    transform.Rotate(0, 0, rotation);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void Update()
        {
            float rotationAmount = speed * direction * Time.deltaTime * 10f;
            Rotate(rotationAmount);
        }
    }

    public enum RotateAxis
    {
        Y, X, Z
    }
}