using UnityEngine;

public class SwordCutPlane : MonoBehaviour
{
    [Header("Blade References")]
    public Transform bladeTip;
    public Transform bladeBase;

    [Header("Slice Plane Position")]
    [Tooltip("Где находится плоскость разреза: 0 = у основания, 0.5 = посередине, 1 = у кончика")]
    [Range(0f, 1f)]
    public float planePosition = 0.5f;

    [Tooltip("Угол поворота плоскости разреза (в градусах)")]
    [Range(-180f, 180f)]
    public float planeRotation = 0f;

    [Header("Blade Orientation")]
    [Tooltip("Дополнительный ориентир направления плоскости меча. Если задан, используйте локальный объект на лезвии.")]
    public Transform bladeSide;

    [Header("Debug")]
    public bool visualizePlane = true;

    public Vector3 GetPlanePosition()
    {
        if (bladeTip == null || bladeBase == null)
            return transform.position;

        return Vector3.Lerp(bladeBase.position, bladeTip.position, planePosition);
    }

    public Plane GetCutPlane()
    {
        if (bladeTip == null || bladeBase == null)
        {
            Debug.LogError("[SwordCutPlane] Missing blade references!");
            return new Plane(Vector3.up, transform.position);
        }

        // Направление от основания к кончику меча
        Vector3 bladeDir = (bladeTip.position - bladeBase.position).normalized;
        
        // Позиция плоскости между основанием и кончиком
        Vector3 planePos = GetPlanePosition();
        
        // НОРМАЛЬ плоскости должна соответствовать плоскости лезвия меча
        // Для этого используем дополнительный ориентир bladeSide, если он задан.
        Vector3 bladeSideDir;
        if (bladeSide != null)
        {
            bladeSideDir = (bladeSide.position - bladeTip.position).normalized;
        }
        else
        {
            // По умолчанию берём локальную ось меча, которая лежит в плоскости лезвия
            bladeSideDir = transform.up;
        }

        bladeSideDir = Vector3.ProjectOnPlane(bladeSideDir, bladeDir).normalized;
        if (bladeSideDir.sqrMagnitude < 0.001f)
        {
            bladeSideDir = Vector3.ProjectOnPlane(transform.right, bladeDir).normalized;
        }

        Vector3 planeNormal = Vector3.Cross(bladeDir, bladeSideDir).normalized;

        if (planeNormal.sqrMagnitude < 0.001f)
        {
            // fallback: если меч ориентирован строго вертикально, используем локальную ось right
            bladeSideDir = Vector3.ProjectOnPlane(transform.right, bladeDir).normalized;
            planeNormal = Vector3.Cross(bladeDir, bladeSideDir).normalized;
        }

        // Применяем поворот плоскости вокруг направления лезвия
        if (planeRotation != 0f)
        {
            Quaternion rotation = Quaternion.AngleAxis(planeRotation, bladeDir);
            planeNormal = rotation * planeNormal;
        }

        return new Plane(planeNormal, planePos);
    }

    public Vector3 GetBladeDirection()
    {
        if (bladeTip == null || bladeBase == null)
            return transform.forward;

        return (bladeTip.position - bladeBase.position).normalized;
    }

    void OnDrawGizmos()
    {
        if (!visualizePlane || bladeTip == null || bladeBase == null)
            return;

        // Направление от основания к кончику меча
        Vector3 bladeDir = (bladeTip.position - bladeBase.position).normalized;

        // Рисуем направление меча
        Gizmos.color = Color.red;
        Gizmos.DrawLine(bladeBase.position, bladeTip.position);
        Gizmos.DrawWireSphere(bladeTip.position, 0.05f);
        Gizmos.DrawWireSphere(bladeBase.position, 0.05f);

        // Рисуем плоскость разреза
        Plane plane = GetCutPlane();
        Vector3 normal = plane.normal;
        Vector3 pos = GetPlanePosition();

        Gizmos.color = Color.yellow;
        
        // Две ориентирующие линии на плоскости
        Vector3 right = Vector3.Cross(normal, bladeDir).normalized;
        Vector3 up = Vector3.Cross(right, normal).normalized;

        float size = 0.3f;
        Gizmos.DrawLine(pos + right * size, pos - right * size);
        Gizmos.DrawLine(pos + up * size, pos - up * size);

        // Показываем направление меча и плоскость лезвия
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(pos, pos + bladeDir * 0.2f);
        if (bladeSide != null)
        {
            Gizmos.DrawLine(pos, pos + (bladeSide.position - bladeTip.position).normalized * 0.2f);
        }

        // Визуализация позиции плоскости на мече
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(pos, 0.08f);
    }
}
