using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 公转平台（OrbitingPlatform）—— 平台绕一个旋转中心做圆周运动，但自身始终保持水平（固定倾斜角），
/// 类似摩天轮车厢：车厢绕中心转圈，但车厢地板始终朝上，玩家站在上面会跟着平台一起移动。
///
/// 与 RotatingPlatform 的区别：本脚本【没有自转】（平台自身不随圆周倾斜），
/// 因此碰撞体的轴对齐包围盒（bounds）始终稳定，射线检测能正确识别玩家。
///
/// 骑手跟随逻辑与 MovingPlatform 完全一致：
///   1. 射线检测谁站在平台上（从平台顶面多点向下打射线）
///   2. 计算平台线速度
///   3. 把速度注入 player.platformVelocity，由 player.move() 叠加
/// </summary>
public class OrbitingPlatform : MonoBehaviour
{
    [Header("【旋转中心参考物】拖入一个GameObject作为旋转中心（优先使用），留空则使用下方的世界坐标")]
    [SerializeField] private Transform _pivotTransform;

    [Header("【旋转中心世界坐标】当上方参考物为空时生效")]
    [SerializeField] private Vector2 _pivotWorldPosition;

    [Header("【旋转速度】每秒旋转的度数")]
    [SerializeField] private float _rotationSpeed = 30f;

    [Header("【顺时针旋转】true=顺时针, false=逆时针")]
    [SerializeField] private bool _clockwise = true;

    [Header("【固定倾斜角度】平台始终保持的角度（度）。0=水平（摩天轮车厢效果）")]
    [SerializeField] private float _fixedTiltAngle = 0f;

    [Header("玩家检测设置（与移动平台一致）")]
    [Tooltip("平台顶面采样射线数量，宽平台调高")]
    public int sampleCount = 12;
    [Tooltip("射线向下探测长度")]
    public float checkHeight = 0.35f;
    [Tooltip("射线起点高出平台顶面距离")]
    public float rayUpOffset = 0.1f;

    [Header("【调试】")]
    [Tooltip("打印射线检测结果和速度注入日志")]
    [SerializeField] private bool _debugLog = true;
    [Tooltip("调试日志打印间隔（秒），避免每帧刷屏")]
    [SerializeField] private float _debugInterval = 0.5f;

    private Rigidbody2D _rb;
    private Collider2D _platformCol;   // 真正承载玩家的碰撞体（可能在子物体上）
    private float _currentAngle;       // 当前角度（度）
    private float _radius;             // 旋转半径
    private Vector2 _platformVelocity; // 平台当前线速度（世界空间）
    private Vector2 _effectivePivot;   // 实际旋转中心
    private readonly List<Rigidbody2D> _riders = new List<Rigidbody2D>();
    private int _playerLayerMask;      // 玩家层 mask
    private float _debugTimer;         // 调试日志计时器

    void Start()
    {
        // 确定旋转中心
        _effectivePivot = (_pivotTransform != null)
            ? (Vector2)_pivotTransform.position
            : _pivotWorldPosition;

        // 初始化刚体（Kinematic）
        _rb = GetComponent<Rigidbody2D>();
        if (_rb == null)
            _rb = gameObject.AddComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        _rb.gravityScale = 0;
        _rb.useFullKinematicContacts = true;

        // 找到真正承载玩家的碰撞体
        _platformCol = FindPlatformCollider();

        // 解析玩家层
        int playerIdx = LayerMask.NameToLayer("player");
        if (playerIdx >= 0)
            _playerLayerMask = 1 << playerIdx;
        else
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            _playerLayerMask = (p != null) ? (1 << p.layer) : -1;
        }

        // 计算旋转半径和起始角度
        _radius = Vector2.Distance(transform.position, _effectivePivot);
        Vector2 startDir = (Vector2)transform.position - _effectivePivot;
        _currentAngle = Mathf.Atan2(startDir.y, startDir.x) * Mathf.Rad2Deg;

        if (_debugLog)
        {
            Debug.Log($"[OrbitingPlatform] 初始化完成，旋转中心={_effectivePivot}，半径={_radius}，玩家层mask={_playerLayerMask}");
            Debug.Log($"[OrbitingPlatform] 平台碰撞体={(_platformCol != null ? _platformCol.name : "null")}，bounds={(_platformCol != null ? _platformCol.bounds.ToString() : "无")}");
        }
    }

    /// <summary>找到真正承载玩家的平台碰撞体：优先本物体，其次遍历子物体取包围盒面积最大的那个。</summary>
    private Collider2D FindPlatformCollider()
    {
        Collider2D self = GetComponent<Collider2D>();
        if (self != null && !self.isTrigger) return self;

        Collider2D best = null;
        float bestArea = -1f;
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>())
        {
            if (c.isTrigger) continue;
            Bounds b = c.bounds;
            float area = b.size.x * b.size.y;
            if (area > bestArea)
            {
                bestArea = area;
                best = c;
            }
        }
        return best;
    }

    void FixedUpdate()
    {
        // 更新时间角度
        float dirSign = _clockwise ? -1f : 1f;
        _currentAngle += _rotationSpeed * Time.fixedDeltaTime * dirSign;

        // 计算圆周上的新位置
        float rad = _currentAngle * Mathf.Deg2Rad;
        Vector2 newPos = _effectivePivot + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * _radius;

        _rb.MovePosition(newPos);

        // 关键：公转平台始终保持固定倾斜角（水平），不自转
        _rb.MoveRotation(_fixedTiltAngle);

        // ⭐修复：用解析公式计算线速度，而不是 rb.position 前后差。
        // MovePosition 移动的是物理体内部位置，同一帧内立即读 _rb.position 仍是旧值，
        // 导致前后差恒为 0。圆周运动切线速度 = 半径 × 角速度 × 切线方向（确定值）。
        float angularSpeed = _rotationSpeed * Mathf.Deg2Rad * dirSign; // 弧度/秒，含方向
        Vector2 tangent = new Vector2(-Mathf.Sin(rad), Mathf.Cos(rad)); // 切线方向（角度增大方向）
        _platformVelocity = _radius * angularSpeed * tangent;

        // 射线检测谁站在平台上
        HashSet<Rigidbody2D> curPlayers = CheckAllPlayersOnPlatform();

        // 清理失效骑手
        for (int i = _riders.Count - 1; i >= 0; i--)
        {
            Rigidbody2D rider = _riders[i];
            if (rider == null || !rider.gameObject.activeInHierarchy || !curPlayers.Contains(rider))
            {
                ApplyPlatformVelocity(rider, Vector2.zero);
                _riders.RemoveAt(i);
            }
        }

        // 添加新骑手
        foreach (var rb in curPlayers)
        {
            if (!_riders.Contains(rb))
                _riders.Add(rb);
        }

        // 每帧注入速度
        foreach (var rb in _riders)
        {
            if (rb != null)
                ApplyPlatformVelocity(rb, _platformVelocity);
        }

        
        
    }

    /// <summary>把平台速度注入玩家脚本（世界空间）。player.move() 会读取并叠加。</summary>
    private void ApplyPlatformVelocity(Rigidbody2D rider, Vector2 velocity)
    {
        if (rider == null) return;
        player p = rider.GetComponent<player>();
        if (p != null)
        {
            p.platformVelocity = velocity;
        }
        else if (rider.bodyType == RigidbodyType2D.Dynamic)
        {
            rider.velocity = velocity;
        }
    }

    /// <summary>多点顶面射线：从平台顶面往下射，命中玩家层即认为有玩家站在上面</summary>
    private HashSet<Rigidbody2D> CheckAllPlayersOnPlatform()
    {
        HashSet<Rigidbody2D> result = new HashSet<Rigidbody2D>();
        if (_platformCol == null) return result;

        Bounds bounds = _platformCol.bounds;
        int count = Mathf.Max(1, sampleCount);

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / (count - 1);
            Vector2 rayTopPoint = Vector2.Lerp(
                new Vector2(bounds.min.x, bounds.max.y),
                new Vector2(bounds.max.x, bounds.max.y),
                t
            );
            Vector2 rayOrigin = new Vector2(rayTopPoint.x, rayTopPoint.y + rayUpOffset);

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, checkHeight, _playerLayerMask);
            Debug.DrawRay(rayOrigin, Vector2.down * checkHeight, Color.green, Time.fixedDeltaTime);

            if (hit.collider != null && hit.rigidbody != null && hit.rigidbody.bodyType == RigidbodyType2D.Dynamic)
                result.Add(hit.rigidbody);
        }
        return result;
    }

    void OnDestroy()
    {
        for (int i = _riders.Count - 1; i >= 0; i--)
        {
            ApplyPlatformVelocity(_riders[i], Vector2.zero);
        }
        _riders.Clear();
    }

    #region Scene视图调试绘制

    void OnDrawGizmosSelected()
    {
        Vector2 pivot = (_pivotTransform != null)
            ? (Vector2)_pivotTransform.position
            : _pivotWorldPosition;

        // 旋转中心点
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pivot, 0.25f);
        Gizmos.DrawLine(pivot, transform.position);

        // 旋转路径圆
        float r = Application.isPlaying
            ? _radius
            : Vector2.Distance(transform.position, pivot);
        DrawCircle(pivot, r, 36);

        // 平台顶面射线可视化
        if (_platformCol == null) _platformCol = FindPlatformCollider();
        if (_platformCol != null)
        {
            Bounds b = _platformCol.bounds;
            Gizmos.color = Color.green;
            int count = Mathf.Max(1, sampleCount);
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / (count - 1);
                Vector2 p = Vector2.Lerp(new Vector2(b.min.x, b.max.y), new Vector2(b.max.x, b.max.y), t);
                Vector2 origin = new Vector2(p.x, p.y + rayUpOffset);
                Gizmos.DrawLine(origin, origin + Vector2.down * checkHeight);
            }
        }
    }

    private void DrawCircle(Vector2 center, float radius, int segments)
    {
        float step = 360f / segments;
        Vector2 prev = center + Vector2.right * radius;
        for (int i = 1; i <= segments; i++)
        {
            float rad = i * step * Mathf.Deg2Rad;
            Vector2 pt = center + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
            Gizmos.DrawLine(prev, pt);
            prev = pt;
        }
    }

    #endregion
}
