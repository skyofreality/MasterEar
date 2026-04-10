using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ReadyPlayerMeLipSyncBinder : MonoBehaviour
{
    private static readonly string[] OvrVisemeNames =
    {
        "sil", "PP", "FF", "TH", "DD", "kk", "CH", "SS",
        "nn", "RR", "aa", "E", "ih", "oh", "ou"
    };

    private static readonly string[][] OvrVisemeAliases =
    {
        new[] { "sil", "viseme_sil" },
        new[] { "PP", "viseme_PP" },
        new[] { "FF", "viseme_FF" },
        new[] { "TH", "viseme_TH" },
        new[] { "DD", "viseme_DD" },
        new[] { "kk", "viseme_kk" },
        new[] { "CH", "viseme_CH" },
        new[] { "SS", "viseme_SS" },
        new[] { "nn", "viseme_nn" },
        new[] { "RR", "viseme_RR" },
        new[] { "aa", "viseme_aa" },
        new[] { "E", "viseme_E" },
        new[] { "ih", "viseme_I", "viseme_ih" },
        new[] { "oh", "viseme_O", "viseme_oh" },
        new[] { "ou", "viseme_U", "viseme_ou" }
    };

    [Header("Primary References")]
    public AnatomyLiveClient liveClient;
    public GameObject avatarRoot;
    public AudioSource targetAudioSource;

    [Header("Resolved Components")]
    public SkinnedMeshRenderer faceRenderer;
    public OVRLipSyncContext lipSyncContext;
    public OVRLipSyncContextMorphTarget morphTargetContext;

    [Header("Auto Setup")]
    public bool autoConfigureOnStart = true;
    public bool addMissingLipSyncComponents = true;
    public bool searchInactiveChildren = true;
    public bool logMappingDetails = true;
    [Range(1, 100)]
    public int smoothing = 70;

    private void Start()
    {
        if (autoConfigureOnStart)
        {
            StartCoroutine(ConfigureWhenReady());
        }
    }

    [ContextMenu("Auto Configure Lip Sync")]
    public void AutoConfigureLipSync()
    {
        ResolveReferences();
        EnsureLipSyncComponents();
        MapBlendshapes();
    }

    private IEnumerator ConfigureWhenReady()
    {
        for (int i = 0; i < 30; i++)
        {
            ResolveReferences();
            if (targetAudioSource != null)
            {
                break;
            }

            yield return null;
        }

        AutoConfigureLipSync();
    }

    private void ResolveReferences()
    {
        if (liveClient == null)
        {
            liveClient = FindObjectOfType<AnatomyLiveClient>();
        }

        if (targetAudioSource == null && liveClient != null)
        {
            targetAudioSource = liveClient.teacherAudioSource;
        }

        if (avatarRoot == null)
        {
            avatarRoot = FindLikelyAvatarRoot();
        }

        if (faceRenderer == null)
        {
            faceRenderer = FindBestFaceRenderer();
        }
    }

    private void EnsureLipSyncComponents()
    {
        if (targetAudioSource == null)
        {
            Debug.LogWarning("[ReadyPlayerMeLipSyncBinder] No target AudioSource found.");
            return;
        }

        GameObject host = targetAudioSource.gameObject;

        if (lipSyncContext == null)
        {
            lipSyncContext = host.GetComponent<OVRLipSyncContext>();
        }

        if (morphTargetContext == null)
        {
            morphTargetContext = host.GetComponent<OVRLipSyncContextMorphTarget>();
        }

        if (addMissingLipSyncComponents)
        {
            if (lipSyncContext == null)
            {
                lipSyncContext = host.AddComponent<OVRLipSyncContext>();
            }

            if (morphTargetContext == null)
            {
                morphTargetContext = host.AddComponent<OVRLipSyncContextMorphTarget>();
            }
        }

        if (lipSyncContext != null)
        {
            lipSyncContext.audioSource = targetAudioSource;
            lipSyncContext.audioLoopback = true;
            lipSyncContext.skipAudioSource = false;
            lipSyncContext.enableAcceleration = true;
        }

        if (morphTargetContext != null)
        {
            morphTargetContext.skinnedMeshRenderer = faceRenderer;
            morphTargetContext.smoothAmount = smoothing;
            morphTargetContext.laughterBlendTarget = -1;
        }
    }

    private void MapBlendshapes()
    {
        if (morphTargetContext == null || faceRenderer == null || faceRenderer.sharedMesh == null)
        {
            Debug.LogWarning("[ReadyPlayerMeLipSyncBinder] Cannot map blendshapes because the face renderer or morph target context is missing.");
            return;
        }

        Mesh mesh = faceRenderer.sharedMesh;
        int[] mapping = Enumerable.Repeat(-1, OVRLipSync.VisemeCount).ToArray();
        Dictionary<string, int> blendShapeByName = new Dictionary<string, int>();

        for (int i = 0; i < mesh.blendShapeCount; i++)
        {
            string blendShapeName = mesh.GetBlendShapeName(i);
            if (!blendShapeByName.ContainsKey(blendShapeName))
            {
                blendShapeByName.Add(blendShapeName, i);
            }
        }

        List<string> foundMappings = new List<string>();
        List<string> missingMappings = new List<string>();

        for (int i = 0; i < OvrVisemeAliases.Length; i++)
        {
            int blendShapeIndex = FindBlendShapeIndex(blendShapeByName, OvrVisemeAliases[i]);
            if (blendShapeIndex >= 0)
            {
                mapping[i] = blendShapeIndex;
                foundMappings.Add($"{OvrVisemeNames[i]}->{blendShapeIndex}");
            }
            else
            {
                missingMappings.Add(OvrVisemeNames[i]);
            }
        }

        morphTargetContext.visemeToBlendTargets = mapping;

        if (logMappingDetails)
        {
            Debug.Log($"[ReadyPlayerMeLipSyncBinder] Using face renderer '{faceRenderer.name}' with {mesh.blendShapeCount} blendshapes.");
            Debug.Log($"[ReadyPlayerMeLipSyncBinder] Mapped visemes: {string.Join(", ", foundMappings)}");

            if (missingMappings.Count > 0)
            {
                Debug.LogWarning($"[ReadyPlayerMeLipSyncBinder] Missing viseme blendshapes: {string.Join(", ", missingMappings)}");
            }
        }
    }

    private GameObject FindLikelyAvatarRoot()
    {
        if (faceRenderer != null)
        {
            return faceRenderer.transform.root.gameObject;
        }

        SkinnedMeshRenderer[] renderers = FindObjectsOfType<SkinnedMeshRenderer>(searchInactiveChildren);
        SkinnedMeshRenderer bestRenderer = renderers
            .OrderByDescending(GetVisemeMatchCount)
            .FirstOrDefault(renderer => renderer != null && renderer.sharedMesh != null && renderer.name.IndexOf("Wolf3D", System.StringComparison.OrdinalIgnoreCase) >= 0);

        return bestRenderer != null ? bestRenderer.transform.root.gameObject : null;
    }

    private SkinnedMeshRenderer FindBestFaceRenderer()
    {
        IEnumerable<SkinnedMeshRenderer> candidateRenderers;

        if (avatarRoot != null)
        {
            candidateRenderers = avatarRoot.GetComponentsInChildren<SkinnedMeshRenderer>(searchInactiveChildren);
        }
        else
        {
            candidateRenderers = FindObjectsOfType<SkinnedMeshRenderer>(searchInactiveChildren);
        }

        return candidateRenderers
            .Where(renderer => renderer != null && renderer.sharedMesh != null)
            .OrderByDescending(GetVisemeMatchCount)
            .FirstOrDefault();
    }

    private int GetVisemeMatchCount(SkinnedMeshRenderer renderer)
    {
        if (renderer == null || renderer.sharedMesh == null)
        {
            return 0;
        }

        Mesh mesh = renderer.sharedMesh;
        int matches = 0;

        Dictionary<string, int> blendShapeByName = new Dictionary<string, int>();
        for (int i = 0; i < mesh.blendShapeCount; i++)
        {
            string blendShapeName = mesh.GetBlendShapeName(i);
            if (!blendShapeByName.ContainsKey(blendShapeName))
            {
                blendShapeByName.Add(blendShapeName, i);
            }
        }

        for (int i = 0; i < OvrVisemeAliases.Length; i++)
        {
            if (FindBlendShapeIndex(blendShapeByName, OvrVisemeAliases[i]) >= 0)
            {
                matches++;
            }
        }

        return matches;
    }

    private static int FindBlendShapeIndex(Dictionary<string, int> blendShapeByName, IEnumerable<string> candidateNames)
    {
        foreach (string candidateName in candidateNames)
        {
            if (blendShapeByName.TryGetValue(candidateName, out int blendShapeIndex))
            {
                return blendShapeIndex;
            }
        }

        return -1;
    }
}
