using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemy : MonoBehaviour//暂时我还没搞懂然后写注释但是确实好用
{// ── 巡逻 ──                  大d老师倾情编写怠惰一会
    [SerializeField] private Transform[] waypoints;      // 巡逻点（两个点来回走）
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float waitAtWaypoint = 1f;  // 到点后停多久

    // ── 追击 ──
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float detectionRange = 5f;  // 发现玩家的距离

    // ── 攻击 ──
    [SerializeField] private float attackRange = 1.5f;   // 近战攻击距离
    [SerializeField] private float attackCooldown = 0.8f;
    [SerializeField] private int attackDamage = 1;

    // ── 引用 ──
    [SerializeField] private LayerMask playerLayer;

    private enum State { Patrol, Chase, Attack }
    private State currentState;

    private Transform player;
    private int currentWaypointIndex;
    private float waitTimer;
    private float attackTimer;
    private bool isWaiting;

    void Start()
    {
        currentState = State.Patrol;
        isWaiting = false;
        attackTimer = 0f;
    }

    void Update()
    {
        // 始终检测玩家距离
        float distanceToPlayer = DetectPlayer();

        // 状态切换
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

    // ── 检测玩家 ──
    float DetectPlayer()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectionRange, playerLayer);
        if (hit != null)
        {
            player = hit.transform;
            return Vector2.Distance(transform.position, player.position);
        }
        player = null;
        return Mathf.Infinity;
    }

    // ── 巡逻 ──
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
        transform.position = Vector2.MoveTowards(
            transform.position, target.position, patrolSpeed * Time.deltaTime);

        // 翻转朝向
        FlipToward(target.position);

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

        transform.position = Vector2.MoveTowards(
            transform.position, target, chaseSpeed * Time.deltaTime);

        FlipToward(player.position);
    }

    // ── 攻击 ──
    void Attack()
    {
        if (player == null) return;

        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0)
        {
            attackTimer = attackCooldown;
            Debug.Log("敌人近战攻击！造成 " + attackDamage + " 点伤害");
            // 这里调玩家受伤逻辑：
            // player.GetComponent<PlayerHealth>().TakeDamage(attackDamage);
        }
    }

    // ── 辅助 ──
    void SwitchState(State newState)
    {
        currentState = newState;
        attackTimer = 0f; // 切状态时重置攻击冷却
    }

    void FlipToward(Vector2 target)
    {
        if (target.x > transform.position.x)
            transform.localScale = new Vector3(1, 1, 1);
        else if (target.x < transform.position.x)
            transform.localScale = new Vector3(-1, 1, 1);
    }

    // ── 可视化巡逻范围（仅编辑器中可见）──
    void OnDrawGizmosSelected()
    {
        // 检测范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // 攻击范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // 巡逻点连线
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
        }
    }
}
