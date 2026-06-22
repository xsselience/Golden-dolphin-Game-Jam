using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bossenemy : MonoBehaviour//暂时我还没搞懂然后写注释但是确实好用
{// ── 巡逻 ──                  大d老师倾情编写怠惰一会
    [Header("巡逻组件")]
    [SerializeField] private Transform[] waypoints;      // 巡逻点（两个点来回走）
    [SerializeField] private float patrolSpeed = 2f;     // 巡逻移动速度
    [SerializeField] private float waitAtWaypoint = 1f;  // 到点后停多久

    // ── 追击 ──
    [Header("追击组件")]
    [SerializeField] private float chaseSpeed = 4f;      // 追击移动速度
    [SerializeField] private float detectionRange = 4f;  // 发现玩家的距离

    // ── 攻击 ──
    [Header("攻击组件")]
    [SerializeField] private float attackRange = 1.5f;   // 近战攻击距离
    [SerializeField] private float attackCooldown = 1.5f;// 攻击间隔（秒）

    // ── 引用 ──
    [Header("引用组件")]
    [SerializeField] private LayerMask playerLayer;// 玩家所在的 Layer，用于检测

    [Header("动画使用组件")]
    public Animator anim;
    public bool Attacking;

    [Header("生命值使用组件")]
    public int health = 10;

    private Rigidbody2D rb;
    private enum State { Patrol, Chase, Attack }//状态机
    private State currentState;//当前状态

    private bool hasLanded = false;  // 落地标记
    private Transform player;//缓存玩家的Transform
    private int currentWaypointIndex;// 当前巡逻点的索引
    private float waitTimer;// 巡逻等待计时器
    private float attackTimer;// 攻击冷却计时器
    private bool isWaiting;// 是否正在巡逻点等待

    void Start()
    {
        currentState = State.Patrol;
        isWaiting = false;
        attackTimer = 0f;
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        // 生成后先下落，不等不跑 AI
        StartCoroutine(FallToGround());
        // 没有巡逻点 → 直接去找玩家
        if (waypoints == null || waypoints.Length == 0)
        {
            DetectPlayer();
            if (player != null)
                currentState = State.Chase;
        }
    }

    void Update()
    {
        if (!hasLanded) return;  // 没落地，什么都不做
        SwitchAnim();
        // 冷却倒计时，不论什么状态都跑
        if (attackTimer > 0)
            attackTimer -= Time.deltaTime;
        // 始终检测玩家距离
        float distanceToPlayer = DetectPlayer();

        // 状态切换
        switch (currentState)
        {
            case State.Patrol:
                Patrol();
                if (distanceToPlayer <= detectionRange && player != null)// 玩家进入检测范围 → 切换追击
                    SwitchState(State.Chase);
                break;

            case State.Chase:
                Chase();
                if (distanceToPlayer > detectionRange || player == null)// 玩家跑出检测范围或丢失玩家 → 切回巡逻
                    SwitchState(State.Patrol);
                else if (distanceToPlayer <= attackRange)// 玩家进入攻击范围 → 切换攻击
                    SwitchState(State.Attack);
                break;

            case State.Attack:
                Attack();
                if (distanceToPlayer > attackRange || player == null)// 玩家跑出攻击范围或丢失玩家 → 切回追击
                    SwitchState(State.Chase);
                break;
        }
    }

    // ── 检测玩家 ──
    float DetectPlayer()
    {
        // 画一个圆检测范围内有没有玩家 Layer 的物体
        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectionRange, playerLayer);
        if (hit != null)
        {
            player = hit.transform;// 缓存玩家
            return Vector2.Distance(transform.position, player.position);
        }
        player = null;// 没有玩家就清空
        return Mathf.Infinity;// 返回无限远，保证不会触发任何条件
    }

    // ── 巡逻 ──
    void Patrol()
    {
        // 没有巡逻点 → 切追击
        if (waypoints == null || waypoints.Length == 0)
        {
            DetectPlayer();
            if (player != null)
                SwitchState(State.Chase);
            return;
        }
        // 没有巡逻点就不动
        if (waypoints.Length == 0) return;

        // 正在等待时，只倒计时，不走
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0) isWaiting = false;
            return;
        }

        Transform target = waypoints[currentWaypointIndex];

        // 向当前巡逻点移动
        Vector2 newPos = Vector2.MoveTowards(
            rb.position, target.position, patrolSpeed * Time.deltaTime);
        rb.MovePosition(newPos);

        // 翻转朝向
        FlipToward(target.position);

        // 到达巡逻点附近，开始等待，并切换到下一个巡逻点
        if (Vector2.Distance(transform.position, target.position) < 0.1f)
        {
            isWaiting = true;
            waitTimer = waitAtWaypoint;
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }
    }

    // ── 追击 ──
    void Chase()
    {
        if (player == null) return;

        // 目标位置：玩家X + 敌人自己的Y（只在地面追击）
        Vector2 target = new Vector2(player.position.x, transform.position.y);

        Vector2 newPos = Vector2.MoveTowards(
            rb.position, target, chaseSpeed * Time.deltaTime);
        rb.MovePosition(newPos);


        FlipToward(player.position);
    }

    // ── 攻击 ──
    void Attack()
    {
        if (player == null) return;

        attackTimer -= Time.deltaTime;// 冷却倒计时

        if (attackTimer <= 0)
        {
            attackTimer = attackCooldown;
            Attacking = true;//玩家受伤逻辑交由攻击动画武器上的代码实现
        }
        else if(attackTimer > 0)
        {
            Attacking = false;
        }
    }

    // ── 辅助 ──
    void SwitchState(State newState)
    {
        currentState = newState;
    }

    void FlipToward(Vector2 target)//转向
    {
        if (target.x > transform.position.x)
            transform.localScale = new Vector3(1, 1, 1);// 右
        else if (target.x < transform.position.x)
            transform.localScale = new Vector3(-1, 1, 1);// 左
    }

    // ── 可视化巡逻范围（仅编辑器中可见）──
    void OnDrawGizmosSelected()
    {
        // 黄色检测范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // 红色攻击范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // 青色圈和线巡逻点连线
        if (waypoints != null && waypoints.Length > 1)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] != null)
                {
                    Gizmos.DrawWireSphere(waypoints[i].position, 0.2f);// 巡逻点小球
                    int next = (i + 1) % waypoints.Length;
                    if (waypoints[next] != null)
                        Gizmos.DrawLine(waypoints[i].position, waypoints[next].position); // 连线
                }
            }
        }
    }

    IEnumerator FallToGround()
    {
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 3f;

        float stillTimer = 0f;
        while (stillTimer < 0.2f)
        {
            if (Mathf.Abs(rb.velocity.y) < 0.1f)
                stillTimer += Time.deltaTime;
            else
                stillTimer = 0f;
            yield return null;
        }

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        hasLanded = true;  // 落地，解锁 AI

        // 没有巡逻点 → 追击
        if (waypoints == null || waypoints.Length == 0)
        {
            DetectPlayer();
            if (player != null)
                currentState = State.Chase;
        }
    }

    private void SwitchAnim()//动画判定
    {
        anim.SetBool("EnemyAttacking", Attacking);
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            // 玩家死亡逻辑
        }
    }
}
