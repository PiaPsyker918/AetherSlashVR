using UnityEngine;

/// <summary>
/// Управляет глобальными параметрами системы разреза
/// </summary>
public class SliceManager : MonoBehaviour
{
    public static SliceManager Instance { get; private set; }

    [Header("Slice Behavior")]
    [Tooltip("Удалять ли отрицательную часть разреза")]
    public bool removeNegativeSide = false;

    [Tooltip("Задержка перед удалением отрицательной части")]
    public float removeDelay = 0f;

    [Tooltip("Скорость полета отделённых частей")]
    public float sliceFlingForce = 3f;

    [Header("Physics")]
    [Tooltip("Масса отделённой части")]
    public float sliceMass = 0.5f;

    [Tooltip("Применять ли импульс от удара")]
    public bool inheritImpactForce = true;

    [Header("Visuals")]
    public Material crossSectionMaterial;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetSliceConfig(Cuttable cuttable)
    {
        if (cuttable == null) return;

        cuttable.removeNegativeSide = removeNegativeSide;
        cuttable.removeDelay = removeDelay;
        cuttable.crossSectionMaterial = crossSectionMaterial ?? cuttable.crossSectionMaterial;
    }

    void OnDrawGizmosSelected()
    {
        // Визуализация для отладки
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.1f);
    }
}
