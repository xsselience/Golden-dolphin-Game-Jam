using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class boss1ai : MonoBehaviour
{
    // ==================== 引用 ====================
    [Header("引用")]
    [SerializeField] private Transform player;        //player位置
    [SerializeField] private Animator animator;       //动画组件
    [SerializeField] private int bossHealth;          //boss血量

    [Header("武器")]
    [SerializeField] private shenshangweapen bossWeapon;

    // ==================== 普攻 ====================
    [Header("状态一：三段式普通攻击")]//普攻部分预计要搬迁至武器部分
    [SerializeField] private bool normalAttackBool; // Animator 中的 bool 名

    // ==================== 冲刺 ====================
    [Header("状态二：水平冲刺")]
    [SerializeField] private bool DashBool;
    [SerializeField] private float dashSpeed = 15f;          // 冲刺速度
    [SerializeField] private float dashDistance = 8f;        // 冲刺距离
    [SerializeField] private bool isDashing = false;      // 是否正在冲刺移动
    [SerializeField] private Vector3 dashTargetPos;       // 冲刺终点
    [SerializeField] private float dashDirection;         // 冲刺方向（1 或 -1）

    [Header("冲刺碰撞")]
    [SerializeField] private LayerMask wallLayer;     // 墙壁图层

    // ==================== 召唤 ====================
    [Header("状态三：召唤小兵")]
    [SerializeField] private bool SpawnBool;
    [SerializeField] private GameObject enemyPrefab;          // 普通敌人预制体
    [SerializeField] private Transform spawnPoint;            // 生成位置（可设为 Boss 附近空物体）

    // ==================== 天降子弹 ====================
    [Header("状态四：天降子弹")]
    [SerializeField] private bool missileBool;
    [SerializeField] private bool isLocking;
    private Vector3 cachedBulletSpawnPos;
    private float lockedPlayerX;                            // 锁定完成时记录的玩家 X
    [SerializeField] private GameObject redLinePrefab;        // 红线预制体
    [SerializeField] private GameObject fallingBulletPrefab;  // 下落子弹预制体
    [SerializeField] private float bulletFallHeight = 10f;    // 子弹从玩家上方多高处生成
    //[SerializeField] private float lockDuration = 0.8f;       // 锁定时间（红线显示多久）
    [SerializeField] private int bulletDamage = 20;

    // ==================== 自动出招 ====================
    [Header("自动出招")]
    [SerializeField] private bool autoAttack = true;           // 是否启用自动出招
    [SerializeField] private float actionCooldown = 2f;        // 每次出招后的全局冷却
    [SerializeField] private float spawnCooldownExtra = 8f;    // 召唤的额外冷却（防止连续招怪）
    [SerializeField] private float bulletCooldownExtra = 6f;   // 天降子弹的额外冷却

    // —— 概率权重（加起来不用等于 100，按比例分配）——
    [Header("概率权重")]
    [SerializeField] private float weightNormalAttack = 50f;   // 普通攻击
    [SerializeField] private float weightDash = 30f;           // 冲刺
    [SerializeField] private float weightSpawn = 10f;          // 召唤小兵
    [SerializeField] private float weightBullet = 10f;         // 天降子弹

    [Header("双范围检测")]
    [SerializeField] private float meleeRange;        // 近战范围：玩家在这个圈里普攻才生效
    [SerializeField] private float approachSpeed;       // 范围外时朝玩家移动的速度

    [Header("激活范围")]
    [SerializeField] private float activationRange = 12f;     // 玩家进这个范围 Boss 才激活
    private bool isActive = false;                             // 是否已激活

    private float lastActionTime = -99f;          // 上次出招时间
    private float lastSpawnTime = -99f;           // 上次召唤时间
    private float lastBulletTime = -99f;          // 上次天降子弹时间

    private Rigidbody2D rb;

    // ==================== 内部状态 ====================
    private enum BossState { Idle, NormalAttack, Dash, Spawn, VerticalBullet }
    private BossState currentState = BossState.Idle;
    private bool isActing = false;  // 是否正在执行某个状态（防止重复触发）

    // ==================== 初始化 ====================
    void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (animator == null)
            animator = GetComponent<Animator>();
        spawnPoint = GetComponent<Transform>();
        rb = GetComponent<Rigidbody2D>();
    }

    public void Update()
    {
        // 自动出招
        SwitchAnim();
        toAttack();
        if (isLocking)
        {
            Trackingplayer();
        }

        if (isDashing)
        {
            indash();
        }
    }

    public void toAttack()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // —— 安全兜底：如果当前状态机说 Idle 但 isActing 还卡着，强解 ——
        if (currentState == BossState.Idle && isActing)
        {
            Debug.LogWarning("检测到 isActing 卡住，强制释放！");
            isActing = false;
            normalAttackBool = false;
            DashBool = false;
            SpawnBool = false;
            missileBool = false;
            isLocking = false;
            isDashing = false;
            if (!animator.enabled) animator.enabled = true;
        }

        // —— 未激活：检测玩家是否进入激活范围 ——
        if (!isActive)
        {
            if (distanceToPlayer <= activationRange)
            {
                isActive = true;
                Debug.Log("Boss 已激活！");
            }
            else
            {
                return; // 没激活，什么都不做
            }
        }

        // —— 已激活后的正常逻辑 ——

        // 在近战范围外 → 朝玩家移动
        if (distanceToPlayer > meleeRange && !isActing)
        {
            MoveTowardPlayer();
        }

        // 自动出招
        if (autoAttack && !isActing)
        {
            TryAutoAction();
        }
    }

    /// <summary>
    /// 按加权概率随机选择一个可用招式，触发。
    /// </summary>
    void TryAutoAction()
    {
        // 全局冷却还没好 → 不出招
        if (Time.time - lastActionTime < actionCooldown) return;

        // —— 确保 Animator 已回到 Idle，否则不触发 ——
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (!stateInfo.IsName("shenshangStand"))  // 名字改成你动画里 Idle 状态的实际名字
        {
            return;
        }
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // 计算可用权重。冷却没好的排除。
        float wNormal = weightNormalAttack;
        float wDash = weightDash;
        float wSpawn = (Time.time - lastSpawnTime >= spawnCooldownExtra) ? weightSpawn : 0f;
        float wBullet = (Time.time - lastBulletTime >= bulletCooldownExtra) ? weightBullet : 0f;

        // —— 玩家不在近战范围内 → 普攻权重清零 ——
        if (distanceToPlayer > meleeRange)
        {
            wNormal = 0f;
        }

        float totalWeight = wNormal + wDash + wSpawn + wBullet;//总权重


        if (totalWeight <= 0f)
        {
            // 没有任何可用招式（比如都在冷却且不在近战范围）
            return;
        }

        float roll = Random.Range(0f, totalWeight);

        if (roll < wNormal)//随机数小于普攻权重
        {
            Debug.Log("触发普攻");
            TriggerNormalAttack();
        }
        else if (roll < wNormal + wDash)//随机数小于普攻+冲锋权重
        {
            Debug.Log("触发冲刺");
            TriggerDash();
        }
        else if (roll < wNormal + wDash + wSpawn)//随机数小于普攻+冲锋+召唤权重
        {
            Debug.Log("触发召唤");
            TriggerSpawn();
            lastSpawnTime = Time.time;
        }
        else
        {
            Debug.Log("触发导弹");
            TriggerVerticalBullet();
            lastBulletTime = Time.time;
        }

        lastActionTime = Time.time;
    }

    /// <summary>
    /// 朝玩家水平方向移动（范围外时用）。
    /// </summary>
    void MoveTowardPlayer()
    {
        Vector2 target = new Vector2(player.position.x, transform.position.y);
        transform.position = Vector2.MoveTowards(
            transform.position, target, approachSpeed * Time.deltaTime);
        FlipToward(player.position);
    }

    /// <summary>
    /// 让 Boss 面朝目标方向。
    /// </summary>
    void FlipToward(Vector2 target)
    {
        // 只翻转 X 的符号，保留原有体型
        Vector3 scale = transform.localScale;
        float absX = Mathf.Abs(scale.x);   // 原始体型宽度

        if (target.x > transform.position.x)
            transform.localScale = new Vector3(absX, scale.y, scale.z);   // 朝右
        else if (target.x < transform.position.x)
            transform.localScale = new Vector3(-absX, scale.y, scale.z);  // 朝左
    }

    // ==================== 外部调用入口 ====================
    // 可以在其他脚本或 Inspector 事件中调用这五个方法
    public void TriggerNormalAttack()
    {
        if (isActing) return;
        currentState = BossState.NormalAttack;
        isActing = true;
        normalAttackBool = true;
    }

    /// <summary>触发水平冲刺</summary>
    public void TriggerDash()
    {
        if (isActing) return;
        DashRoutine();
    }

    /// <summary>触发召唤小兵</summary>
    public void TriggerSpawn()
    {
        if (isActing) return;
        onSpawn();
    }

    /// <summary>触发天降子弹</summary>
    public void TriggerVerticalBullet()
    {
        if (isActing) return;
        Triggermissile();
    }

    private void SwitchAnim()//动画判定
    {
        animator.SetBool("normalAttack", normalAttackBool);
        animator.SetBool("dash", DashBool);
        animator.SetBool("SpawnRoutine", SpawnBool);
        animator.SetBool("missile", missileBool); 
    }

    // ==================== 状态一：三段式普通攻击 ====================
    public void OnHitStage1()
    {
        Debug.Log("OnHitStage1 触发，bossWeapon=" + (bossWeapon != null));
        if (bossWeapon) bossWeapon.currentHitStage = 1;
    }

    public void OnHitStage2()
    {
        if (bossWeapon) bossWeapon.currentHitStage = 2;
    }

    public void OnHitStage3()
    {
        if (bossWeapon) bossWeapon.currentHitStage = 3;
    }
    /// <summary>动画结束回调，在动画末尾用 Animation Event 调用</summary>
    public void OnNormalAttackEnd()
    {
        Debug.Log("===== 普攻结束 =====");
        if (bossWeapon) bossWeapon.currentHitStage = 0;
        normalAttackBool = false;
        isActing = false;
        currentState = BossState.Idle;
        lastActionTime = Time.time;
    }//这两段是动画主调用的代码不用搬迁

    /// <summary>
    /// 第三段攻击命中时，由 Animation Event 调用。
    /// </summary>
    

    // ==================== 状态二：水平冲刺 ====================//应该改好了

    void DashRoutine()
    {
        isActing = true;
        currentState = BossState.Dash;
        DashBool = true;
        Debug.Log("DashBool = true");
    }
    public void DashStart()
    {
        if (player == null) return;

        float direction = player.position.x > transform.position.x ? 1f : -1f;
        dashTargetPos = transform.position + Vector3.right * direction * dashDistance;
        isDashing = true;
        animator.enabled = false;
        Debug.Log("开始冲刺");
    }
    public void indash()
    {
        if (!isDashing) return;

        float direction = dashTargetPos.x > transform.position.x ? 1f : -1f;
        float step = dashSpeed * Time.deltaTime + 0.1f;  // 多探一点，安全边际

        // BoxCast 检测前方是否有墙（用 Boss 碰撞体大小）
        Collider2D col = GetComponent<Collider2D>();
        RaycastHit2D hit = Physics2D.BoxCast(
            transform.position,
            col.bounds.size,
            0f,
            Vector2.right * direction,
            step,
            wallLayer
        );

        if (hit.collider != null)
        {
            // 撞墙，停在墙前
            float stopX = hit.point.x - direction * col.bounds.extents.x;
            Vector2 stopPos = new Vector2(stopX, rb.position.y);
            rb.MovePosition(stopPos);
            transform.position = new Vector3(stopPos.x, stopPos.y, transform.position.z);
            isDashing = false;
            animator.enabled = true;
            DashEnd();
            Debug.Log("冲刺撞墙，停");
            return;
        }

        // 没撞墙，正常移动
        Vector2 newPos = Vector2.MoveTowards(rb.position, dashTargetPos, step - 0.1f);
        rb.MovePosition(newPos);
        transform.position = new Vector3(newPos.x, newPos.y, transform.position.z);

        if (Vector2.Distance(rb.position, dashTargetPos) <= 0.05f)
        {
            rb.MovePosition(dashTargetPos);
            transform.position = dashTargetPos;
            isDashing = false;
            animator.enabled = true;
            DashEnd();
        }
    }
    
    public void DashEnd()
    {
        Debug.Log("===== 冲刺结束 =====");
        DashBool = false;
        isActing = false;
        currentState = BossState.Idle;
        lastActionTime = Time.time;
    }


    // ==================== 状态三：召唤小兵（动画事件驱动） ====================
    public void onSpawn()
    {
        if (isActing) return;
        isActing = true;
        currentState = BossState.Spawn;

        SpawnBool = true;  // 触发 Animator 播放召唤动画
    }

    /// <summary>
    /// 在召唤动画「挥手/施法」的关键帧上用 Animation Event 调用。
    /// 实际生成小兵。
    /// </summary>
    public void OnSpawnEnemy()
    {
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;

        GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

        // 可选：让小兵立即锁定玩家
        //enemy enemyAI = enemy.GetComponent<enemy>();
        //if (enemyAI != null)
        //{
            // EnemyAI 自己检测玩家并追击
        //}
    }

    /// <summary>
    /// 在召唤动画末尾用 Animation Event 调用。结束召唤状态。
    /// </summary>
    public void OnSpawnEnd()
    {
        Debug.Log("===== 召唤结束 =====");
        SpawnBool = false;
        isActing = false;
        currentState = BossState.Idle;
        lastActionTime = Time.time;
    }

    // ==================== 状态四：天降子弹 ====================
    public void Triggermissile()
    {
        if (isActing) return;
        isActing = true;
        currentState = BossState.VerticalBullet;

        isLocking = true;           // 开始追踪玩家 X
        missileBool = true;            // 触发 Animator 播放锁定动画
    }

    public void Trackingplayer()
    {
        if (player == null) return;

        // —— 锁定期间每帧追踪玩家 X ——
        
        lockedPlayerX = player.position.x;
        
    }

    /// <summary>
    /// 锁定完成，动画事件调用。生成红线，子弹自己跑，Boss 进后摇。
    /// </summary>
    public void OnLockComplete()
    {
        isLocking = false;

        // 子弹生成位置：锁定记录的 X + 玩家头顶上方
        cachedBulletSpawnPos = new Vector3(lockedPlayerX, player.position.y + bulletFallHeight, 0);

        // 红线从玩家位置向上拉到子弹生成点
        if (redLinePrefab != null)
        {
            Vector3 linePos = player.position + Vector3.up * 9f;  // 拔高单位
            GameObject redLine = Instantiate(redLinePrefab, player.position, Quaternion.identity);
            RedLine line = redLine.GetComponent<RedLine>();
            if (line != null)
                line.SetTarget(cachedBulletSpawnPos.y);
        }

        StartCoroutine(SpawnBulletAfterDelay());
    }

    /// <summary>
    /// 1 秒后生成下落子弹。独立协程，不影响 Boss。
    /// </summary>
    IEnumerator SpawnBulletAfterDelay()
    {
        yield return new WaitForSeconds(1f);

        GameObject bullet = Instantiate(fallingBulletPrefab, cachedBulletSpawnPos, Quaternion.identity);
        FallingBullet fallingBullet = bullet.GetComponent<FallingBullet>();
        if (fallingBullet != null)
        {
            fallingBullet.damage = bulletDamage;
        }
    }

    /// <summary>
    /// 后摇结束，动画事件调用。收尾。
    /// </summary>
    public void OnVerticalBulletEnd()
    {
        Debug.Log("===== 天降子弹结束 =====");
        missileBool = false;
        isActing = false;
        currentState = BossState.Idle;
        lastActionTime = Time.time;
    }


    void OnDrawGizmosSelected()
    {
        // 激活范围 — 黄色圈（最大）
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationRange);

        // 近战范围 — 红色圈
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeRange);
    }
}
