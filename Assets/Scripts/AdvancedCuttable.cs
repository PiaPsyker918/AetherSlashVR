using UnityEngine;

/// <summary>
/// Расширенный компонент для режущихся объектов с доп. возможностями
/// </summary>
public class AdvancedCuttable : MonoBehaviour
{
    [Header("Slice Configuration")]
    public Material crossSectionMaterial;
    public bool removeNegativeSide = false;
    public float removeDelay = 0f;

    [Header("Effects")]
    public ParticleSystem sliceParticles;
    public AudioClip sliceSound;
    public float soundVolume = 1f;

    [Header("Callbacks")]
    public bool useCallbacks = false;

    private Cuttable baseCuttable;

    void Start()
    {
        // Убедимся, что у нас есть базовый Cuttable компонент
        baseCuttable = GetComponent<Cuttable>();
        if (baseCuttable == null)
        {
            baseCuttable = gameObject.AddComponent<Cuttable>();
        }

        SyncSettings();
    }

    void SyncSettings()
    {
        if (baseCuttable == null) return;

        baseCuttable.crossSectionMaterial = crossSectionMaterial;
        baseCuttable.removeNegativeSide = removeNegativeSide;
        baseCuttable.removeDelay = removeDelay;
    }

    public void OnObjectSliced(GameObject positiveObj, GameObject negativeObj)
    {
        if (!useCallbacks) return;

        // Воспроизведение звука
        if (sliceSound != null)
        {
            AudioSource.PlayClipAtPoint(sliceSound, transform.position, soundVolume);
        }

        // Воспроизведение частиц
        if (sliceParticles != null)
        {
            ParticleSystem particles = Instantiate(sliceParticles, transform.position, Quaternion.identity);
            particles.Play();
            Destroy(particles.gameObject, 3f);
        }
    }

    #if UNITY_EDITOR
    void OnValidate()
    {
        SyncSettings();
    }
    #endif
}
