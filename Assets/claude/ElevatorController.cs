using UnityEngine;

/// <summary>
/// 电梯控制器 —— 攻击后触发电梯平台移动（与 MovingPlatform 的 Elevator 模式配合）。
///
/// 用法：
///   1. 场景中建空物体，挂本脚本 + 一个 Trigger Collider（BoxCollider2D，IsTrigger=true）。
///   2. 把电梯平台（挂 MovingPlatform，且模式为 Elevator）拖到 _targetPlatform。
///   3. 可多个控制器都指向同一个平台：攻击任意一个都会切换平台移动/返回。
///
/// 逻辑：
///   攻击控制器 → 平台若在起点则移向尽头；若在尽头则返回起点；移动中攻击无效。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ElevatorController : MonoBehaviour
{
    [Header("【关联的电梯平台】")]
    [Tooltip("拖入要控制的电梯平台（该物体需挂 MovingPlatform 且模式为 Elevator）。留空则自动查找父物体上的 MovingPlatform")]
    [SerializeField] private MovingPlatform _targetPlatform;

    void Start()
    {
        // 确保碰撞体为 Trigger，用于检测武器命中
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;

        // 注意：这里【不】加 Rigidbody2D。
        // 因为控制器作为电梯平台的子物体时，若挂 Static 刚体会被物理引擎视为静止几何，
        // 导致不跟随父级移动。触发检测由玩家/武器侧的 Dynamic 刚体满足，控制器无需刚体。

        // 未手动指定平台时，自动查找父物体上的 MovingPlatform（便于作为电梯子物体直接使用）
        if (_targetPlatform == null)
        {
            _targetPlatform = GetComponentInParent<MovingPlatform>();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[ElevatorController] {gameObject.name} 被碰撞：{other.name}");

        // 检测武器命中（完全对齐 OneWayDoor 的判定：只认 playerweapon 组件，不做 tag/layer 过滤）
        playerweapon weapon = other.GetComponent<playerweapon>();
        if (weapon == null)
        {
            Debug.Log($"[ElevatorController] {gameObject.name} 碰撞体 {other.name} 上没有 playerweapon，忽略");
            return;
        }

        Debug.Log($"[ElevatorController] {gameObject.name} 检测到武器命中，准备触发电梯");

        if (_targetPlatform == null)
        {
            Debug.LogWarning("[ElevatorController] " + gameObject.name + " 未设置 _targetPlatform，且父物体上没有 MovingPlatform！");
            return;
        }

        _targetPlatform.ToggleElevator();
    }

    #region Scene 视图调试绘制

    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);

        if (_targetPlatform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, _targetPlatform.transform.position);
            UnityEditor.Handles.Label(
                (transform.position + _targetPlatform.transform.position) * 0.5f + Vector3.up * 0.3f,
                "电梯控制器 → " + _targetPlatform.name);
        }
    }
    #endif

    #endregion
}
