using UnityEngine;

public class AvatarLookAtController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public bool autoUseMainCamera = true;

    [Header("Body Facing")]
    public Transform bodyYawTransform;
    public bool rotateBodyYaw = true;
    [Range(30f, 360f)]
    public float bodyYawDegreesPerSecond = 120f;

    [Header("Head Look")]
    public Transform headBone;
    public bool rotateHead = true;
    [Range(30f, 360f)]
    public float headDegreesPerSecond = 180f;
    [Range(0f, 80f)]
    public float maxHeadYaw = 35f;
    [Range(0f, 50f)]
    public float maxHeadPitch = 20f;

    [Header("Eye Look")]
    public Transform leftEyeBone;
    public Transform rightEyeBone;
    public bool rotateEyes = true;
    [Range(30f, 720f)]
    public float eyeDegreesPerSecond = 300f;
    [Range(0f, 40f)]
    public float maxEyeYaw = 12f;
    [Range(0f, 30f)]
    public float maxEyePitch = 8f;

    [Header("Behavior")]
    public bool useUnscaledTime = false;
    public bool logAutoResolution = false;

    private Quaternion initialHeadLocalRotation;
    private bool cachedInitialHeadRotation;
    private Quaternion initialLeftEyeLocalRotation;
    private Quaternion initialRightEyeLocalRotation;
    private bool cachedInitialEyeRotations;

    private void Start()
    {
        ResolveReferences();
        CacheHeadRotation();
        CacheEyeRotations();
    }

    private void LateUpdate()
    {
        ResolveTarget();
        if (target == null)
        {
            return;
        }

        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (deltaTime <= 0f)
        {
            return;
        }

        if (rotateBodyYaw && bodyYawTransform != null)
        {
            UpdateBodyYaw(deltaTime);
        }

        if (rotateHead && headBone != null)
        {
            UpdateHeadLook(deltaTime);
        }

        if (rotateEyes)
        {
            UpdateEyeLook(deltaTime);
        }
    }

    private void ResolveReferences()
    {
        if (bodyYawTransform == null)
        {
            bodyYawTransform = transform;
        }

        if (headBone != null)
        {
            if (leftEyeBone == null)
            {
                leftEyeBone = FindChildRecursive(headBone, "LeftEye");
            }

            if (rightEyeBone == null)
            {
                rightEyeBone = FindChildRecursive(headBone, "RightEye");
            }

            if (logAutoResolution)
            {
                if (leftEyeBone != null)
                {
                    Debug.Log("[AvatarLookAtController] Auto-resolved LeftEye bone.");
                }

                if (rightEyeBone != null)
                {
                    Debug.Log("[AvatarLookAtController] Auto-resolved RightEye bone.");
                }
            }
        }
    }

    private void ResolveTarget()
    {
        if (target == null && autoUseMainCamera && Camera.main != null)
        {
            target = Camera.main.transform;

            if (logAutoResolution)
            {
                Debug.Log("[AvatarLookAtController] Using Camera.main as look target.");
            }
        }
    }

    private void CacheHeadRotation()
    {
        if (headBone != null && !cachedInitialHeadRotation)
        {
            initialHeadLocalRotation = headBone.localRotation;
            cachedInitialHeadRotation = true;
        }
    }

    private void CacheEyeRotations()
    {
        if (cachedInitialEyeRotations)
        {
            return;
        }

        if (leftEyeBone != null)
        {
            initialLeftEyeLocalRotation = leftEyeBone.localRotation;
        }

        if (rightEyeBone != null)
        {
            initialRightEyeLocalRotation = rightEyeBone.localRotation;
        }

        cachedInitialEyeRotations = true;
    }

    private void UpdateBodyYaw(float deltaTime)
    {
        Vector3 toTarget = target.position - bodyYawTransform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion desiredRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        bodyYawTransform.rotation = Quaternion.RotateTowards(
            bodyYawTransform.rotation,
            desiredRotation,
            bodyYawDegreesPerSecond * deltaTime);
    }

    private void UpdateHeadLook(float deltaTime)
    {
        if (headBone.parent == null)
        {
            return;
        }

        CacheHeadRotation();

        Vector3 worldDirection = target.position - headBone.position;
        if (worldDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector3 localDirection = headBone.parent.InverseTransformDirection(worldDirection.normalized);
        float yaw = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
        float pitch = -Mathf.Atan2(localDirection.y, new Vector2(localDirection.x, localDirection.z).magnitude) * Mathf.Rad2Deg;

        yaw = Mathf.Clamp(yaw, -maxHeadYaw, maxHeadYaw);
        pitch = Mathf.Clamp(pitch, -maxHeadPitch, maxHeadPitch);

        Quaternion targetLocalRotation = initialHeadLocalRotation * Quaternion.Euler(pitch, yaw, 0f);
        headBone.localRotation = Quaternion.RotateTowards(
            headBone.localRotation,
            targetLocalRotation,
            headDegreesPerSecond * deltaTime);
    }

    private void UpdateEyeLook(float deltaTime)
    {
        CacheEyeRotations();

        if (leftEyeBone != null)
        {
            UpdateSingleEyeLook(leftEyeBone, initialLeftEyeLocalRotation, deltaTime);
        }

        if (rightEyeBone != null)
        {
            UpdateSingleEyeLook(rightEyeBone, initialRightEyeLocalRotation, deltaTime);
        }
    }

    private void UpdateSingleEyeLook(Transform eyeBone, Quaternion initialLocalRotation, float deltaTime)
    {
        if (eyeBone == null || eyeBone.parent == null)
        {
            return;
        }

        Vector3 worldDirection = target.position - eyeBone.position;
        if (worldDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector3 localDirection = eyeBone.parent.InverseTransformDirection(worldDirection.normalized);
        float yaw = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
        float pitch = -Mathf.Atan2(localDirection.y, new Vector2(localDirection.x, localDirection.z).magnitude) * Mathf.Rad2Deg;

        yaw = Mathf.Clamp(yaw, -maxEyeYaw, maxEyeYaw);
        pitch = Mathf.Clamp(pitch, -maxEyePitch, maxEyePitch);

        Quaternion targetLocalRotation = initialLocalRotation * Quaternion.Euler(pitch, yaw, 0f);
        eyeBone.localRotation = Quaternion.RotateTowards(
            eyeBone.localRotation,
            targetLocalRotation,
            eyeDegreesPerSecond * deltaTime);
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindChildRecursive(root.GetChild(i), childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
