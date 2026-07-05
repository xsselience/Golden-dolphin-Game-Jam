using UnityEngine;

public class BulletSquare : MonoBehaviour
{
    [HideInInspector] public float bulletSpeed;
    [HideInInspector] public Vector2 targetDir;
    [Header("子弹最大存活时间(秒)")]
    public float maxLifeTime = 3f;

    private Rigidbody2D rb;
    private float lifeTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // 子弹关闭重力，匀速直线飞行
        rb.gravityScale = 0;
        lifeTimer = maxLifeTime;

        // 关键修复：给子弹设置移动速度
        if (rb != null && targetDir != Vector2.zero)
        {
            rb.velocity = targetDir * bulletSpeed;
        }
        else
        {
            Debug.LogWarning("子弹速度或方向未设置，无法移动！");
        }
    }

    void Update()
    {
        // 生命周期倒计时，超时自动销毁
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0)
        {
            Destroy(gameObject);
        }
    }

    // 正确2D碰撞回调函数，碰到任意物体销毁子弹（碰撞后即判定为命中）
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 可选：添加命中逻辑（比如检测碰撞目标是玩家、播放特效等）
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("子弹命中玩家！");
            // 这里可以添加玩家掉血、播放命中特效等逻辑
        }

        Destroy(gameObject);
    }
}