using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 旋转平台 - 围绕固定旋转中心做圆周运动
/// 平台倾斜角度过大时玩家会自动滑落
/// 可在Inspector面板设置旋转中心、速度、方向、是否保持水平等参数
/// </summary>
public class RotatingPlatform : MonoBehaviour
{
    [Header("【旋转中心参考物】拖入一个GameObject作为旋转中心（优先使用），留空则使用下方的世界坐标")]
    [SerializeField] private Transform _pivotTransform;

    [Header("【旋转中心世界坐标】当上方参考物为空时生效")]
    [SerializeField] private Vector2 _pivotWorldPosition;

    [Header("【旋转速度】每秒旋转的度数")]
    [SerializeField] private float _rotationSpeed = 30f;

    [Header("【顺时针旋转】true=顺时针, false=逆时针")]
    [SerializeField] private bool _clockwise = true;

    [Header("【保持固定倾斜】勾选后平台不随圆周旋转、保持固定角度（像摩天轮车厢），不勾选则随旋转倾斜")]
    [SerializeField] private bool _maintainHorizontal = false;

    [Header("【固定倾斜角度】上方勾选时生效：平台固定的角度（度）。0=水平，正数=逆时针倾斜，负数=顺时针倾斜")]
    [SerializeField] private float _fixedTiltAngle = 0f;

    [Header("【滑落角度阈值】平台倾斜超过此角度时玩家滑落（保持固定倾斜时按固定角度判断）")]
    [Tooltip("平台倾斜超过此角度时玩家开始滑落。建议与 player.cs 的 groundCheckRadius 可站立角度对齐（约45°），实现“跳不动就开始滑”")]
    [SerializeField] private float _slideAngleThreshold = 45f;

    [Header("【滑落力度】玩家滑落时的水平推力大小")]
    [SerializeField] private float _slideForce = 5f;

    private Rigidbody2D _rb;
    private float _currentAngle;              // 当前角度（弧度制 → 用角度存储，计算时转换）
    private float _radius;                    // 旋转半径
    private Vector2 _lastFrameDelta;          // 本帧位移（用于同步骑手）
    private Vector2 _effectivePivot;          // 实际使用的旋转中心

    // 骑手列表
    private List<RiderEntry> _riders = new List<RiderEntry>();

    private struct RiderEntry
    {
        public Transform transform;
        public Rigidbody2D rb;
    }

    void Start()
    {
        // 确定旋转中心
        _effectivePivot = (_pivotTransform != null)
            ? (Vector2)_pivotTransform.position
            : _pivotWorldPosition;

        // 初始化刚体（Kinematic模式，不响应物理力）
        _rb = GetComponent<Rigidbody2D>();
        if (_rb == null)
            _rb = gameObject.AddComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // 根据初始位置计算旋转半径和起始角度
        _radius = Vector2.Distance(transform.position, _effectivePivot);
        Vector2 startDir = (Vector2)transform.position - _effectivePivot;
        _currentAngle = Mathf.Atan2(startDir.y, startDir.x) * Mathf.Rad2Deg;
    }

    void FixedUpdate()
    {
        // 更新时间角度
        float dirSign = _clockwise ? -1f : 1f;
        _currentAngle += _rotationSpeed * Time.fixedDeltaTime * dirSign;

        // 计算圆周上的新位置
        float rad = _currentAngle * Mathf.Deg2Rad;
        Vector2 newPos = _effectivePivot + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * _radius;

        Vector2 beforeMove = _rb.position;
        _rb.MovePosition(newPos);

        // 平台自身旋转：不保持水平时，平台表面始终朝旋转中心外侧
        if (!_maintainHorizontal)
        {
            float platformAngle = _currentAngle - 90f;
            _rb.MoveRotation(platformAngle);
        }
        else
        {
            // 保持固定倾斜（摩天轮车厢式）：始终固定为指定角度，不随圆周旋转
            _rb.MoveRotation(_fixedTiltAngle);
        }

        _lastFrameDelta = _rb.position - beforeMove;
    }

    void LateUpdate()
    {
        for (int i = _riders.Count - 1; i >= 0; i--)
        {
            RiderEntry rider = _riders[i];
            if (rider.transform == null)
            {
                _riders.RemoveAt(i);
                continue;
            }

            // 计算平台当前倾斜角度（与水平面的夹角）
            float platformTilt = _maintainHorizontal
                ? Mathf.Abs(_fixedTiltAngle)
                : Vector2.Angle(transform.up, Vector2.up);

            // 超出滑落阈值 → 持续施加沿斜面向下的滑落力，让玩家滑下
            // 不立即移出骑手列表，等玩家真正离开平台时由 OnCollisionExit2D 移除
            if (platformTilt > _slideAngleThreshold)
            {
                if (rider.rb != null && !rider.rb.isKinematic)
                {
                    // 计算沿斜面向下的方向：把重力方向投影到平台表面
                    Vector2 down = Vector2.down;
                    Vector2 surfaceDown = down - Vector2.Dot(down, (Vector2)transform.up) * (Vector2)transform.up;
                    if (surfaceDown.sqrMagnitude < 0.001f)
                        surfaceDown = Vector2.down; // 平台几乎水平时兜底，直接向下
                    surfaceDown.Normalize();

                    // 施加沿斜面向下的滑落速度（每帧持续生效，直到玩家离开平台）
                    rider.rb.velocity = surfaceDown * _slideForce;
                }
                continue; // 跳过下方的平台粘连位移，让玩家自由下滑
            }

            // 安全倾斜范围内 → 骑手跟随平台移动
            if (rider.rb != null)
                rider.rb.MovePosition(rider.rb.position + _lastFrameDelta);
            else
                rider.transform.Translate(_lastFrameDelta);
        }
    }

    #region 碰撞事件 - 骑手管理

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsStandingOnTop(collision))
            TryAddRider(collision.transform);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (IsStandingOnTop(collision))
            TryAddRider(collision.transform);
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        RemoveRider(collision.transform);
    }

    private void TryAddRider(Transform t)
    {
        if (RiderExists(t)) return;
        Rigidbody2D riderRb = t.GetComponent<Rigidbody2D>();
        _riders.Add(new RiderEntry { transform = t, rb = riderRb });
    }

    private void RemoveRider(Transform t)
    {
        _riders.RemoveAll(r => r.transform == t);
    }

    private bool RiderExists(Transform t)
    {
        for (int i = 0; i < _riders.Count; i++)
            if (_riders[i].transform == t) return true;
        return false;
    }

    /// <summary>
    /// 判断碰撞体是否站在平台上方（接触法线朝上）
    /// </summary>
    private bool IsStandingOnTop(Collision2D collision)
    {
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
                return true;
        }
        return false;
    }

    #endregion

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
