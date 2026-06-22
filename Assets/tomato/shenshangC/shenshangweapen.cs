using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class shenshangweapen : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private Transform boss;
    [Header("伤害")]
    [SerializeField] private int normalDamage = 15;
    [SerializeField] private int heavyDamage = 30;

    [Header("第三段击退")]
    [SerializeField] private float knockbackForce;
    [SerializeField] private float knockbackDuration;

    [Header("图层")]
    [SerializeField] private LayerMask playerLayer;

    public int currentHitStage;   // 0=未激活, 1=第一段, 2=第二段, 3=第三段（BossAI 设置）
    private Transform player;

    void Start()
    {
        boss = transform.parent;   // 武器是 Boss 的子物体
        GameObject obj = GameObject.FindGameObjectWithTag("Player");
        if (obj) player = obj.transform;
    }

    // —— 在 BossWeapon 里加一个格挡计算 ——
    private int CalcDamage(int baseDamage, player playerScript)
    {
        if (playerScript == null) return baseDamage;

        // 完美弹反：无伤
        if (playerScript.perfectActive)
        {
            Debug.Log("完美弹反！无伤");
            return 0;
        }

        // 普通格挡：减伤（自己调比例）
        if (playerScript.isBlocking)
        {
            return Mathf.RoundToInt(baseDamage * 0.3f);  // 只吃 30% 伤害
        }

        return baseDamage;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (currentHitStage == 0) return;
        if (((1 << other.gameObject.layer) & playerLayer) == 0) return;

        if (!player) player = other.transform;
        player ps = other.GetComponent<player>();

        if (currentHitStage <= 2)
        {
            // 第一、二段：普通伤害
            int dmg = CalcDamage(normalDamage, ps);
            if (dmg > 0 && ps != null) ps.TakeDamage(dmg);
            else if (dmg == 0 && ps != null)
                ps.ActivateInvincibility();
        }
        else if (currentHitStage == 3)
        {
            // 第三段：重击 + 击退（关掉 stage 防重复触发）
            int dmg = CalcDamage(heavyDamage, ps);
            if (dmg == 0) return;

            if (ps != null)
            {
                ps.TakeDamage(dmg);
                StartCoroutine(Knockback());
            }
            else if (dmg == 0 && ps != null)
            {
                ps.ActivateInvincibility();   // 完美格挡 → 无敌，不击退
            }
        }
    }

    IEnumerator Knockback()
    {
        Debug.Log("击退触发！player=" + (player != null) + " force=" + knockbackForce);
        if (!player) yield break;

        Rigidbody2D pRb = player.GetComponent<Rigidbody2D>();

        // 远离 Boss，仅水平方向
        float facing = player.position.x > boss.position.x ? 1f : -1f;
        Vector2 dir = new Vector2(facing, 0f);

        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;//不衰减
            Vector2 move = dir * knockbackForce * Time.deltaTime;

            if (pRb != null)
                pRb.MovePosition(pRb.position + move);
            else
                player.Translate(move);

            yield return null;
        }
    }
}
