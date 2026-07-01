using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangedEnemy : MonoBehaviour
{
    [Header("巡逻组件")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float waitAtWaypoint = 1f;

    [Header("追击组件")]
    [SerializeField] private float chaseSpeed = 3f;
    [SerializeField] private float detectionRange = 6f;
    [SerializeField] private float detectionAngle = 45f;

    [Header("攻击组件")]
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletSpeed = 8f;
    [SerializeField] private int bulletDamage = 10;

    [Header("引用组件")]
    [SerializeField] private LayerMask playerLayer;

    [Header("生命值")]
    [SerializeField] private int health = 8;

    private Rigidbody2D rb;
    private Animator anim;

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
        if (attackTimer > 0)
            attackTimer -= Time.deltaTime;

        float distanceToPlayer = DetectPlayer();

        switch (currentState)
        {
            case State.Patrol:
                Patrol();
                if (distanceToPlayer <= detectionRange && player != null)
                    SwitchState(State.Chase);
                break;

            case State.Chase:
                Chase();
                if (distanceToPlayer > detectionRange || player == null)
                    SwitchState(State.Patrol);
                else if (distanceToPlayer <= attackRange)
                    SwitchState(State.Attack);
                break;

            case State.Attack:
                Attack();
                if (distanceToPlayer > attackRange || player == null)
                    SwitchState(State.Chase);
                break;
        }
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
        if (player == null) return;

        float targetX = Mathf.Clamp(player.position.x, patrolMin.x, patrolMax.x);
        Vector2 target = new Vector2(targetX, transform.position.y);

        Vector2 newPos = Vector2.MoveTowards(rb.position, target, chaseSpeed * Time.deltaTime);
        rb.MovePosition(newPos);
        FlipToward(player.position);
    }

    // ==================== 攻击 ====================

    void Attack()
    {
        if (player == null) return;

        FlipToward(player.position);

        if (attackTimer <= 0)
        {
            attackTimer = attackCooldown;
            FireBullet();
        }
    }

    void FireBullet()
    {
        if (bulletPrefab == null)
        {
            return;
        }

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
        if (eb != null)
            eb.damage = bulletDamage;
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

        // 检测范围（黄色扇形）
        Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
        DrawArcGizmo(transform.position, detectionRange, startAngle, endAngle);
        Gizmos.color = Color.yellow;
        DrawArcGizmo(transform.position, detectionRange, startAngle, endAngle, true);

        // 攻击范围（红色扇形）
        Gizmos.color = new Color(1f, 0f, 0f, 0.15f);
        DrawArcGizmo(transform.position, attackRange, startAngle, endAngle);
        Gizmos.color = Color.red;
        DrawArcGizmo(transform.position, attackRange, startAngle, endAngle, true);

        // 巡逻点
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
