using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class RangedEnemy : MonoBehaviour
{
    [Header("巡逻组件")]
    [Tooltip("巡逻路径点数组，敌人会循环来回走")]
    [SerializeField] private Transform[] waypoints;
    [Tooltip("巡逻移动速度")]
    [SerializeField] private float patrolSpeed = 2f;
    [Tooltip("走到路点后停留等待时间")]
    [SerializeField] private float waitAtWaypoint = 1f;

    [Header("追击组件")]
    [Tooltip("发现玩家后的追击移动速度")]
    [SerializeField] private float chaseSpeed = 3f;
    [Tooltip("扇形侦测总半径，玩家进范围才会被发现")]
    [SerializeField] private float detectionRange = 6f;
    [Tooltip("侦测扇形半角，数值越大视野越宽")]
    [SerializeField] private float detectionAngle = 45f;

    [Header("攻击组件")]
    [Tooltip("攻击有效范围，玩家进入该范围敌人开始攻击")]
    [SerializeField] private float attackRange = 5f;
    [Tooltip("两次发射子弹的冷却间隔")]
    [SerializeField] private float attackCooldown = 2f;
    [Tooltip("子弹预制体，拖入做好的子弹")]
    [SerializeField] private GameObject bulletPrefab;
    [Tooltip("子弹生成发射点，放在敌人枪口位置")]
    [SerializeField] private Transform firePoint;
    [Tooltip("子弹飞行速度")]
    [SerializeField] private float bulletSpeed = 8f;
    [Tooltip("子弹命中玩家造成的伤害数值")]
    [SerializeField] private int bulletDamage = 10;
    [Header("攻击缓冲")]
    [Tooltip("发射子弹后锁定攻击状态的时长，防止状态来回切换连发")]
    [SerializeField] private float attackLockDuration = 0.6f;
    private float attackLockTimer = 0;
    [Tooltip("子弹发射强制冷却，填5就是5秒才能射一次")]
    [SerializeField] private float shootInterval = 5f;
    private float nextShootTime;

    [Header("动画使用组件")]
    [Tooltip("攻击状态标记，控制攻击动画布尔参数")]
    public bool Attacking;
    private Animator anim;

    [Header("引用组件")]
    [Tooltip("玩家图层遮罩，只勾选Player图层")]
    [SerializeField] private LayerMask playerLayer;

    [Header("生命值")]
    [Tooltip("敌人总血量，归零销毁自身")]
    [SerializeField] private int health = 8;

    private Rigidbody2D rb;
    private enum State { Patrol, Chase, Attack }
    private State currentState;
    private Transform player;
    private int currentWaypointIndex;
    private float waitTimer;
    private float attackTimer;
    private bool isWaiting;
    private Vector2 patrolMin;
    private Vector2 patrolMax;

    void Start()
    {
        currentState = State.Patrol;
        isWaiting = false;
        attackTimer = 0f;
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        CalculatePatrolBounds();
    }

    void Update()
    {
        // 新增锁定计时递减
        if (attackLockTimer > 0)
            attackLockTimer -= Time.deltaTime;
        SwitchAnim();
        if (attackTimer > 0)
            attackTimer -= Time.deltaTime;
        float distanceToPlayer = DetectPlayer();
        switch (currentState)
        {
            case State.Patrol:
                Patrol();
                if (distanceToPlayer <= detectionRange && player != null)
                    SwitchState(State.Chase);
                else
                {
                    Attacking = false;
                    anim.SetBool("EnemyAttacking", false);
                }
                break;
            case State.Chase:
                Chase();
                if (distanceToPlayer > detectionRange || player == null)
                {
                    Attacking = false;
                    anim.SetBool("EnemyAttacking", false);
                    SwitchState(State.Patrol);
                }
                else if (distanceToPlayer <= attackRange)
                {
                    SwitchState(State.Attack);
                }
                break;
            case State.Attack:
                Attack();
                if (attackLockTimer <= 0 && (distanceToPlayer > attackRange || player == null))
                {
                    Attacking = false;
                    anim.SetBool("EnemyAttacking", false);
                    SwitchState(State.Chase);
                }
                break;
        }
    }

    private void SwitchAnim()
    {
        if (anim != null)
            anim.SetBool("EnemyAttacking", Attacking);
    }

    // ==================== 巡逻区域 ====================
    void CalculatePatrolBounds()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            patrolMin = patrolMax = transform.position;
            return;
        }
        patrolMin = waypoints[0].position;
        patrolMax = waypoints[0].position;
        foreach (Transform t in waypoints)
        {
            Vector2 p = t.position;
            patrolMin = Vector2.Min(patrolMin, p);
            patrolMax = Vector2.Max(patrolMax, p);
        }
    }

    // ==================== 检测玩家 ====================
    float DetectPlayer()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectionRange, playerLayer);
        if (hit == null)
        {
            player = null;
            return Mathf.Infinity;
        }
        Vector2 dirToPlayer = hit.transform.position - transform.position;
        float dist = dirToPlayer.magnitude;
        Vector2 facingDir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        float angle = Vector2.Angle(facingDir, dirToPlayer);
        if (angle <= detectionAngle)
        {
            player = hit.transform;
            return dist;
        }
        player = null;
        return Mathf.Infinity;
    }

    // ==================== 巡逻 ====================
    void Patrol()
    {
        if (waypoints.Length == 0) return;
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0) isWaiting = false;
            return;
        }
        Transform target = waypoints[currentWaypointIndex];
        Vector2 targetPos = new Vector2(target.position.x, rb.position.y);
        Vector2 newPos = Vector2.MoveTowards(rb.position, targetPos, patrolSpeed * Time.deltaTime);
        rb.MovePosition(newPos);
        FlipToward(target.position);
        if (Mathf.Abs(rb.position.x - targetPos.x) < 0.1f)
        {
            isWaiting = true;
            waitTimer = waitAtWaypoint;
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }
    }

    // ==================== 追击 ====================
    void Chase()
    {
        if (player == null)
            return;
        float targetX = Mathf.Clamp(player.position.x, patrolMin.x, patrolMax.x);
        Vector2 target = new Vector2(targetX, transform.position.y);
        Vector2 newPos = Vector2.MoveTowards(rb.position, target, chaseSpeed * Time.deltaTime);
        rb.MovePosition(newPos);
        FlipToward(player.position);
        if (Mathf.Abs(targetX - rb.position.x) < 0.05f &&
            (player.position.x < patrolMin.x || player.position.x > patrolMax.x))
        {
            if (player.position.x > transform.position.x)
                transform.localScale = new Vector3(-1, 1, 1);
            else
                transform.localScale = new Vector3(1, 1, 1);
        }
    }

    // ==================== 攻击 ====================
    // ==================== 攻击 ====================
    // ==================== 攻击 ====================
    // ==================== 攻击 ====================
    void Attack()
    {
        if (player == null)
        {
            Attacking = false;
            anim.SetBool("EnemyAttacking", false);
            return;
        }
        FlipToward(player.position);
        Attacking = true;
        anim.SetBool("EnemyAttacking", true);

        // 全局时间判定，没到冷却时间直接跳过发射
        if (Time.time >= nextShootTime)
        {
            if (bulletPrefab != null && firePoint != null)
            {
                Vector3 spawnPos = firePoint.position;
                float dirX = player.position.x - transform.position.x;
                Quaternion bulletRot;
                Vector2 bulletDir;

                if (dirX > 0)
                {
                    // 玩家在右侧，子弹原图朝左，旋转180度
                    bulletRot = Quaternion.Euler(0, 0, 180);
                    bulletDir = Vector2.right;
                }
                else
                {
                    // 玩家在左侧，无需旋转
                    bulletRot = Quaternion.identity;
                    bulletDir = Vector2.left;
                }

                GameObject bullet = Instantiate(bulletPrefab, spawnPos, bulletRot);

                Collider2D enemyCol = GetComponent<Collider2D>();
                Collider2D bulletCol = bullet.GetComponent<Collider2D>();
                if (enemyCol != null && bulletCol != null)
                    Physics2D.IgnoreCollision(bulletCol, enemyCol);

                Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
                if (bulletRb != null)
                {
                    bulletRb.gravityScale = 0; // 禁止子弹下坠
                    bulletRb.velocity = bulletDir * bulletSpeed;
                }

                EnemyBullet eb = bullet.GetComponent<EnemyBullet>();
                if (eb != null) eb.damage = bulletDamage;
            }
            // 发射后强制锁定5秒，无论动画怎么重复播放、状态怎么切换都不会再射
            nextShootTime = Time.time + shootInterval;
        }
    }
    public void OnFireBullet()
    {
        if (bulletPrefab == null || player == null) return;
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        Collider2D enemyCol = GetComponent<Collider2D>();
        Collider2D bulletCol = bullet.GetComponent<Collider2D>();
        if (enemyCol != null && bulletCol != null)
            Physics2D.IgnoreCollision(bulletCol, enemyCol);
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            Vector2 dir = (player.position - spawnPos).normalized;
            bulletRb.velocity = dir * bulletSpeed;
        }
        EnemyBullet eb = bullet.GetComponent<EnemyBullet>();
        if (eb != null) eb.damage = bulletDamage;
    }

    // ==================== 辅助 ====================
    void SwitchState(State newState)
    {
        currentState = newState;
    }

    void FlipToward(Vector2 target)
    {
        if (target.x > transform.position.x)
            transform.localScale = new Vector3(1, 1, 1);
        else if (target.x < transform.position.x)
            transform.localScale = new Vector3(-1, 1, 1);
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0) Destroy(gameObject);
    }

    // ==================== 可视化 ====================
    void OnDrawGizmosSelected()
    {
        float facingSign = transform.localScale.x > 0 ? 1f : -1f;
        float halfAngle = detectionAngle;
        float startAngle = (facingSign > 0 ? 0f : 180f) - halfAngle;
        float endAngle = startAngle + halfAngle * 2f;
        Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
        DrawArcGizmo(transform.position, detectionRange, startAngle, endAngle);
        Gizmos.color = Color.yellow;
        DrawArcGizmo(transform.position, detectionRange, startAngle, endAngle, true);
        Gizmos.color = new Color(1f, 0f, 0f, 0.15f);
        DrawArcGizmo(transform.position, attackRange, startAngle, endAngle);
        Gizmos.color = Color.red;
        DrawArcGizmo(transform.position, attackRange, startAngle, endAngle, true);
        if (waypoints != null && waypoints.Length > 1)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] != null)
                {
                    Gizmos.DrawWireSphere(waypoints[i].position, 0.2f);
                    int next = (i + 1) % waypoints.Length;
                    if (waypoints[next] != null)
                        Gizmos.DrawLine(waypoints[i].position, waypoints[next].position);
                }
            }
            Gizmos.color = Color.green;
            Vector2 center = (patrolMin + patrolMax) / 2f;
            Vector2 size = patrolMax - patrolMin;
            Gizmos.DrawWireCube(center, size);
        }
    }

    void DrawArcGizmo(Vector3 origin, float radius, float startAngle, float endAngle, bool wireframe = false)
    {
        int segments = 30;
        Vector3 prev = origin + Quaternion.Euler(0, 0, startAngle) * Vector3.right * radius;
        for (int i = 1; i <= segments; i++)
        {
            float a = Mathf.Lerp(startAngle, endAngle, (float)i / segments);
            Vector3 p = origin + Quaternion.Euler(0, 0, a) * Vector3.right * radius;
            if (wireframe) Gizmos.DrawLine(prev, p);
            prev = p;
        }
        if (wireframe)
        {
            Gizmos.DrawLine(origin, origin + Quaternion.Euler(0, 0, startAngle) * Vector3.right * radius);
            Gizmos.DrawLine(origin, origin + Quaternion.Euler(0, 0, endAngle) * Vector3.right * radius);
        }
    }
}