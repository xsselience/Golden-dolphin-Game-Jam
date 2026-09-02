using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [Header("【移动方向】左右移动填 Vector2.right")]
    [SerializeField] private Vector2 _moveDirection = Vector2.right;
    [Header("移动速度 单位/秒")]
    [SerializeField] private float _moveSpeed = 2f;
    [Header("开启往返自动折返")]
    [SerializeField] private bool _pingPong = true;
    [Header("单向移动最大距离")]
    [SerializeField] private float _pingPongDistance = 3f;

    [Header("【平台模式】")]
    [Tooltip("Normal=普通往返平台（保持原逻辑不动）；Elevator=电梯模式（攻击控制器后移动，到尽头停止，再攻击返回）")]
    [SerializeField] private PlatformMode _mode = PlatformMode.Normal;

    [Header("【电梯模式参数】")]
    [Tooltip("电梯移动的最大距离（世界单位），到达即尽头并停止")]
    [SerializeField] private float _elevatorDistance = 3f;
    [Tooltip("首次攻击后电梯的移动方向（上/下/左/右）")]
    [SerializeField] private ElevatorDirection _elevatorFirstDirection = ElevatorDirection.Up;

    [Header("玩家检测设置")]
    [Tooltip("平台顶面采样射线数量，宽平台调高")]
    public int sampleCount = 12;
    [Tooltip("射线向下探测长度")]
    public float checkHeight = 0.35f;
    [Tooltip("射线起点高出平台顶面距离")]
    public float rayUpOffset = 0.1f;

    [Header("【调试】")]
    [SerializeField] private bool _debugLog = false;

    private Rigidbody2D _rb;
    private Collider2D _platformCol;   // 真正承载玩家的碰撞体（可能在子物体上）
    private float _traveledDistance;
    private int _currentDir = 1;
    private readonly List<Rigidbody2D> _riders = new List<Rigidbody2D>();
    private Vector2 _platformVelocity; // 平台当前速度（世界空间），注入给玩家
    private int _playerLayerMask;      // 玩家层 mask（运行时自动解析）

    // 电梯模式状态
    private bool _elevatorActive;
    private int _elevatorDir = 1;
    private float _elevatorTravel;

    public enum PlatformMode { Normal, Elevator }
    public enum ElevatorDirection { Up, Down, Left, Right }

    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb == null)
            _rb = gameObject.AddComponent<Rigidbody2D>();

        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        _rb.gravityScale = 0;
        _rb.useFullKinematicContacts = true;

        // 关键修复：碰撞体可能挂在【子物体】上（本物体只有脚本）。
        // 用 GetComponentInChildren 找真正的平台碰撞体；找不到才在本物体上造一个。
        _platformCol = FindPlatformCollider();

        // 解析玩家层：优先 player 层，不存在则用 Player 标签物体的 layer
        int playerIdx = LayerMask.NameToLayer("player");
        if (playerIdx >= 0)
            _playerLayerMask = 1 << playerIdx;
        else
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            _playerLayerMask = (p != null) ? (1 << p.layer) : -1; // -1 = 所有层
        }
    }

    /// <summary>
    /// 找到真正承载玩家的平台碰撞体：优先本物体，其次遍历子物体取包围盒面积最大的那个。
    /// </summary>
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
        Vector2 dirNormalized = _moveDirection.magnitude > 0.001f ? _moveDirection.normalized : Vector2.right;
        Vector2 deltaMove = Vector2.zero;

        if (_mode == PlatformMode.Elevator)
        {
            if (_elevatorActive)
            {
                float step = _moveSpeed * Time.fixedDeltaTime;
                _elevatorTravel += step * _elevatorDir;

                if (_elevatorTravel <= 0f)
                {
                    _elevatorTravel = 0f;
                    _elevatorActive = false;
                    _elevatorDir = 1;
                }
                else if (_elevatorTravel >= _elevatorDistance)
                {
                    _elevatorTravel = _elevatorDistance;
                    _elevatorActive = false;
                }
                else
                {
                    deltaMove = GetElevatorMoveDirection() * (step * _elevatorDir);
                }
            }
        }
        else
        {
            float moveStep = _moveSpeed * Time.fixedDeltaTime * _currentDir;
            deltaMove = dirNormalized * moveStep;

            if (_pingPong)
            {
                _traveledDistance += moveStep;
                if (Mathf.Abs(_traveledDistance) >= _pingPongDistance)
                {
                    float overflow = Mathf.Abs(_traveledDistance) - _pingPongDistance;
                    _traveledDistance = Mathf.Sign(_traveledDistance) * (_pingPongDistance - overflow);
                    _currentDir *= -1;
                }
            }
        }

        // 移动平台本体，并计算平台当前速度
        _rb.MovePosition(_rb.position + deltaMove);
        _platformVelocity = deltaMove / Time.fixedDeltaTime;

        // 射线检测谁站在平台上
        HashSet<Rigidbody2D> curPlayers = CheckAllPlayersOnPlatform();

        // 清理失效骑手（离开平台的玩家，清除注入的平台速度）
        for (int i = _riders.Count - 1; i >= 0; i--)
        {
            Rigidbody2D rider = _riders[i];
            if (rider == null || !rider.gameObject.activeInHierarchy || !curPlayers.Contains(rider))
            {
                ApplyPlatformVelocity(rider, Vector2.zero);
                _riders.RemoveAt(i);
            }
        }

        // 添加新踩上平台的玩家，并注入平台速度
        foreach (var rb in curPlayers)
        {
            if (!_riders.Contains(rb))
                _riders.Add(rb);
        }

        // 每帧把平台速度写入所有骑手，由 player.move() 在下一帧叠加
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
            p.platformVelocity = velocity;
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

    private Vector2 GetElevatorMoveDirection()
    {
        switch (_elevatorFirstDirection)
        {
            case ElevatorDirection.Up: return Vector2.up;
            case ElevatorDirection.Down: return Vector2.down;
            case ElevatorDirection.Left: return Vector2.left;
            default: return Vector2.right;
        }
    }

    /// <summary>电梯模式：攻击控制器后调用。尽头则返回起点，起点则去尽头；移动中调用无效。</summary>
    public void ToggleElevator()
    {
        if (_mode != PlatformMode.Elevator) return;
        if (_elevatorActive) return;

        _elevatorDir = (_elevatorTravel >= _elevatorDistance - 0.001f) ? -1 : 1;
        _elevatorActive = true;
    }

    void OnDestroy()
    {
        // 平台销毁时，清除所有骑手注入的平台速度
        for (int i = _riders.Count - 1; i >= 0; i--)
        {
            ApplyPlatformVelocity(_riders[i], Vector2.zero);
        }
        _riders.Clear();
    }

    void OnDrawGizmosSelected()
    {
        if (_platformCol == null) _platformCol = FindPlatformCollider();
        if (_platformCol == null) return;
        Bounds b = _platformCol.bounds;
        Gizmos.color = Color.yellow;
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
