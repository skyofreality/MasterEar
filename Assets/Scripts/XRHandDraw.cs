
using System.Collections.Generic;
using UnityEngine;

public class XRHandDraw : MonoBehaviour {
    [Header("Tracking Settings")]
    [SerializeField] private GameObject trackingHand;

    [Header("Drawing Settings")]
    [SerializeField] private float minFingerPinchStrength = 0.5f;
    [SerializeField] private float minDistanceBeforeNewPoint = 0.004f;
    [SerializeField] private float tubeDefaultWidth = 0.010f;
    [SerializeField] private int tubeSides = 8;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Material defaultLineMaterial;
    [SerializeField] private GameObject drawingOptionsPanel;
    [SerializeField] private bool showDrawingOptionsWithDrawing = true;

    [Header("Behavior Settings")]
    [SerializeField] private bool enableGravity = false;
    [SerializeField] private bool colliderTrigger = false;

    [Header("Erase Settings")]
    [SerializeField] private float eraseRadius = 0.05f;
    [SerializeField] private LayerMask tubeLayer;
    [SerializeField] private float timeBeforeEraseStarts = 0.5f;
    [SerializeField] private float handMoveSpeedThreshold = 0.01f;

    [Header("Debug Visual")]
    [SerializeField] private GameObject tipVisualPrefab;
    private GameObject tipVisualInstance;
    private bool isDrawingEnabled = false;


    private float palmOpenTimer = 0f;
    private Vector3 lastHandPosition = Vector3.zero;
    private Vector3 prevPointDistance = Vector3.zero;

    private List<Vector3> points = new List<Vector3>();
    private List<TubeRenderer> tubeRenderers = new List<TubeRenderer>();
    private TubeRenderer currentTubeRenderer;

    private bool isPinchingReleased = false;
    private OVRHand ovrHand;
    private OVRSkeleton ovrSkeleton;
    private Transform intexfinger;

    private int pointsSinceLastMeshUpdate = 0;
    private int updateMeshEveryNPoints = 5;

    private void Start() {
        ovrHand = trackingHand.GetComponent<OVRHand>();
        ovrSkeleton = trackingHand.GetComponent<OVRSkeleton>();
    }

    private void Update() {
        if (ovrSkeleton.Bones != null && intexfinger == null) {
            foreach (var b in ovrSkeleton.Bones) {
                if (b.Id == OVRSkeleton.BoneId.Hand_IndexTip) {
                    intexfinger = b.Transform;
                    break;
                }
            }
        }

        if (intexfinger == null)
            return;

        
        if (!isDrawingEnabled)
            return;

        CheckPinchState();

        if (IsPalmOpen()) {
            palmOpenTimer += Time.deltaTime;
            if (palmOpenTimer >= timeBeforeEraseStarts && HandIsMoving()) {
                TryErase();
            }
        } else {
            palmOpenTimer = 0f;
        }
    }

    private void AddNewTubeRenderer() {
        points.Clear();
        prevPointDistance = Vector3.zero;

        GameObject go = new GameObject($"TubeRenderer__{tubeRenderers.Count}");
        go.transform.position = Vector3.zero;
        go.layer = LayerMask.NameToLayer("Tube");

        TubeRenderer goTubeRenderer = go.AddComponent<TubeRenderer>();
        tubeRenderers.Add(goTubeRenderer);

        var renderer = go.GetComponent<MeshRenderer>();
        Material newMat = new Material(defaultLineMaterial);
        newMat.color = defaultColor;
        renderer.material = newMat;

        goTubeRenderer.ColliderTrigger = colliderTrigger;
        goTubeRenderer.SetPositions(points.ToArray());
        goTubeRenderer._radiusOne = tubeDefaultWidth;
        goTubeRenderer._radiusTwo = tubeDefaultWidth;
        goTubeRenderer._sides = tubeSides;

        currentTubeRenderer = goTubeRenderer;
    }

    private void CheckPinchState() {
        bool isIndexFingerPinching = ovrHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        float indexFingerPinchStrength = ovrHand.GetFingerPinchStrength(OVRHand.HandFinger.Index);

        if (ovrHand.GetFingerConfidence(OVRHand.HandFinger.Index) != OVRHand.TrackingConfidence.High)
            return;

        if (isIndexFingerPinching && indexFingerPinchStrength >= minFingerPinchStrength) {
            if (currentTubeRenderer == null) {
                AddNewTubeRenderer();
            }
            UpdateTube();
            isPinchingReleased = true;
            return;
        }

        if (isPinchingReleased) {
            if (enableGravity && currentTubeRenderer != null)
                currentTubeRenderer.EnableGravity();

            if (currentTubeRenderer != null)
                currentTubeRenderer.GenerateMesh();

            currentTubeRenderer = null;
            isPinchingReleased = false;
        }
    }

    private void UpdateTube() {
        if (prevPointDistance == Vector3.zero) {
            prevPointDistance = intexfinger.position;
        }

        float dist = Vector3.Distance(prevPointDistance, intexfinger.position);
        if (dist >= minDistanceBeforeNewPoint) {
            int numIntermediatePoints = Mathf.CeilToInt(dist / minDistanceBeforeNewPoint);
            Vector3 start = prevPointDistance;
            Vector3 end = intexfinger.position;

            for (int i = 1; i <= numIntermediatePoints; i++) {
                Vector3 interpolated = Vector3.Lerp(start, end, i / (float)(numIntermediatePoints + 1));
                AddPoint(interpolated);
            }

            prevPointDistance = intexfinger.position;
            AddPoint(prevPointDistance);
        }
    }

    private void AddPoint(Vector3 position) {
        points.Add(position);
        pointsSinceLastMeshUpdate++;

        if (pointsSinceLastMeshUpdate >= updateMeshEveryNPoints) {
            currentTubeRenderer.SetPositions(points.ToArray());
            currentTubeRenderer.GenerateMesh();
            pointsSinceLastMeshUpdate = 0;
        }
    }

    private bool IsPalmOpen() {
        return !ovrHand.GetFingerIsPinching(OVRHand.HandFinger.Index)
            && !ovrHand.GetFingerIsPinching(OVRHand.HandFinger.Middle)
            && !ovrHand.GetFingerIsPinching(OVRHand.HandFinger.Ring)
            && !ovrHand.GetFingerIsPinching(OVRHand.HandFinger.Pinky)
            && !ovrHand.GetFingerIsPinching(OVRHand.HandFinger.Thumb);
    }

    private bool HandIsMoving() {
        float handSpeed = (intexfinger.position - lastHandPosition).magnitude / Time.deltaTime;
        lastHandPosition = intexfinger.position;
        return handSpeed > handMoveSpeedThreshold;
    }

    private void TryErase() {
        Collider[] hits = Physics.OverlapSphere(intexfinger.position, eraseRadius, tubeLayer);
        foreach (var hit in hits) {
            TubeRenderer tube = hit.GetComponent<TubeRenderer>();
            if (tube != null) {
                Destroy(tube.gameObject);
            }
        }
    }

    public void ToggleDrawing() {
        isDrawingEnabled = !isDrawingEnabled;
        UpdateDrawingOptionsPanel();
        Debug.Log("Drawing " + (isDrawingEnabled ? "Enabled" : "Disabled"));
    }

    public void SetDrawingEnabled(bool isEnabled) {
        isDrawingEnabled = isEnabled;
        UpdateDrawingOptionsPanel();
        Debug.Log("Drawing " + (isDrawingEnabled ? "Enabled" : "Disabled"));
    }

    private void UpdateDrawingOptionsPanel() {
        if (drawingOptionsPanel != null && showDrawingOptionsWithDrawing) {
            drawingOptionsPanel.SetActive(isDrawingEnabled);
        }
    }

    public void UpdateLineColor(Color color) {
        defaultColor = color;

        if (currentTubeRenderer != null) {
            var renderer = currentTubeRenderer.GetComponent<MeshRenderer>();
            if (renderer != null) {
                renderer.material.color = color;
            }
        }
    }

    public void UpdateLineWidth(float newValue) {
        tubeDefaultWidth = newValue;
    }

    public void UpdateLineMinDistance(float newValue) {
        minDistanceBeforeNewPoint = newValue;
    }

    private void LateUpdate() {
        if (intexfinger == null || tipVisualPrefab == null)
            return;

        if (tipVisualInstance == null) {
            tipVisualInstance = Instantiate(tipVisualPrefab);
            tipVisualInstance.transform.localScale = Vector3.one * 0.005f; // safety
        }

        tipVisualInstance.transform.position = intexfinger.position;
    }
}
