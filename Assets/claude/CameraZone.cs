using UnityEngine;

/// <summary>
/// 摄像机区域 v2 —— 纯边界定义
///
/// 每个 CameraZone 定义摄像机在该区域的囚笼（PolygonCollider2D）。
/// 同时可选：初始隐藏（黑色遮罩），进入后自动移除。
/// </summary>
public class CameraZone : MonoBehaviour
{
    [Header("═══ 标识 ═══")]
    public string zoneId = "Zone_01";
    public string zoneDisplayName = "";

    [Header("═══ 隐藏区域（可选） ═══")]
    [Tooltip("勾选后，初始被黑色遮罩覆盖，进入后自动移除")]
    public bool startsHidden = false;

    [Header("═══ 调试 ═══")]
    public Color gizmoColor = new Color(0f, 1f, 1f, 0.3f);

    [HideInInspector] public bool isUnlocked = false;

    public PolygonCollider2D boundsPolygon { get; private set; }

    void Awake()
    {
        boundsPolygon = GetComponent<PolygonCollider2D>();
        if (boundsPolygon == null)
        {
            Debug.LogError("[CameraZone] " + gameObject.name + " 缺少 PolygonCollider2D！");
            return;
        }
        boundsPolygon.isTrigger = true;

        if (string.IsNullOrEmpty(zoneDisplayName))
            zoneDisplayName = zoneId;
    }

    void Start()
    {
        CameraZoneManager.Instance?.RegisterZone(this);
        if (!startsHidden)
            isUnlocked = true;
    }

    public Vector2[] GetPath()
    {
        if (boundsPolygon == null) return new Vector2[0];
        return boundsPolygon.GetPath(0);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (boundsPolygon == null)
            boundsPolygon = GetComponent<PolygonCollider2D>();
        if (boundsPolygon == null) return;

        Gizmos.color = gizmoColor;
        Vector2[] pts = boundsPolygon.GetPath(0);
        if (pts.Length >= 2)
        {
            for (int i = 0; i < pts.Length; i++)
            {
                Vector3 a = transform.TransformPoint(pts[i]);
                Vector3 b = transform.TransformPoint(pts[(i + 1) % pts.Length]);
                Gizmos.DrawLine(a, b);
            }
        }

        string label = zoneDisplayName;
        if (startsHidden) label = "[隐藏] " + label;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, label);
    }
#endif
}
