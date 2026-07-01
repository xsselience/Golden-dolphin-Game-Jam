using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingSpawner : MonoBehaviour
{
    [Header("巡逻")]
    [SerializeField] private Transform[] waypoints;          // 两个点来回飞
    [SerializeField] private float flySpeed = 2f;
    [SerializeField] private float bobHeight = 0.3f;        // 上下起伏幅度
    [SerializeField] private float bobSpeed = 3f;           // 起伏速度

    [Header("索敌")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float detectionHalfAngle = 60f; // 120° 总视角
    [SerializeField] private float alertTime = 1f;           // 发现后等多久生成

    [Header("生成")]
    [SerializeField] private SpawnGroup[] spawnGroups;       // 多组生成配置

    [Header("图层")]
    [SerializeField] private LayerMask playerLayer;

    [Header("生命值")]
    [SerializeField] private int health = 10;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;

    private Transform player;
    private int currentWaypointIndex;
    private float bobTimer;
    private float alertTimer;
    //private bool alerted = false;
    private bool spawned = false;

    private float baseY;

    [System.Serializable]
    public class SpawnGroup
    {
        public GameObject enemyPrefab;          // 远程或近战敌人预制体
        public Transform spawnPoint;            // 生成位置
        public Transform[] patrolWaypoints;     // 给生成敌人的巡逻点
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        baseY = transform.position.y;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0;
        }
    }

    void Update()
    {
        if (spawned) return;

        Fly();
        DetectPlayer();
    }

    // ==================== 飞行 ====================

    void Fly()
    {
        if (waypoints == null || waypoints.Length < 2) return;

        Transform target = waypoints[currentWaypointIndex];

        // 水平飞行
        float targetX = target.position.x;
        float newX = Mathf.MoveTowards(transform.position.x, targetX, flySpeed * Time.deltaTime);

        // 上下起伏
        bobTimer += Time.deltaTime * bobSpeed;
        float bobY = Mathf.Sin(bobTimer) * bobHeight;
        float newY = baseY + bobY;

        transform.position = new Vector3(newX, newY, 0);

        // 朝向
        if (targetX > transform.position.x)
            transform.localScale = new Vector3(1, 1, 1);
        else if (targetX < transform.position.x)
            transform.localScale = new Vector3(-1, 1, 1);

        // 到达巡逻点
        if (Mathf.Abs(transform.position.x - targetX) < 0.1f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            baseY = transform.position.y - (Mathf.Sin(bobTimer) * bobHeight); // 修正基点
        }
    }

    // ==================== 检测玩家 ====================

    void DetectPlayer()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectionRange, playerLayer);
        if (hit == null)
        {
            player = null;
            alertTimer = 0f;
            return;
        }

        Vector2 dirToPlayer = hit.transform.position - transform.position;
        float dist = dirToPlayer.magnitude;
        float facingSign = transform.localScale.x > 0 ? 1f : -1f;
        Vector2 facingDir = new Vector2(facingSign, 0f);
        float angle = Vector2.Angle(facingDir, dirToPlayer);

        if (angle <= detectionHalfAngle)
        {
            player = hit.transform;
            alertTimer += Time.deltaTime;

            // 高亮警告
            if (sr != null)
                sr.color = Color.Lerp(Color.white, Color.red, alertTimer / alertTime);

            if (alertTimer >= alertTime && !spawned)
            {
                SpawnAll();
            }
        }
        else
        {
            player = null;
            alertTimer = Mathf.Max(0, alertTimer - Time.deltaTime * 2f); // 缓慢回落
            if (sr != null)
                sr.color = Color.white;
        }
    }

    // ==================== 生成 ====================

    void SpawnAll()
    {
        spawned = true;
        if (sr != null) sr.color = Color.white;

        foreach (SpawnGroup group in spawnGroups)
        {
            if (group.enemyPrefab == null || group.spawnPoint == null) continue;

            GameObject enemy = Instantiate(group.enemyPrefab, group.spawnPoint.position, Quaternion.identity);

            // 给生成的敌人设置巡逻点
            if (group.patrolWaypoints != null && group.patrolWaypoints.Length > 0)
            {
                SetPatrolPoints(enemy, group.patrolWaypoints);
            }
        }

        // 自己消失
        StartCoroutine(FadeOut());
    }

    void SetPatrolPoints(GameObject enemyObj, Transform[] patrols)
    {
        // 近战敌人
        enemy e = enemyObj.GetComponent<enemy>();
        if (e != null)
        {
            var patrolField = typeof(enemy).GetField("waypoints", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (patrolField != null)
                patrolField.SetValue(e, patrols);
        }

        // 远程敌人
        RangedEnemy re = enemyObj.GetComponent<RangedEnemy>();
        if (re != null)
        {
            var patrolField = typeof(RangedEnemy).GetField("waypoints", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (patrolField != null)
                patrolField.SetValue(re, patrols);
        }
    }

    IEnumerator FadeOut()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime;
            if (sr != null)
                sr.color = new Color(1, 1, 1, 1 - t);
            yield return null;
        }
        Destroy(gameObject);
    }

    // ==================== 受伤 ====================

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            // 被杀直接生成
            if (!spawned) SpawnAll();
            Destroy(gameObject);
        }
    }

    // ==================== 可视化 ====================

    void OnDrawGizmosSelected()
    {
        float facingSign = transform.localScale.x > 0 ? 1f : -1f;
        float startAngle = (facingSign > 0 ? 0f : 180f) - detectionHalfAngle;
        float endAngle = startAngle + detectionHalfAngle * 2f;

        Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
        DrawArcGizmo(transform.position, detectionRange, startAngle, endAngle);
        Gizmos.color = Color.yellow;
        DrawArcGizmo(transform.position, detectionRange, startAngle, endAngle, true);

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

        if (spawnGroups != null)
        {
            foreach (SpawnGroup g in spawnGroups)
            {
                if (g.spawnPoint != null)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawWireCube(g.spawnPoint.position, Vector3.one * 0.5f);
                }
            }
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
