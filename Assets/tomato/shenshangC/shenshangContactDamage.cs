using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class shenshangContactDamage : MonoBehaviour
{
    [Header("伤害")]
    [SerializeField] private int contactDamage = 10;

    [Header("击飞")]
    [SerializeField] private float knockbackForce;//水平速度
    [SerializeField] private float knockbackDuration;

    [Header("图层")]
    [SerializeField] private LayerMask playerLayer;

    private Transform boss;

    void Start()
    {
        boss = transform; // 这个脚本挂 Boss 身上，所以 this.transform 就是 Boss
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        TryHit(collision.gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other.gameObject);
    }

    void TryHit(GameObject hitObject)
    {
        int layer = 1 << hitObject.layer;
        if ((layer & playerLayer) == 0) return;

        player p = hitObject.GetComponent<player>();
        if (p == null) return;

        // 直接伤害，不走格挡
        p.TakeDamage(contactDamage);

        // 击飞
        StartCoroutine(Knockback(hitObject.transform));
    }

    IEnumerator Knockback(Transform playerT)
    {
        Rigidbody2D pRb = playerT.GetComponent<Rigidbody2D>();
        player pScript = playerT.GetComponent<player>();
        float facing = playerT.position.x > boss.position.x ? 1f : -1f;

        if (pScript != null) pScript.isKnockedBack = true;

        float hSpeed = knockbackForce;
        float vSpeed = knockbackForce * 0.8f;

        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;
            vSpeed -= 40f * Time.deltaTime;

            if (pRb != null)
                pRb.velocity = new Vector2(facing * hSpeed, vSpeed);
            else
                playerT.Translate(new Vector2(facing * hSpeed, vSpeed) * Time.deltaTime);

            yield return null;
        }

        // 击退时间到 → 归还控制，剩余速度自然衰减
        if (pScript != null) pScript.isKnockedBack = false;
    }
}
