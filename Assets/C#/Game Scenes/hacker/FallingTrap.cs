using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallingTrap : MonoBehaviour
{
    [Header("设置")]
    [SerializeField] private int crushDamage = 999;   // 秒杀

    [Header("图层")]
    [SerializeField] private LayerMask enemyLayer;

    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer sr;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();

        // 初始冻结
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0;
        }

        if (sr != null) sr.color = Color.white;
    }

    /// <summary>黑入激活，开始下落</summary>
    public void Activate()
    {
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 3f;
        }

        if (sr != null) sr.color = Color.red;   // 落下时变红
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"陷阱撞到: {collision.gameObject.name} layer={collision.gameObject.layer}");
        // 砸到敌人秒杀
        int layer = 1 << collision.gameObject.layer;
        if ((layer & enemyLayer) != 0)
        {
            Debug.Log("命中敌人图层！");
            bossenemy be = collision.gameObject.GetComponent<bossenemy>();
            if (be != null) be.TakeDamage(crushDamage);

            enemy e = collision.gameObject.GetComponent<enemy>();
            if (e != null) e.TakeDamage(crushDamage);
        }
    }
}
