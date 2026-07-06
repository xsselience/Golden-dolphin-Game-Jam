using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/// 自动炮塔单脚本
/// 1. 扇形弧形视野检测（距离、角度可在面板调节）
/// 2. 预留开火逻辑标记位置，未实现发射子弹
/// 3. 改用距离检测代替Trigger碰撞，按C黑入销毁
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
    [Tooltip("玩家交互范围半径")]
    public float interactRadius = 2f;
    [Header("可视化设置")]
    [Tooltip("是否显示视野范围（在游戏场景中）")]
    public bool showVisionRange = true;
    [Tooltip("视野线条颜色")]
    public Color visionColor = Color.red;
    [Tooltip("交互范围颜色")]
    public Color interactColor = Color.yellow;

    // 新增UI提示
    private Text promptText;
    private readonly string hackTip = "[C] 黑入炮塔";

    // 缓存玩家物体
    private Transform playerTrans;
    // 标记炮塔是否已经被黑入自爆
    private bool isHacked = false;
    // 标记玩家是否处于交互范围内
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

        // 查找提示UI
        if (p != null)
        {
            Transform textTrans = p.transform.Find("Canvas/PromptText");
            if (textTrans != null)
            {
                promptText = textTrans.GetComponent<Text>();
                promptText.enabled = false;
            }
        }

        // 创建可视化线条
        SetupVisionLines();
    }

    void Update()
    {
        // 已经黑入，停止所有逻辑
        if (isHacked)
        {
            if (promptText != null) promptText.enabled = false;
            return;
        }

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

        // 【替换】每帧距离检测判断玩家是否在交互圈内，不再用Trigger
        CheckInteractRangeByDistance();

        // 控制提示文字显示隐藏
        UpdatePromptText();

        // 2. 玩家在交互圈内、按下C键，执行黑入自爆
        if (playerInInteractRange && Input.GetKeyDown(KeyCode.C))
        {
            HackAndDestroy();
        }

        // 更新可视化线条（每帧更新位置）
        if (showVisionRange)
        {
            UpdateVisionLines();
        }
    }

    // 新增：距离检测替代Trigger碰撞
    void CheckInteractRangeByDistance()
    {
        if (playerTrans == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTrans = p.transform;
            return;
        }
        float dis = Vector2.Distance(transform.position, playerTrans.position);
        playerInInteractRange = dis <= interactRadius;
    }

    // 新增：控制提示文字
    void UpdatePromptText()
    {
        if (promptText == null) return;
        if (playerInInteractRange)
        {
            promptText.text = hackTip;
            promptText.enabled = true;
        }
        else
        {
            promptText.enabled = false;
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

        // 更新交互范围圆形（使用面板interactRadius）
        float radius = interactRadius;
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
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null)
                playerTrans = p.transform;
            else
                return false;
        }
        Vector2 dirToPlayer = playerTrans.position - transform.position;
        float distanceToPlayer = dirToPlayer.magnitude;
        if (distanceToPlayer > sightDistance)
            return false;
        float angleBetween = Vector2.Angle(transform.right, dirToPlayer);
        if (angleBetween < sightAngle / 2)
        {
            return true;
        }
        return false;
    }

    /// 发射子弹
    /// 发射子弹
    void FireBullet()
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("子弹预制体未设置！请在Inspector中拖拽赋值。");
            return;
        }
        if (playerTrans == null) return;

        // 计算朝向玩家的归一化方向
        Vector2 targetDir = (playerTrans.position - transform.position).normalized;
        // 根据方向设置子弹旋转（2D sprite默认朝右）
        float bulletAngle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg+180f;
        Quaternion bulletRot = Quaternion.Euler(0, 0, bulletAngle);

        // 关键修改：预制体参数改用 bulletPrefab，局部变量改名避免冲突
        GameObject newBullet = Instantiate(bulletPrefab, transform.position, bulletRot);

        Collider2D turretCol = GetComponent<Collider2D>();
        Collider2D bulletCol = newBullet.GetComponent<Collider2D>();
        if (turretCol != null && bulletCol != null)
        {
            Physics2D.IgnoreCollision(turretCol, bulletCol);
        }

        BulletSquare b = newBullet.GetComponent<BulletSquare>();
        if (b != null)
        {
            b.bulletSpeed = bulletSpeed;
            b.targetDir = targetDir;
            Debug.Log($"子弹创建成功，速度: {b.bulletSpeed}, 方向: {b.targetDir}");
        }
        else
        {
            SimpleBullet simpleBullet = newBullet.AddComponent<SimpleBullet>();
            simpleBullet.targetDir = targetDir;
            simpleBullet.bulletSpeed = bulletSpeed;
        }
    }

    /// 黑入炮塔，延迟销毁模拟自爆
    void HackAndDestroy()
    {
        isHacked = true;
        if (promptText != null) promptText.enabled = false;
        if (visionLineRenderer != null) visionLineRenderer.enabled = false;
        if (interactLineRenderer != null) interactLineRenderer.enabled = false;
        Invoke(nameof(SelfDestroy), destroyDelay);
    }

    void SelfDestroy()
    {
        Destroy(gameObject);
    }

    // ============ 已完全删除原来 OnTriggerEnter2D / OnTriggerExit2D，不再使用碰撞器触发 ============

    void OnDestroy()
    {
        if (visionLineRenderer != null && visionLineRenderer.gameObject != null)
            Destroy(visionLineRenderer.gameObject);
        if (interactLineRenderer != null && interactLineRenderer.gameObject != null)
            Destroy(interactLineRenderer.gameObject);
    }

    void OnDrawGizmosSelected()
    {
        // 场景视图绘制交互范围圆形
        Gizmos.color = interactColor;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}

// 简单子弹移动脚本（如果没有BulletSquare脚本时使用）
public class SimpleBullet : MonoBehaviour
{
    public Vector2 targetDir;
    public float bulletSpeed = 8f;
    void Start()
    {
        Destroy(gameObject, 2f);
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.velocity = targetDir * bulletSpeed;
    }
}