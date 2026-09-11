using UnityEngine;

/// <summary>
/// 房间传送门 —— 玩家靠近后显示交互提示，按交互键（默认 E）才传送。
/// 与 RoomConnectionManager 配合使用，支持"门对门"双向传送（可配成一次性单向）。
/// </summary>
public class RoomDoor : MonoBehaviour
{
    [Header("═══ 门标识 ═══")]
    [Tooltip("此门的唯一ID，用于在管理器中配对")]
    public string doorId = "Door_01";

    [Header("═══ 所属摄像机区域 ═══")]
    [Tooltip("此门所属 CameraZone 的 zoneId，玩家到达此门时切换到此区域")]
    public string zoneId = "";

    [Header("═══ 一次性传送 ═══")]
    [Tooltip("此门是否已经用过（由管理器在传送时标记，用过即失效）")]
    public bool used = false;

    [Header("═══ 连接类型 ═══")]
    [Tooltip("水平连接：房间左右相邻，传送后保持玩家当前移动速度\n垂直上行：房间上下相邻（从下层传到上层），传送后给玩家额外向上速度防止掉回去")]
    public ConnectionType connectionType = ConnectionType.Horizontal;

    [Header("═══ 垂直上行参数（仅 VerticalUp 模式生效） ═══")]
    [Tooltip("传送到达后给玩家的水平速度（正值=向右，负值=向左）")]
    [SerializeField] private float _arrivalVelocityX = 3f;

    [Tooltip("传送到达后给玩家的垂直速度（正值=向上，建议8左右）")]
    [SerializeField] private float _arrivalVelocityY = 8f;

    [Header("═══ 交互设置 ═══")]
    [Tooltip("触发传送的按键")]
    public KeyCode interactKey = KeyCode.E;

    [Tooltip("靠近时显示的提示文字")]
    public string promptText = "按 E 交互";

    /// <summary>连接类型枚举</summary>
    public enum ConnectionType
    {
        Horizontal, // 水平连接：房间左右相邻
        VerticalUp   // 垂直上行：从下层传到上层
    }

    // 公开属性供管理器读取
    public float ArrivalVelocityX => _arrivalVelocityX;
    public float ArrivalVelocityY => _arrivalVelocityY;

    private bool _playerInRange = false; // 玩家是否在门的触发范围内

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

    void Update()
    {
        // 三个条件都满足才传送：玩家在范围内、门未用过、按下交互键
        if (!_playerInRange) return;
        if (used) return;
        if (!Input.GetKeyDown(interactKey)) return;

        // 按下交互键后先隐藏提示，避免黑屏时提示还残留
        HidePrompt();

        // 通知管理器执行传送
        if (RoomConnectionManager.Instance != null)
            RoomConnectionManager.Instance.OnPlayerEnterDoor(this);
        else
            Debug.LogWarning("[RoomDoor] RoomConnectionManager 实例不存在，无法传送！");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = true;

        // 门没用过才显示提示
        if (!used) ShowPrompt();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = false;
        HidePrompt();
    }

    // ═══ 提示显示/隐藏 ═══

    void ShowPrompt()
    {
        if (DoorPromptText.Instance != null)
            DoorPromptText.Instance.Show(promptText);
        else
            Debug.Log($"[RoomDoor] {promptText}"); // 场景里没挂 DoorPromptText 时用日志占位
    }

    void HidePrompt()
    {
        if (DoorPromptText.Instance != null)
            DoorPromptText.Instance.Hide();
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
