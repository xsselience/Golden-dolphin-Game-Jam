using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 房间传送管理器 —— 维护所有"门对门"配对，处理传送流程
///
/// 使用方式：
///   1. 场景中创建空物体 "RoomConnectionManager"，挂载此脚本
///   2. 每个房间出口放空物体挂 RoomDoor，设置 zoneId
///   3. 在 Inspector 的 doorPairs 列表中将两个门拖入配对即可
///      （不需要手动设置 doorId，直接拖引用就行）
///
/// 传送逻辑：
///   - 水平连接：保持玩家当前移动速度，自然走入目标房间
///   - 垂直上行：给玩家额外速度防止掉回下层
/// </summary>
public class RoomConnectionManager : MonoBehaviour
{
    [Header("═══ 黑屏过渡 ═══")]
    [SerializeField] private Image _fadeImage;
    [SerializeField] private float _fadeOutDuration = 0.4f;
    [SerializeField] private float _holdDuration = 0.15f;
    [SerializeField] private float _fadeInDuration = 0.4f;

    [Header("═══ 门配对列表 ═══")]
    [SerializeField] private List<RoomDoorPair> _doorPairs = new List<RoomDoorPair>();

    [Header("═══ 调试 ═══")]
    [SerializeField] private bool _debugLog = true;

    /// <summary>门配对结构 —— 拖入两个RoomDoor引用即完成配对</summary>
    [Serializable]
    public struct RoomDoorPair
    {
        [Tooltip("配对名称（如 Room1_Room2），方便管理，可不填")]
        public string pairId;
        [Tooltip("门A")]
        public RoomDoor doorA;
        [Tooltip("门B")]
        public RoomDoor doorB;
    }

    // 快速查找：RoomDoor引用 → 它的配对门
    // 用引用做key，不依赖doorId字符串，避免同名/空名导致的配对错误
    private Dictionary<RoomDoor, RoomDoor> _pairedDoorMap;
    private bool _isTransitioning;

    public static RoomConnectionManager Instance { get; private set; }

    #region 单例初始化

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        BuildPairedDoorMap();
    }

    void Start()
    {
        if (_fadeImage != null)
        {
            _fadeImage.color = Color.clear;
            _fadeImage.raycastTarget = false;
            _fadeImage.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 构建 RoomDoor引用 → 配对门 的快速查找字典
    /// 用引用做key，每个门拿到的配对门一定是另一个，不会指向自己
    /// </summary>
    private void BuildPairedDoorMap()
    {
        _pairedDoorMap = new Dictionary<RoomDoor, RoomDoor>();

        foreach (RoomDoorPair pair in _doorPairs)
        {
            if (pair.doorA == null || pair.doorB == null) continue;
            if (pair.doorA == pair.doorB)
            {
                Debug.LogWarning($"[RoomConnectionManager] 配对 '{pair.pairId}' 的两个门是同一个物体，已跳过");
                continue;
            }

            // 双向注册：doorA → doorB，doorB → doorA
            if (!_pairedDoorMap.ContainsKey(pair.doorA))
                _pairedDoorMap.Add(pair.doorA, pair.doorB);
            else
                Debug.LogWarning($"[RoomConnectionManager] 门 {pair.doorA.name} 出现在多个配对中，保留第一个");

            if (!_pairedDoorMap.ContainsKey(pair.doorB))
                _pairedDoorMap.Add(pair.doorB, pair.doorA);
            else
                Debug.LogWarning($"[RoomConnectionManager] 门 {pair.doorB.name} 出现在多个配对中，保留第一个");
        }

        if (_debugLog)
            Debug.Log($"[RoomConnectionManager] 已构建 {_pairedDoorMap.Count} 个门的配对映射（{_doorPairs.Count} 个配对组）");
    }

    #endregion

    #region 传送入口

    /// <summary>
    /// 玩家进入某个门时调用（由 RoomDoor.OnTriggerEnter2D 触发）
    /// </summary>
    public void OnPlayerEnterDoor(RoomDoor enteredDoor)
    {
        if (_isTransitioning) return;
        if (enteredDoor == null) return;

        // 直接用引用查找配对门
        if (!_pairedDoorMap.TryGetValue(enteredDoor, out RoomDoor targetDoor))
        {
            Debug.LogWarning($"[RoomConnectionManager] 门 '{enteredDoor.name}' (doorId={enteredDoor.doorId}) 未在 doorPairs 中配对！");
            return;
        }

        StartCoroutine(TransitionRoutine(enteredDoor, targetDoor));
    }

    #endregion

    #region 传送流程协程

    IEnumerator TransitionRoutine(RoomDoor fromDoor, RoomDoor toDoor)
    {
        _isTransitioning = true;

        // 禁用两端的门，防止传送期间重复触发
        Collider2D fromCol = fromDoor.GetComponent<Collider2D>();
        Collider2D toCol = toDoor.GetComponent<Collider2D>();
        if (fromCol != null) fromCol.enabled = false;
        if (toCol != null) toCol.enabled = false;

        if (_debugLog)
        {
            string typeStr = toDoor.connectionType == RoomDoor.ConnectionType.VerticalUp ? "垂直上行" : "水平";
            Debug.Log($"[RoomConnectionManager] 传送({typeStr})：{fromDoor.name} → {toDoor.name}");
        }

        // ═══ 阶段1：黑屏淡入 ═══
        yield return StartCoroutine(FadeToBlack(_fadeOutDuration));

        // ═══ 阶段2：保存玩家当前速度 + 移动玩家 ═══
        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
        Rigidbody2D playerRb = null;
        float savedVelocityX = 0f;

        if (player != null)
        {
            playerRb = player.GetComponent<Rigidbody2D>();

            // 保存玩家进入门前一刻的水平速度
            if (playerRb != null)
            {
                savedVelocityX = playerRb.velocity.x;
                playerRb.velocity = Vector2.zero;
            }

            // 传送到目标门的位置
            player.position = toDoor.transform.position;
        }

        // ═══ 阶段3：切换摄像机区域（传送门与房间一一对应） ═══
        if (!string.IsNullOrEmpty(toDoor.zoneId) && CameraZoneManager.Instance != null)
            CameraZoneManager.Instance.ForceSwitchZone(toDoor.zoneId);

        // ═══ 阶段4：根据连接类型设置玩家速度 ═══
        if (playerRb != null)
        {
            if (toDoor.connectionType == RoomDoor.ConnectionType.VerticalUp)
            {
                // 垂直上行：给玩家向上的初速度 + 水平速度，确保落到上层平台
                float vx = toDoor.ArrivalVelocityX;
                float vy = toDoor.ArrivalVelocityY;
                playerRb.velocity = new Vector2(vx, vy);
            }
            else
            {
                // 水平连接：恢复玩家进入门前保存的水平速度
                playerRb.velocity = new Vector2(savedVelocityX, 0f);
            }
        }

        // ═══ 阶段5：等待 ═══
        if (_holdDuration > 0)
            yield return new WaitForSecondsRealtime(_holdDuration);

        // ═══ 阶段6：黑屏淡出 ═══
        yield return StartCoroutine(FadeToClear(_fadeInDuration));

        // ═══ 阶段7：恢复碰撞体 ═══
        if (fromCol != null) fromCol.enabled = true;
        if (toCol != null) toCol.enabled = true;

        _isTransitioning = false;

        if (_debugLog)
            Debug.Log($"[RoomConnectionManager] 传送完成 → {toDoor.name}");
    }

    #endregion

    #region 黑屏辅助

    IEnumerator FadeToBlack(float duration)
    {
        if (_fadeImage == null)
        {
            yield return new WaitForSecondsRealtime(duration);
            yield break;
        }

        if (!_fadeImage.gameObject.activeSelf)
        {
            _fadeImage.raycastTarget = false;
            _fadeImage.color = Color.clear;
            _fadeImage.gameObject.SetActive(true);
        }

        float elapsed = 0f;
        Color start = _fadeImage.color;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _fadeImage.color = Color.Lerp(start, Color.black, elapsed / duration);
            yield return null;
        }
        _fadeImage.color = Color.black;
    }

    IEnumerator FadeToClear(float duration)
    {
        if (_fadeImage == null)
        {
            yield return new WaitForSecondsRealtime(duration);
            yield break;
        }

        float elapsed = 0f;
        Color start = _fadeImage.color;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _fadeImage.color = Color.Lerp(start, Color.clear, elapsed / duration);
            yield return null;
        }

        _fadeImage.color = Color.clear;
        _fadeImage.gameObject.SetActive(false);
    }

    #endregion

    #region Editor 可视化

    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        foreach (RoomDoorPair pair in _doorPairs)
        {
            if (pair.doorA != null && pair.doorB != null)
            {
                // 根据类型选颜色
                bool isVertical = pair.doorA.connectionType == RoomDoor.ConnectionType.VerticalUp
                               || pair.doorB.connectionType == RoomDoor.ConnectionType.VerticalUp;
                Gizmos.color = isVertical ? Color.cyan : Color.magenta;
                Gizmos.DrawLine(pair.doorA.transform.position, pair.doorB.transform.position);

                Vector3 mid = (pair.doorA.transform.position + pair.doorB.transform.position) * 0.5f;
                string label = string.IsNullOrEmpty(pair.pairId)
                    ? $"{pair.doorA.name} ↔ {pair.doorB.name}"
                    : pair.pairId;
                if (isVertical) label = "↑ " + label;
                UnityEditor.Handles.Label(mid + Vector3.up * 0.3f, label);
            }
        }
    }
    #endif

    #endregion
}
