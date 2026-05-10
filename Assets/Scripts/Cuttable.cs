using UnityEngine;

public class Cuttable : MonoBehaviour
{
    [Header("Slice Settings")]
    public Material crossSectionMaterial;
    
    [Tooltip("Если true, нижняя часть разреза будет удалена")]
    public bool removeNegativeSide = false;
    
    [Tooltip("Скорость удаления нижней части (в секундах)")]
    public float removeDelay = 0f;

    [Header("Progressive Slicing")]
    [Tooltip("Использовать прогрессивное разрушение (как в Slice Art Engine)")]
    public bool useProgressiveSlicing = true;

    private bool alreadyCut = false;

    void Start()
    {
        // Автоматически добавляем ProgressiveSlicer если нужно
        if (useProgressiveSlicing)
        {
            ProgressiveSlicer progressiveSlicer = GetComponent<ProgressiveSlicer>();
            if (progressiveSlicer == null)
            {
                progressiveSlicer = gameObject.AddComponent<ProgressiveSlicer>();
            }

            progressiveSlicer.crossSectionMaterial = crossSectionMaterial;
            progressiveSlicer.removeNegativeSide = removeNegativeSide;
            progressiveSlicer.removeDelay = removeDelay;
        }
    }

    public void Cut(Plane plane)
    {
        if (alreadyCut) return;
        alreadyCut = true;

        MeshFilter mf = GetComponent<MeshFilter>();
        if (!mf) return;

        Mesh originalMesh = mf.mesh;

        SliceResult result = MeshSlicer.Slice(originalMesh, transform, plane, crossSectionMaterial, false);

        // Копируем импульс с оригинального объекта, если он есть
        Rigidbody originalRb = GetComponent<Rigidbody>();
        if (originalRb != null)
        {
            if (result.positiveObject != null)
            {
                Rigidbody rb = result.positiveObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = originalRb.velocity;
                    rb.angularVelocity = originalRb.angularVelocity;
                }
            }

            if (result.negativeObject != null)
            {
                Rigidbody rb = result.negativeObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = originalRb.velocity;
                    rb.angularVelocity = originalRb.angularVelocity;
                }
            }
        }

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

        Destroy(gameObject);
        Debug.Log("[Slice] Object cut successfully");
    }
}
