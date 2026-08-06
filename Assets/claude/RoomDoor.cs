using UnityEngine;

/// <summary>
/// 房间传送门 —— 放在房间出口处，玩家触碰后自动传送到配对的另一个门
/// 与 RoomConnectionManager 配合使用，支持"门对门"双向传送
/// 传送门与房间（CameraZone）一一对应：zoneId 决定摄像机切换到哪个区域
/// </summary>
public class RoomDoor : MonoBehaviour
{
    [Header("═══ 门标识 ═══")]
    [Tooltip("此门的唯一ID，用于在管理器中配对")]
    public string doorId = "Door_01";

    [Header("═══ 所属摄像机区域 ═══")]
    [Tooltip("此门所属 CameraZone 的 zoneId，玩家到达此门时切换到此区域")]
    public string zoneId = "";

    [Header("═══ 连接类型 ═══")]
    [Tooltip("水平连接：房间左右相邻，传送后保持玩家当前移动速度\n垂直上行：房间上下相邻（从下层传到上层），传送后给玩家额外向上速度防止掉回去")]
    public ConnectionType connectionType = ConnectionType.Horizontal;

    [Header("═══ 垂直上行参数（仅 VerticalUp 模式生效） ═══")]
    [Tooltip("传送到达后给玩家的水平速度（正值=向右，负值=向左）")]
    [SerializeField] private float _arrivalVelocityX = 3f;

    [Tooltip("传送到达后给玩家的垂直速度（正值=向上，建议8左右）")]
    [SerializeField] private float _arrivalVelocityY = 8f;

    /// <summary>连接类型枚举</summary>
    public enum ConnectionType
    {
        Horizontal, // 水平连接：房间左右相邻
        VerticalUp   // 垂直上行：从下层传到上层
    }

    // 公开属性供管理器读取
    public float ArrivalVelocityX => _arrivalVelocityX;
    public float ArrivalVelocityY => _arrivalVelocityY;

    void Start()
    {
        // 确保碰撞体为 Trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
            col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        // 确保有 Kinematic 刚体
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (RoomConnectionManager.Instance != null)
            RoomConnectionManager.Instance.OnPlayerEnterDoor(this);
        else
            Debug.LogWarning("[RoomDoor] RoomConnectionManager 实例不存在，无法传送！");
    }

    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // 触发区域可视化
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            // 根据类型选颜色
            Color areaColor = connectionType == ConnectionType.Horizontal
                ? new Color(1f, 0.5f, 0f, 0.2f)   // 橙色 = 水平
                : new Color(0.5f, 0.5f, 1f, 0.2f); // 蓝色 = 垂直上行

            Gizmos.color = areaColor;
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
            Gizmos.color = connectionType == ConnectionType.Horizontal ? Color.yellow : Color.cyan;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }

        // 标签
        string typeIcon = connectionType == ConnectionType.Horizontal ? "↔" : "↑";
        string label = $"{typeIcon} {doorId}";
        if (!string.IsNullOrEmpty(zoneId))
            label += $" [{zoneId}]";
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, label);
    }
    #endif
}
