using NudleNexus.Classroom;
using UnityEngine;
using Oculus.Interaction;

public class CustomDistanceGrabTransformer : OneGrabFreeTransformer, ITransformer
{
    [SerializeField] PickablePart _pickablePart;
    public new void Initialize(IGrabbable grabbable)
    {
        base.Initialize(grabbable);
    }
    
    public new void BeginTransform()
    {
        base.BeginTransform();
        _pickablePart.Grab();
    }
}