using UnityEngine;

/// <summary>
/// Пример сетапа для демонстрации системы разреза
/// </summary>
public class SliceDemo : MonoBehaviour
{
    [Header("Sword References")]
    public Transform sword;
    public Transform bladeTip;
    public Transform bladeBase;

    [Header("Test Objects")]
    public GameObject[] testObjects;

    [Header("Settings")]
    public Material crossSectionMaterial;
    public bool removeNegativeSide = false;

    void Start()
    {
        // Проверяем наличие необходимых компонентов
        ValidateSetup();

        // Настраиваем тестовые объекты
        if (testObjects.Length > 0)
        {
            SetupTestObjects();
        }
    }

    void ValidateSetup()
    {
        if (sword == null)
        {
            Debug.LogError("[SliceDemo] Меч не назначен!");
            return;
        }

        SwordCutPlane cutPlane = sword.GetComponent<SwordCutPlane>();
        if (cutPlane == null)
        {
            Debug.LogWarning("[SliceDemo] На мече нет SwordCutPlane!");
            return;
        }

        if (bladeTip == null || bladeBase == null)
        {
            Debug.LogError("[SliceDemo] Не назначены Blade Tip или Blade Base!");
            return;
        }

        cutPlane.bladeTip = bladeTip;
        cutPlane.bladeBase = bladeBase;

        SwordHit swordHit = sword.GetComponent<SwordHit>();
        if (swordHit == null)
        {
            Debug.LogWarning("[SliceDemo] На мече нет SwordHit!");
        }

        Debug.Log("[SliceDemo] Сетап успешен!");
    }

    void SetupTestObjects()
    {
        foreach (GameObject obj in testObjects)
        {
            if (obj == null) continue;

            Cuttable cuttable = obj.GetComponent<Cuttable>();
            if (cuttable == null)
            {
                cuttable = obj.AddComponent<Cuttable>();
            }

            cuttable.crossSectionMaterial = crossSectionMaterial;
            cuttable.removeNegativeSide = removeNegativeSide;

            Debug.Log("[SliceDemo] Настроен объект: " + obj.name);
        }
    }

    void Update()
    {
        // Демонстрационный контроль для тестирования
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("[SliceDemo] Нажат R - переинициализация");
            ValidateSetup();
        }
    }

    /// <summary>
    /// Добавить объект в тестовый список и настроить его
    /// </summary>
    public void AddTestObject(GameObject obj)
    {
        if (obj == null) return;

        Cuttable cuttable = obj.GetComponent<Cuttable>();
        if (cuttable == null)
        {
            cuttable = obj.AddComponent<Cuttable>();
        }

        cuttable.crossSectionMaterial = crossSectionMaterial;
        cuttable.removeNegativeSide = removeNegativeSide;

        Debug.Log("[SliceDemo] Добавлен объект: " + obj.name);
    }
}
