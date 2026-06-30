using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// 自动炮塔单脚本
/// 1. 扇形弧形视野检测（距离、角度可在面板调节）
/// 2. 预留开火逻辑标记位置，未实现发射子弹
/// 3. 用InteractHintTrigger函数的Trigger圈作为F键交互范围，目前按F后直接销毁物体模拟自爆
public class AutoTurret : MonoBehaviour
{
    [Header("扇形视野检测参数")]
    [Tooltip("视野最远检测距离")]
    public float sightDistance = 4f;
    [Tooltip("扇形视野左右总角度，如60=左右各30°")]
    public float sightAngle = 60f;
    [Tooltip("玩家所在层级，只检测Player层")]
    public LayerMask playerLayer;

    [Header("子弹发射配置")]
    [Tooltip("方形子弹预制体")]
    public GameObject bulletPrefab;
    [Tooltip("子弹飞行速度（外部可调节）")]
    public float bulletSpeed = 8f;
    [Tooltip("两次发射冷却间隔")]
    public float fireCooldown = 0.6f;
    private float fireTimer;

    [Header("交互自爆设置")]
    [Tooltip("黑入成功后销毁物体的延迟时间")]
    public float destroyDelay = 0.8f;

    [Header("可视化设置")]
    [Tooltip("是否显示视野范围（在游戏场景中）")]
    public bool showVisionRange = true;
    [Tooltip("视野线条颜色")]
    public Color visionColor = Color.red;
    [Tooltip("交互范围颜色")]
    public Color interactColor = Color.yellow;

    // 缓存玩家物体
    private Transform playerTrans;
    // 标记炮塔是否已经被黑入自爆
    private bool isHacked = false;
    // 标记玩家是否处于交互触发圈内
    private bool playerInInteractRange = false;

    // 可视化组件
    private LineRenderer visionLineRenderer;
    private LineRenderer interactLineRenderer;

    void Start()
    {
        // 启动时查找玩家，Tag为Player
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null)
            playerTrans = p.transform;
        else
            Debug.LogWarning("未找到Tag为'Player'的游戏对象！");

        // 创建可视化线条
        SetupVisionLines();
    }

    void Update()
    {
        // 已经黑入，停止所有逻辑
        if (isHacked) return;

        // 冷却倒计时
        if (fireTimer > 0)
            fireTimer -= Time.deltaTime;

        // 1. 每帧执行扇形视野检测
        bool canFire = CheckSightView();
        if (canFire && fireTimer <= 0)
        {
            FireBullet();
            fireTimer = fireCooldown;
        }

        // 2. 玩家在交互圈内、按下F键，执行自爆
        if (playerInInteractRange && Input.GetKeyDown(KeyCode.F))
        {
            HackAndDestroy();
        }

        // 更新可视化线条（每帧更新位置）
        if (showVisionRange)
        {
            UpdateVisionLines();
        }
    }

    /// 设置可视化线条
    void SetupVisionLines()
    {
        // 创建扇形视野的LineRenderer
        GameObject visionObj = new GameObject("VisionRange");
        visionObj.transform.SetParent(transform);
        visionObj.transform.localPosition = Vector3.zero;
        visionObj.transform.localRotation = Quaternion.identity;

        visionLineRenderer = visionObj.AddComponent<LineRenderer>();
        visionLineRenderer.startWidth = 0.05f;
        visionLineRenderer.endWidth = 0.05f;
        // 使用更可靠的材质
        visionLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        visionLineRenderer.startColor = visionColor;
        visionLineRenderer.endColor = visionColor;
        visionLineRenderer.positionCount = 0;
        visionLineRenderer.useWorldSpace = true;

        // 创建交互范围的LineRenderer
        GameObject interactObj = new GameObject("InteractRange");
        interactObj.transform.SetParent(transform);
        interactObj.transform.localPosition = Vector3.zero;
        interactObj.transform.localRotation = Quaternion.identity;

        interactLineRenderer = interactObj.AddComponent<LineRenderer>();
        interactLineRenderer.startWidth = 0.05f;
        interactLineRenderer.endWidth = 0.05f;
        interactLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        interactLineRenderer.startColor = interactColor;
        interactLineRenderer.endColor = interactColor;
        interactLineRenderer.positionCount = 0;
        interactLineRenderer.useWorldSpace = true;
    }

    /// 更新可视化线条
    void UpdateVisionLines()
    {
        if (visionLineRenderer == null || interactLineRenderer == null) return;

        Vector3 pos = transform.position;
        Vector3 forwardDir = transform.right;

        // 更新扇形视野
        int segments = 30;
        List<Vector3> points = new List<Vector3>();

        // 从左边开始绘制
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angle = -sightAngle / 2 + sightAngle * t;
            Vector3 dir = Quaternion.AngleAxis(angle, Vector3.forward) * forwardDir;
            Vector3 point = pos + dir * sightDistance;
            points.Add(point);
        }

        visionLineRenderer.positionCount = points.Count;
        visionLineRenderer.SetPositions(points.ToArray());

        // 更新交互范围（圆形）
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        float radius = col != null ? col.radius : 2f;

        int circleSegments = 36;
        List<Vector3> circlePoints = new List<Vector3>();

        for (int i = 0; i <= circleSegments; i++)
        {
            float angle = (float)i / circleSegments * 360f * Mathf.Deg2Rad;
            Vector3 point = pos + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            circlePoints.Add(point);
        }

        interactLineRenderer.positionCount = circlePoints.Count;
        interactLineRenderer.SetPositions(circlePoints.ToArray());
    }

    /// 扇形弧形视野检测
    bool CheckSightView()
    {
        if (playerTrans == null)
        {
            // 每帧重新尝试查找玩家（防止玩家在运行时生成）
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null)
                playerTrans = p.transform;
            else
                return false;
        }

        // 1. 计算炮塔指向玩家的方向向量
        Vector2 dirToPlayer = playerTrans.position - transform.position;
        float distanceToPlayer = dirToPlayer.magnitude;

        // 2. 距离超过设定视野，直接返回
        if (distanceToPlayer > sightDistance)
            return false;

        // 3. 计算玩家与炮塔正前方的夹角
        float angleBetween = Vector2.Angle(transform.right, dirToPlayer);
        // 夹角小于设定扇形半角，代表玩家在视野内
        if (angleBetween < sightAngle / 2)
        {
            return true;
        }
        return false;
    }

    /// 发射子弹
    void FireBullet()
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("子弹预制体未设置！请在Inspector中拖拽赋值。");
            return;
        }
        if (playerTrans == null) return;

        // 创建子弹
        GameObject bullet = Instantiate(bulletPrefab, transform.position, transform.rotation);

        // 获取BulletSquare组件并赋值
        BulletSquare b = bullet.GetComponent<BulletSquare>();
        if (b != null)
        {
            // 直接给公有字段赋值（虽然标记了HideInInspector，但仍然可以赋值）
            b.bulletSpeed = bulletSpeed;
            b.targetDir = (playerTrans.position - transform.position).normalized;
            Debug.Log($"子弹创建成功，速度: {b.bulletSpeed}, 方向: {b.targetDir}");
        }
        else
        {
            Debug.LogError("子弹预制体上没有BulletSquare组件！请检查预制体。");
            // 如果没有BulletSquare，使用SimpleBullet作为备选
            SimpleBullet simpleBullet = bullet.AddComponent<SimpleBullet>();
            simpleBullet.targetDir = (playerTrans.position - transform.position).normalized;
            simpleBullet.bulletSpeed = bulletSpeed;
        }
    }

    /// 黑入炮塔，延迟销毁模拟自爆
    void HackAndDestroy()
    {
        isHacked = true;
        // 隐藏可视化线条
        if (visionLineRenderer != null) visionLineRenderer.enabled = false;
        if (interactLineRenderer != null) interactLineRenderer.enabled = false;
        Invoke(nameof(SelfDestroy), destroyDelay);
    }

    /// 销毁炮塔物体
    void SelfDestroy()
    {
        Destroy(gameObject);
    }

    // 由InteractHintTrigger的碰撞触发调用，玩家走进交互圈
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInInteractRange = true;
            Debug.Log("玩家进入交互范围");
        }
    }

    // 玩家离开交互圈，取消F键响应
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInInteractRange = false;
            Debug.Log("玩家离开交互范围");
        }
    }

    // 销毁时清理子对象
    void OnDestroy()
    {
        if (visionLineRenderer != null && visionLineRenderer.gameObject != null)
            Destroy(visionLineRenderer.gameObject);
        if (interactLineRenderer != null && interactLineRenderer.gameObject != null)
            Destroy(interactLineRenderer.gameObject);
    }
}

// 简单子弹移动脚本（如果没有BulletSquare脚本时使用）
public class SimpleBullet : MonoBehaviour
{
    public Vector2 targetDir;
    public float bulletSpeed = 8f;

    void Start()
    {
        // 2秒后自动销毁
        Destroy(gameObject, 2f);
        // 添加Rigidbody2D让子弹移动
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.velocity = targetDir * bulletSpeed;
    }
}