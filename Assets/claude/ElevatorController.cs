using UnityEngine;

/// <summary>
/// 电梯控制器 —— 黑入模式下，玩家靠近后按交互键（默认 E）触发电梯平台移动。
/// 与 MovingPlatform 的 Elevator 模式配合。
///
/// 交互流程：
///   1. 玩家进入黑入模式（按 C），本控制器高亮（青色），表示"可交互"
///   2. 玩家靠近控制器，出现提示
///   3. 按交互键（E）→ 电梯移动（起点→尽头，或尽头→起点）
///
/// 用法：
///   1. 场景中建空物体，挂本脚本 + 一个 Trigger Collider（BoxCollider2D，IsTrigger=true）
///   2. 把电梯平台（挂 MovingPlatform 且模式为 Elevator）拖到 _targetPlatform（或留空自动找父级）
///   3. 可选：给控制器挂一个 SpriteRenderer，用于黑入高亮变色
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ElevatorController : MonoBehaviour
{
    [Header("【关联的电梯平台】")]
    [Tooltip("拖入要控制的电梯平台（需挂 MovingPlatform 且模式为 Elevator）。留空则自动查找父物体")]
    [SerializeField] private MovingPlatform _targetPlatform;

    [Header("【交互设置】")]
    [Tooltip("触发电梯的按键")]
    public KeyCode interactKey = KeyCode.E;

    [Tooltip("靠近时显示的提示文字")]
    public string promptText = "按 E 触发";

    [Header("【黑入高亮（可选）】")]
    [Tooltip("控制器的 SpriteRenderer，用于黑入时变色，没有可不填")]
    [SerializeField] private SpriteRenderer sr;

    [Tooltip("未黑入时的颜色")]
    [SerializeField] private Color inactiveColor = Color.gray;

    [Tooltip("黑入模式下高亮颜色")]
    [SerializeField] private Color hackHighlight = Color.cyan;

    private player _player;          // 缓存的玩家引用
    private bool _playerInRange;     // 玩家是否在触发范围内
    private bool _hackHighlight;     // 是否处于黑入高亮状态

    void Start()
    {
        // 确保碰撞体为 Trigger，玩家可穿过并触发
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;

        // 黑入高亮用的 SpriteRenderer（没有则跳过，不影响交互功能）
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = inactiveColor;

        // 未手动指定平台时，自动查找父物体上的 MovingPlatform
        if (_targetPlatform == null)
            _targetPlatform = GetComponentInParent<MovingPlatform>();
    }

    void Update()
    {
        // 三个条件都满足才触发：玩家在范围内、黑入高亮中、按下交互键
        if (!_playerInRange || !_hackHighlight) return;
        if (!Input.GetKeyDown(interactKey)) return;

        HidePrompt();

        if (_targetPlatform != null)
            _targetPlatform.ToggleElevator();
        else
            Debug.LogWarning("[ElevatorController] " + gameObject.name + " 未设置电梯平台！");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _player = other.GetComponent<player>();
        _playerInRange = true;
        UpdatePrompt();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = false;
        _player = null;
        UpdatePrompt();
    }

    /// <summary>黑入模式进入/退出时由 player 调用，控制高亮和提示显示</summary>
    public void SetForHack(bool on)
    {
        _hackHighlight = on;
        if (sr != null) sr.color = on ? hackHighlight : inactiveColor;
        UpdatePrompt();
    }

    /// <summary>根据"玩家在范围内 + 黑入高亮"决定是否显示提示</summary>
    void UpdatePrompt()
    {
        if (_playerInRange && _hackHighlight)
            ShowPrompt();
        else
            HidePrompt();
    }

    void ShowPrompt()
    {
        if (DoorPromptText.Instance != null)
            DoorPromptText.Instance.Show(promptText);
        else
            Debug.Log($"[ElevatorController] {promptText}"); // 没挂 DoorPromptText 时用日志占位
    }

    void HidePrompt()
    {
        if (DoorPromptText.Instance != null)
            DoorPromptText.Instance.Hide();
    }

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
        }
    }
    #endif
}
