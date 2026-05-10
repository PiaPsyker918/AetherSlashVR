using UnityEngine;

public class SwordHit : MonoBehaviour
{
    [Header("Hit Detection")]
    public float minHitSpeed = 1f;
    public float cooldown = 0.1f;

    [Header("VFX")]
    public GameObject hitVFX;

    public enum LogLevel { None, HitsOnly, Debug }
    
    [Header("Debug")]
    public LogLevel logLevel = LogLevel.HitsOnly;

    private Vector3 lastPos;
    private Vector3 velocity;
    private float lastHitTime;
    private SwordCutPlane swordCutPlane;

    void Start()
    {
        swordCutPlane = GetComponent<SwordCutPlane>();
        if (swordCutPlane == null)
        {
            Debug.LogError("[SwordHit] SwordCutPlane component not found!");
        }

        // Добавляем тег Sword если его нет
        if (string.IsNullOrEmpty(gameObject.tag) || gameObject.tag == "Untagged")
        {
            gameObject.tag = "Sword";
        }

        lastPos = transform.position;
    }

    void Update()
    {
        velocity = (transform.position - lastPos) / Time.deltaTime;
        lastPos = transform.position;
    }

    void OnTriggerStay(Collider other)
    {
        // ❌ 1. ЖЁСТКИЙ ФИЛЬТР РУК
        if (other.CompareTag("XRHand"))
            return;

        // ❌ 2. ЖЁСТКИЙ ФИЛЬТР МЕЧА / XR RIG
        if (other.transform.root.CompareTag("Player"))
            return;

        // Проверяем на ProgressiveSlicer (новая система)
        if (other.GetComponent("ProgressiveSlicer") != null)
        {
            // ProgressiveSlicer сам обрабатывает столкновения
            return;
        }

        // Старый способ с Cuttable (для обратной совместимости)
        Cuttable cuttable = other.GetComponent<Cuttable>();
        if (!cuttable)
            return;

        float speed = velocity.magnitude;

        if (Time.time - lastHitTime < cooldown)
            return;

        if (speed < minHitSpeed)
            return;

        lastHitTime = Time.deltaTime;

        // 🔪 РАЗРЕЗАЕМ
        if (swordCutPlane != null)
        {
            Plane cutPlane = swordCutPlane.GetCutPlane();
            cuttable.Cut(cutPlane);

            // 💥 VFX
            if (hitVFX != null)
            {
                Vector3 hitPos = other.ClosestPoint(transform.position);
                Instantiate(hitVFX, hitPos, Quaternion.identity);
            }

            // 🧠 LOG
            if (logLevel == LogLevel.HitsOnly)
            {
                Debug.Log("[🔪 SLICE] " + other.name + " speed=" + speed.ToString("F2"));
            }
            else if (logLevel == LogLevel.Debug)
            {
                Debug.Log("[🔪 SLICE DEBUG] " + other.name + 
                    " speed=" + speed.ToString("F2") + 
                    " pos=" + transform.position);
            }
        }
    }
}
