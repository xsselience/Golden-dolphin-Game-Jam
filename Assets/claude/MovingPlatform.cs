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

    [Header("玩家检测设置（和消失平台逻辑一致）")]
    [Tooltip("只勾选Player层，排除地面、其他物体")]
    public LayerMask playerLayer;
    [Tooltip("平台顶面采样射线数量，宽平台调高")]
    public int sampleCount = 12;
    [Tooltip("射线向下探测长度")]
    public float checkHeight = 0.35f;
    [Tooltip("射线起点高出平台顶面距离")]
    public float rayUpOffset = 0.1f;

    private Rigidbody2D _rb;
    private BoxCollider2D _platformCol;
    private Vector2 _startPosition;
    private float _traveledDistance;
    private int _currentDir = 1;
    private readonly List<Rigidbody2D> _riders = new List<Rigidbody2D>();

    void Start()
    {
        _startPosition = transform.position;
        _rb = GetComponent<Rigidbody2D>();
        if (_rb == null)
            _rb = gameObject.AddComponent<Rigidbody2D>();

        // 平台运动刚体固定配置
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        _rb.gravityScale = 0;

        // 获取平台碰撞体，没有自动添加
        _platformCol = GetComponent<BoxCollider2D>();
        if (_platformCol == null)
            _platformCol = gameObject.AddComponent<BoxCollider2D>();
        _platformCol.isTrigger = false;
    }

    void FixedUpdate()
    {
        // 1. 计算平台本帧位移与瞬时速度
        Vector2 dirNormalized = _moveDirection.magnitude > 0.001f ? _moveDirection.normalized : Vector2.right;
        float moveStep = _moveSpeed * Time.fixedDeltaTime * _currentDir;
        Vector2 deltaMove = dirNormalized * moveStep;
        Vector2 platformVelocity = deltaMove / Time.fixedDeltaTime;

        // 往返折返逻辑
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

        // 移动平台本体
        _rb.MovePosition(_rb.position + deltaMove);

        // 多点射线检测当前所有站在平台上的玩家
        HashSet<Rigidbody2D> curPlayers = CheckAllPlayersOnPlatform();

        // 清理失效骑手 + 速度逻辑（攻击视作静止）
        for (int i = _riders.Count - 1; i >= 0; i--)
        {
            Rigidbody2D rider = _riders[i];
            if (rider == null || !rider.gameObject.activeInHierarchy || !curPlayers.Contains(rider))
            {
                _riders.RemoveAt(i);
                continue;
            }

            player playerComp = rider.GetComponent<player>();
            // 攻击锁定时，等同于静止，只跟随平台速度，不会前窜
            if (playerComp != null && playerComp.attackLocked)
            {
                rider.velocity = new Vector2(platformVelocity.x, rider.velocity.y);
            }
            else
            {
                // 正常状态叠加平台速度，可自由走动离开平台
                rider.velocity += new Vector2(platformVelocity.x, 0);
            }
        }

        // 添加新踩上平台的玩家
        foreach (var rb in curPlayers)
        {
            if (!_riders.Contains(rb))
                _riders.Add(rb);
        }
    }

    /// <summary>
    /// 复刻DisappearingPlatform多点顶面射线，返回所有站在平台上的玩家刚体
    /// </summary>
    private HashSet<Rigidbody2D> CheckAllPlayersOnPlatform()
    {
        HashSet<Rigidbody2D> result = new HashSet<Rigidbody2D>();
        Bounds bounds = _platformCol.bounds;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / (sampleCount - 1);
            Vector2 rayTopPoint = Vector2.Lerp(
                new Vector2(bounds.min.x, bounds.max.y),
                new Vector2(bounds.max.x, bounds.max.y),
                t
            );
            Vector2 rayOrigin = new Vector2(rayTopPoint.x, rayTopPoint.y + rayUpOffset);

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, checkHeight, playerLayer);
            Debug.DrawRay(rayOrigin, Vector2.down * checkHeight, Color.green, Time.fixedDeltaTime);

            if (hit.collider != null)
            {
                Rigidbody2D playerRb = hit.rigidbody;
                if (playerRb != null && playerRb.bodyType == RigidbodyType2D.Dynamic)
                {
                    result.Add(playerRb);
                }
            }
        }
        return result;
    }

    void OnDestroy()
    {
        _riders.Clear();
    }

    // Scene窗口绘制射线辅助调试
    void OnDrawGizmosSelected()
    {
        if (_platformCol == null) return;
        Bounds b = _platformCol.bounds;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / (sampleCount - 1);
            Vector2 p = Vector2.Lerp(new Vector2(b.min.x, b.max.y), new Vector2(b.max.x, b.max.y), t);
            Vector2 origin = new Vector2(p.x, p.y + rayUpOffset);
            Gizmos.DrawLine(origin, origin + Vector2.down * checkHeight);
        }
    }
}