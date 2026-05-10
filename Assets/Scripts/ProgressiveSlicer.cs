using System.Collections.Generic;
using UnityEngine;

public class ProgressiveSlicer : MonoBehaviour
{
    private const float SurfaceLineOffset = 0.0015f;
    private const float TracePointMinDistance = 0.005f;

    [Header("Slice Settings")]
    public Material crossSectionMaterial;
    public bool removeNegativeSide = false;
    public float removeDelay = 0f;
    public float sliceDepth = 0.1f;
    public int maxSlices = 10;
    public float minSliceDistance = 0.05f;

    [Header("Debug")]
    public bool showSlicePoints = true;
    public Color slicePointColor = Color.red;
    public bool showTracerLine = true;
    public Color tracerColor = Color.green;

    private struct SurfaceSample
    {
        public Vector3 point;
        public Vector3 normal;

        public SurfaceSample(Vector3 point, Vector3 normal)
        {
            this.point = point;
            this.normal = normal.sqrMagnitude > 0.001f ? normal.normalized : Vector3.up;
        }
    }

    private readonly List<Vector3> slicePoints = new List<Vector3>();
    private readonly List<Vector3> tracerPoints = new List<Vector3>();
    private readonly List<Plane> slicePlanes = new List<Plane>();
    private bool isSlicing;
    private SwordCutPlane swordCutPlane;
    private LineRenderer tracerLine;
    private Collider objectCollider;
    private Vector3 lastSurfaceNormal = Vector3.up;
    private Vector3 lastSurfacePoint;

    void Start()
    {
        GameObject sword = GameObject.FindGameObjectWithTag("Sword");
        if (sword != null)
        {
            swordCutPlane = sword.GetComponent<SwordCutPlane>();
        }

        objectCollider = GetComponent<Collider>();
        CreateTracerLine();
    }

    void CreateTracerLine()
    {
        if (tracerLine != null || !showTracerLine)
        {
            return;
        }

        tracerLine = gameObject.AddComponent<LineRenderer>();
        tracerLine.material = new Material(Shader.Find("Sprites/Default"));
        tracerLine.startColor = tracerColor;
        tracerLine.endColor = tracerColor;
        tracerLine.startWidth = 0.01f;
        tracerLine.endWidth = 0.01f;
        tracerLine.useWorldSpace = true;
        tracerLine.positionCount = 0;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Sword") && !isSlicing)
        {
            StartProgressiveSlice(other);
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Sword") && isSlicing)
        {
            ContinueProgressiveSlice(other);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Sword") && isSlicing)
        {
            FinishProgressiveSlice();
        }
    }

    void StartProgressiveSlice(Collider swordCollider)
    {
        isSlicing = true;
        slicePoints.Clear();
        tracerPoints.Clear();
        slicePlanes.Clear();

        SurfaceSample entrySample = GetSurfaceSample(swordCollider);
        lastSurfaceNormal = entrySample.normal;
        lastSurfacePoint = entrySample.point;

        slicePoints.Add(entrySample.point);
        tracerPoints.Add(GetDisplayPoint(entrySample));

        if (swordCutPlane != null)
        {
            slicePlanes.Add(swordCutPlane.GetCutPlane());
        }

        UpdateTracerLine();
    }

    void ContinueProgressiveSlice(Collider swordCollider)
    {
        if (!isSlicing)
        {
            return;
        }

        SurfaceSample sample = GetSurfaceSample(swordCollider);
        lastSurfaceNormal = sample.normal;
        lastSurfacePoint = sample.point;

        Vector3 displayPoint = GetDisplayPoint(sample);
        if (tracerPoints.Count == 0 || Vector3.Distance(displayPoint, tracerPoints[tracerPoints.Count - 1]) >= TracePointMinDistance)
        {
            tracerPoints.Add(displayPoint);
            UpdateTracerLine();
        }

        if (slicePoints.Count >= maxSlices)
        {
            return;
        }

        Vector3 lastSlicePoint = slicePoints[slicePoints.Count - 1];
        if (Vector3.Distance(sample.point, lastSlicePoint) < minSliceDistance)
        {
            return;
        }

        slicePoints.Add(sample.point);
        if (swordCutPlane != null)
        {
            slicePlanes.Add(swordCutPlane.GetCutPlane());
        }
    }

    SurfaceSample GetSurfaceSample(Collider swordCollider)
    {
        if (objectCollider == null)
        {
            objectCollider = GetComponent<Collider>();
        }

        if (objectCollider == null)
        {
            return new SurfaceSample(transform.position, lastSurfaceNormal);
        }

        Vector3 swordPoint = GetSwordSamplePosition(swordCollider);
        RaycastHit hit;

        if (swordCutPlane != null && swordCutPlane.bladeBase != null && swordCutPlane.bladeTip != null)
        {
            List<SurfaceSample> segmentSamples = new List<SurfaceSample>(2);
            Vector3 bladeBase = swordCutPlane.bladeBase.position;
            Vector3 bladeTip = swordCutPlane.bladeTip.position;
            Vector3 bladeVector = bladeTip - bladeBase;
            float bladeLength = bladeVector.magnitude;

            if (bladeLength > 0.0001f)
            {
                Vector3 bladeDir = bladeVector / bladeLength;

                if (objectCollider.Raycast(new Ray(bladeBase, bladeDir), out hit, bladeLength))
                {
                    segmentSamples.Add(new SurfaceSample(hit.point, hit.normal));
                }

                if (objectCollider.Raycast(new Ray(bladeTip, -bladeDir), out hit, bladeLength))
                {
                    segmentSamples.Add(new SurfaceSample(hit.point, hit.normal));
                }

                if (segmentSamples.Count == 1)
                {
                    return segmentSamples[0];
                }

                if (segmentSamples.Count == 2)
                {
                    return ChooseBestSample(segmentSamples[0], segmentSamples[1]);
                }
            }
        }

        Plane cutPlane = swordCutPlane != null ? swordCutPlane.GetCutPlane() : new Plane(lastSurfaceNormal, swordPoint);
        Vector3 planeNormal = cutPlane.normal.sqrMagnitude > 0.0001f ? cutPlane.normal.normalized : lastSurfaceNormal;
        float probeDistance = Mathf.Max(objectCollider.bounds.extents.magnitude, 0.25f);

        bool hasPositiveHit = objectCollider.Raycast(
            new Ray(swordPoint + planeNormal * probeDistance, -planeNormal),
            out RaycastHit positiveHit,
            probeDistance * 2f);

        bool hasNegativeHit = objectCollider.Raycast(
            new Ray(swordPoint - planeNormal * probeDistance, planeNormal),
            out RaycastHit negativeHit,
            probeDistance * 2f);

        if (hasPositiveHit && hasNegativeHit)
        {
            SurfaceSample positiveSample = new SurfaceSample(positiveHit.point, positiveHit.normal);
            SurfaceSample negativeSample = new SurfaceSample(negativeHit.point, negativeHit.normal);
            return ChooseBestSample(positiveSample, negativeSample);
        }

        if (hasPositiveHit)
        {
            return new SurfaceSample(positiveHit.point, positiveHit.normal);
        }

        if (hasNegativeHit)
        {
            return new SurfaceSample(negativeHit.point, negativeHit.normal);
        }

        if (swordCollider != null)
        {
            Vector3 swordCenter = swordCollider.bounds.center;
            Vector3 directionToSword = swordCenter - swordPoint;
            if (directionToSword.sqrMagnitude > 0.0001f)
            {
                Vector3 rayDir = directionToSword.normalized;
                if (objectCollider.Raycast(new Ray(swordPoint, rayDir), out hit, directionToSword.magnitude + probeDistance))
                {
                    return new SurfaceSample(hit.point, hit.normal);
                }
            }
        }

        return new SurfaceSample(lastSurfacePoint != Vector3.zero ? lastSurfacePoint : swordPoint, planeNormal);
    }

    Vector3 GetSwordSamplePosition(Collider swordCollider)
    {
        if (swordCutPlane != null)
        {
            return swordCutPlane.GetPlanePosition();
        }

        if (swordCollider != null)
        {
            return swordCollider.bounds.center;
        }

        return transform.position;
    }

    SurfaceSample ChooseBestSample(SurfaceSample a, SurfaceSample b)
    {
        if (lastSurfacePoint != Vector3.zero)
        {
            float distA = (a.point - lastSurfacePoint).sqrMagnitude;
            float distB = (b.point - lastSurfacePoint).sqrMagnitude;
            return distA <= distB ? a : b;
        }

        if (swordCutPlane != null)
        {
            Vector3 swordOrigin = swordCutPlane.transform.position;
            float distA = (a.point - swordOrigin).sqrMagnitude;
            float distB = (b.point - swordOrigin).sqrMagnitude;
            return distA <= distB ? a : b;
        }

        return a;
    }

    Vector3 GetDisplayPoint(SurfaceSample sample)
    {
        return sample.point + sample.normal * SurfaceLineOffset;
    }

    void UpdateTracerLine()
    {
        if (tracerLine == null)
        {
            return;
        }

        tracerLine.startColor = tracerColor;
        tracerLine.endColor = tracerColor;
        tracerLine.positionCount = tracerPoints.Count;

        for (int i = 0; i < tracerPoints.Count; i++)
        {
            tracerLine.SetPosition(i, tracerPoints[i]);
        }
    }

    void FinishProgressiveSlice()
    {
        isSlicing = false;

        if (tracerLine != null)
        {
            tracerLine.positionCount = 0;
        }

        if (slicePlanes.Count == 0 && swordCutPlane != null)
        {
            slicePlanes.Add(swordCutPlane.GetCutPlane());
        }

        GameObject currentObject = gameObject;
        for (int i = 0; i < slicePlanes.Count && currentObject != null; i++)
        {
            currentObject = PerformSlice(currentObject, slicePlanes[i]);
        }
    }

    GameObject PerformSlice(GameObject source, Plane plane)
    {
        MeshFilter meshFilter = source.GetComponent<MeshFilter>();
        if (!meshFilter)
        {
            return null;
        }

        Mesh originalMesh = meshFilter.mesh;
        SliceResult result = MeshSlicer.Slice(originalMesh, source.transform, plane, crossSectionMaterial, false);

        if (result.negativeObject != null && removeNegativeSide)
        {
            if (removeDelay > 0f)
            {
                FadeAndDestroy fade = result.negativeObject.AddComponent<FadeAndDestroy>();
                fade.duration = removeDelay;
                fade.Begin();
            }
            else
            {
                Destroy(result.negativeObject);
            }
        }

        if (result.positiveObject != null)
        {
            result.positiveObject.transform.position = source.transform.position;
            result.positiveObject.transform.rotation = source.transform.rotation;
            result.positiveObject.transform.localScale = source.transform.localScale;
            CopyComponentsToSlice(result.positiveObject);
        }

        Destroy(source);
        return result.positiveObject;
    }

    void CopyComponentsToSlice(GameObject sliceObj)
    {
        ProgressiveSlicer newSlicer = sliceObj.AddComponent<ProgressiveSlicer>();
        newSlicer.crossSectionMaterial = crossSectionMaterial;
        newSlicer.removeNegativeSide = removeNegativeSide;
        newSlicer.removeDelay = removeDelay;
        newSlicer.sliceDepth = sliceDepth;
        newSlicer.maxSlices = maxSlices;
        newSlicer.minSliceDistance = minSliceDistance;
        newSlicer.showSlicePoints = showSlicePoints;
        newSlicer.slicePointColor = slicePointColor;
        newSlicer.showTracerLine = showTracerLine;
        newSlicer.tracerColor = tracerColor;
    }

    void OnDrawGizmos()
    {
        if (!showSlicePoints || slicePoints.Count == 0)
        {
            return;
        }

        Gizmos.color = slicePointColor;
        for (int i = 0; i < slicePoints.Count; i++)
        {
            Gizmos.DrawWireSphere(slicePoints[i], 0.02f);

            if (i > 0)
            {
                Gizmos.DrawLine(slicePoints[i - 1], slicePoints[i]);
            }
        }

        if (showTracerLine && tracerPoints.Count > 1)
        {
            Gizmos.color = tracerColor;
            for (int i = 0; i < tracerPoints.Count - 1; i++)
            {
                Gizmos.DrawLine(tracerPoints[i], tracerPoints[i + 1]);
            }
        }
    }
}
