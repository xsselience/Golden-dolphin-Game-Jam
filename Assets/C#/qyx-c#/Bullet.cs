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
    }

    void Start()
    {
        rb.velocity = targetDir * bulletSpeed;
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

    // 正确2D碰撞回调函数，碰到任意物体销毁子弹
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Destroy(gameObject);
    }
}